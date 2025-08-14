using SpiritReforged.Common.Misc;

namespace SpiritReforged.Common.Particle;

public enum ParticleLayer
{
	BelowProjectile,
	AboveProjectile,
	AboveNPC,
	AbovePlayer,
	BelowSolid,
	BelowWall
}

public enum ParticleDrawType
{
	DefaultAlphaBlend,
	DefaultAdditive,
	Custom,
	BatchedAdditiveBlend,
	CustomBatchedAdditiveBlend
}

public static class ParticleHandler
{
	private static readonly int MaxParticlesAllowed = 500;

	private static Particle[] particles;
	private static int nextVacantIndex;
	private static int activeParticles;
	private static Dictionary<Type, int> particleTypes;
	private static Dictionary<int, Texture2D> particleTextures;

	public static int TypeOf<T>() where T : Particle => TypeOf(typeof(T));
	public static int TypeOf(Type t) => particleTypes[t];

	internal static void RegisterParticles()
	{
		particles = new Particle[MaxParticlesAllowed];
		particleTypes = [];
		particleTextures = [];

		Type baseParticleType = typeof(Particle);
		SpiritReforgedMod spiritMod = ModContent.GetInstance<SpiritReforgedMod>();

		foreach (Type type in spiritMod.Code.GetTypes())
			if (type.IsSubclassOf(baseParticleType) && !type.IsAbstract && type != baseParticleType)
			{
				int assignedType = particleTypes.Count;
				particleTypes[type] = assignedType;

				string texturePath = type.Namespace.Replace('.', '/') + "/" + type.Name;
				var particleTexture = ModContent.RequestIfExists(texturePath, out Asset<Texture2D> tex, AssetRequestMode.ImmediateLoad) ? tex.Value 
					: ModContent.Request<Texture2D>("SpiritReforged/Assets/Textures/Bloom", AssetRequestMode.ImmediateLoad).Value;

				particleTextures[assignedType] = particleTexture;
			}
	}

	internal static void Unload()
	{
		particles = null;
		particleTypes = null;
		particleTextures = null;
	}

	/// <summary>
	/// Spawns the particle instance provided into the world (if the particle limit is not reached).
	/// </summary>
	public static void SpawnParticle(Particle particle)
	{
		if (Main.netMode == NetmodeID.Server || activeParticles == MaxParticlesAllowed)
			return;

		particles[nextVacantIndex] = particle;
		particle.ID = nextVacantIndex;
		particle.Type = TypeOf(particle.GetType());

		if (nextVacantIndex + 1 < particles.Length && particles[nextVacantIndex + 1] == null)
			nextVacantIndex++;
		else
			for (int i = 0; i < particles.Length; i++)
				if (particles[i] == null)
					nextVacantIndex = i;

		activeParticles++;
	}

	public static void SpawnParticle(int type, Vector2 position, Vector2 velocity, Vector2 origin = default, float rotation = 0f, float scale = 1f)
	{
		var particle = new Particle(); // yes i know constructors exist. yes i'm doing this so you dont have to make constructors over and over.
		particle.Position = position;
		particle.Velocity = velocity;
		particle.Color = Color.White;
		particle.Origin = origin;
		particle.Rotation = rotation;
		particle.Scale = scale;
		particle.Type = type;

		SpawnParticle(particle);
	}

	public static void SpawnParticle(int type, Vector2 position, Vector2 velocity) => SpawnParticle(type, position, velocity, Vector2.Zero, 0f, 1f);

	/// <summary>
	/// Deletes the particle at the given index. You typically do not have to use this; use Particle.Kill() instead.
	/// </summary>
	public static void DeleteParticleAtIndex(int index)
	{
		particles[index] = null;
		activeParticles--;
		nextVacantIndex = index;
	}

	/// <summary>
	/// Clears all the currently spawned particles.
	/// </summary>
	public static void ClearAllParticles()
	{
		for (int i = 0; i < particles.Length; i++)
			particles[i] = null;

		activeParticles = 0;
		nextVacantIndex = 0;
	}

	internal static void UpdateAllParticles()
	{
		foreach (Particle particle in particles)
		{
			if (particle == null)
				continue;

			particle.TimeActive++;
			particle.Position += particle.Velocity;

			particle.Update();

			if (particle.TimeActive > particle.MaxTime && particle.MaxTime > 0)
				particle.Kill();
		}
	}

	internal static void DrawAllParticles(SpriteBatch spriteBatch, ParticleLayer drawLayer) => DrawAllParticles(spriteBatch, (p) => p.DrawLayer == drawLayer);
	internal static void DrawAllParticles(SpriteBatch spriteBatch, Func<Particle, bool> func)
	{
		var batchedNonpremultiplyParticles = new List<Particle>();

		foreach (Particle particle in particles)
		{
			if (particle != null && func.Invoke(particle))
			{
				switch (particle.DrawType)
				{
					case ParticleDrawType.DefaultAlphaBlend:
						spriteBatch.Draw(particleTextures[particle.Type], particle.Position - Main.screenPosition, null, particle.Color, particle.Rotation, particle.Origin + particleTextures[particle.Type].Size() / 2, particle.Scale, SpriteEffects.None, 0f);
						break;

					case ParticleDrawType.DefaultAdditive:
						spriteBatch.Draw(particleTextures[particle.Type], particle.Position - Main.screenPosition, null, particle.Color.Additive(), particle.Rotation, particle.Origin + particleTextures[particle.Type].Size() / 2, particle.Scale, SpriteEffects.None, 0f);
						break;

					case ParticleDrawType.Custom:
						particle.CustomDraw(spriteBatch);
						break;

					case ParticleDrawType.BatchedAdditiveBlend:
						batchedNonpremultiplyParticles.Add(particle);
						break;

					case ParticleDrawType.CustomBatchedAdditiveBlend:
						batchedNonpremultiplyParticles.Add(particle);
						break;
				}
			}
		}

		if (batchedNonpremultiplyParticles.Count != 0)
		{
			spriteBatch.End();
			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);

			foreach (Particle batchedParticle in batchedNonpremultiplyParticles)
			{
				if (batchedParticle.DrawType == ParticleDrawType.CustomBatchedAdditiveBlend)
					batchedParticle.CustomDraw(spriteBatch);
				else
					spriteBatch.Draw(particleTextures[batchedParticle.Type], batchedParticle.Position - Main.screenPosition, null, batchedParticle.Color, batchedParticle.Rotation, batchedParticle.Origin + particleTextures[batchedParticle.Type].Size() / 2, batchedParticle.Scale * Main.GameViewMatrix.Zoom, SpriteEffects.None, 1f);
			}

			spriteBatch.End();
			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
		}
	}

	/// <summary>
	/// Gets the texture of the given particle type.
	/// </summary>
	public static Texture2D GetTexture(int type) => particleTextures[type];

	/// <summary>
	/// Returns the numeric type of the given particle.
	/// </summary>
	public static int ParticleType<T>() => particleTypes[typeof(T)];
}