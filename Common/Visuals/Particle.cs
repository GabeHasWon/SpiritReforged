using SpiritReforged.Common.Misc;
using System.Reflection;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Common.Visuals;

/// <summary> Provides alternative utilities over <see cref="ABasicParticle"/>.<para/>
/// See <see cref="ParticleRenderers"/> for more info. </summary>
public abstract class Particle : IPooledParticle, IParticle
{
	private static readonly Dictionary<string, Asset<Texture2D>> _textureByName = [];

	public static Texture2D GetTexture<T>() where T : Particle
	{
		Type type = typeof(T);
		if (_textureByName.TryGetValue(type.Name, out Asset<Texture2D> textureAsset))
		{
			return textureAsset.Value;
		}
		else //If the particle texture has not been initialized yet, create a new instance using reflection and fetch the texture a single time
		{
			List<object> parameters = [];
			foreach (ConstructorInfo constructor in type.GetConstructors())
			{
				foreach (ParameterInfo parameter in constructor.GetParameters())
					parameters.Add(!parameter.ParameterType.IsValueType ? null : Activator.CreateInstance(parameter.ParameterType));

				break;
			}

			var instance = (T)Activator.CreateInstance(type, parameters.ToArray());
			Texture2D texture = instance.Texture; //Force a texture initialization

			return texture;
		}
	}

	public virtual string TexturePath => DrawHelpers.RequestLocal(GetType(), GetType().Name);

	public Texture2D Texture
	{
		get
		{
			string name = GetType().Name;
			if (_textureByName.TryGetValue(name, out Asset<Texture2D> textureAsset))
			{
				return textureAsset.Value;
			}
			else
			{
				Asset<Texture2D> newTextureAsset = ModContent.Request<Texture2D>(TexturePath);
				_textureByName.Add(name, newTextureAsset);

				return newTextureAsset.Value;
			}
		}
	}

	public float Progress => (float)TimeActive / MaxTime;

	public bool IsRestingInPool { get; protected set; }

	public bool ShouldBeRemovedFromRenderer { get; set; }

	public int MaxTime;
	public int TimeActive;
	public float Rotation;
	public float Scale = 1f;
	public Vector2 Position;
	public Vector2 Velocity;
	public Color Color = Color.White;

	public virtual void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
	{
		Texture2D texture = Texture;
		spritebatch.Draw(texture, Position + settings.AnchorPosition, null, Color, Rotation, texture.Size() / 2, Scale, 0, 0);
	}

	/// <summary> This particle's basic behaviour. By default, updates position by velocity and expires over time. </summary>
	/// <param name="settings"></param>
	public virtual void Update(ref ParticleRendererSettings settings)
	{
		Position += Velocity;

		if (MaxTime > 0 && ++TimeActive >= MaxTime)
			ShouldBeRemovedFromRenderer = true;
	}

	public void RestInPool() => IsRestingInPool = true;
	public void FetchFromPool() => IsRestingInPool = false; //Reset
}

public sealed class ParticleRenderers : ModSystem
{
	private record class ParticleQueue(ParticleRenderer Renderer, int Time)
	{
		public ParticleRenderer Renderer = Renderer;
		public int Time = Time;
	}

	public static ParticleRenderer[] Renderers { get; private set; }
	public static event Action<ParticleRenderer> OnDrawParticles;

	private static readonly Dictionary<IParticle, ParticleQueue> _particleQueue = new();

	public static readonly ParticleRenderer OverInventory = new();
	public static readonly ParticleRenderer OverHealthBars = new();
	public static readonly ParticleRenderer UnderProjectiles = new();
	public static readonly ParticleRenderer UnderNPCs = new(), OverNPCs = new();
	public static readonly ParticleRenderer OverPlayers = new();
	public static readonly ParticleRenderer OverItems = new();
	public static readonly ParticleRenderer UnderSolids = new(), OverSolids = new();
	public static readonly ParticleRenderer UnderWalls = new();

	public override void Load()
	{
		On_Main.UpdateParticleSystems += UpdateParticles; //Update hooks

		On_Main.DoDraw_WallsAndBlacks += PreDrawWalls;  //Drawing hooks
		On_Main.DoDraw_Tiles_NonSolid += PreDrawSolid;
		On_Main.DoDraw_Tiles_Solid += PostDrawSolid;
		On_Main.DrawNPCs += AroundNPC;
		On_Main.DrawItems += PostDrawItems;
		On_Main.DrawProjectiles += PreDrawProjectiles;
		On_Main.DrawInfernoRings += PostDrawPlayers;
		On_Main.DrawInventory += PostDrawInventory;
		On_Main.DrawInterface_14_EntityHealthBars += PostDrawHealthBars;

		List<ParticleRenderer> renderers = []; //Populate Renderers array
		foreach (FieldInfo field in GetType().GetFields())
		{
			if (field.IsStatic && field.FieldType == typeof(ParticleRenderer))
				renderers.Add((ParticleRenderer)field.GetValue(null));
		}

		Renderers = renderers.ToArray();
	}

	/// <summary> Draws all particles within the given <paramref name="renderer"/> then calls <see cref="OnDrawParticles"/>. </summary>
	/// <param name="spriteBatch"></param>
	/// <param name="renderer"></param>
	/// <param name="uiLayer"></param>
	public static void DrawParticles(SpriteBatch spriteBatch, ParticleRenderer renderer, bool uiLayer = false)
	{
		if (!uiLayer)
			renderer.Settings.AnchorPosition = -Main.screenPosition; //Ensure screen position is always accurate

		renderer.Draw(spriteBatch);

		OnDrawParticles?.Invoke(renderer);
	}

	public static bool QueueParticle(ParticleRenderer renderer, Particle particle, int time) => _particleQueue.TryAdd(particle, new(renderer, time));

	private static void UpdateParticles(On_Main.orig_UpdateParticleSystems orig, Main self)
	{
		foreach (ParticleRenderer renderer in Renderers)
			renderer.Update();

		HashSet<IParticle> queuedForRemoval = []; //Update the particle queue
		foreach (IParticle item in _particleQueue.Keys)
		{
			if (--_particleQueue[item].Time <= 0)
			{
				_particleQueue[item].Renderer.Add(item);
				queuedForRemoval.Add(item);
			}
		}

		foreach (IParticle item in queuedForRemoval)
			_particleQueue.Remove(item);

		orig(self);
	}

	private static void PreDrawWalls(On_Main.orig_DoDraw_WallsAndBlacks orig, Main self)
	{
		if (UnderWalls.Particles.Count > 0)
		{
			SpriteBatch spriteBatch = Main.spriteBatch;

			spriteBatch.End();
			spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, default, default, RasterizerState.CullCounterClockwise, default, Main.Transform);

			DrawParticles(spriteBatch, UnderWalls);

			spriteBatch.RestartToDefault();
		}

		orig(self);
	}

	private static void PreDrawSolid(On_Main.orig_DoDraw_Tiles_NonSolid orig, Main self)
	{
		orig(self);

		UnderSolids.Draw(Main.spriteBatch);
		OnDrawParticles?.Invoke(UnderSolids);
	}

	private static void PostDrawSolid(On_Main.orig_DoDraw_Tiles_Solid orig, Main self)
	{
		orig(self);

		if (OverSolids.Particles.Count > 0)
		{
			SpriteBatch spriteBatch = Main.spriteBatch;
			spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, default, default, RasterizerState.CullCounterClockwise, default, Main.GameViewMatrix.TransformationMatrix);

			DrawParticles(spriteBatch, OverSolids);

			spriteBatch.End();
		}
	}

	private static void AroundNPC(On_Main.orig_DrawNPCs orig, Main self, bool behindTiles)
	{
		DrawParticles(Main.spriteBatch, UnderNPCs);
		orig(self, behindTiles);
		DrawParticles(Main.spriteBatch, OverNPCs);
	}

	private static void PostDrawItems(On_Main.orig_DrawItems orig, Main self)
	{
		orig(self);
		DrawParticles(Main.spriteBatch, OverItems);
	}

	private static void PreDrawProjectiles(On_Main.orig_DrawProjectiles orig, Main self)
	{
		if (UnderProjectiles.Particles.Count > 0)
		{
			SpriteBatch spriteBatch = Main.spriteBatch;
			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, default, default, RasterizerState.CullNone, default, Main.GameViewMatrix.TransformationMatrix);

			DrawParticles(Main.spriteBatch, UnderProjectiles);
			spriteBatch.End();
		}

		orig(self);
	}

	private static void PostDrawPlayers(On_Main.orig_DrawInfernoRings orig, Main self)
	{
		orig(self);

		if (OverPlayers.Particles.Count > 0) //Avoid restarting the SpriteBatch if there's nothing to draw
		{
			SpriteBatch spriteBatch = Main.spriteBatch;

			spriteBatch.End();
			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, default, default, RasterizerState.CullCounterClockwise, default, Main.GameViewMatrix.TransformationMatrix);

			DrawParticles(spriteBatch, OverPlayers);
			spriteBatch.RestartToDefault();
		}
	}

	private static void PostDrawInventory(On_Main.orig_DrawInventory orig, Main self)
	{
		orig(self);
		DrawParticles(Main.spriteBatch, OverInventory, true);
	}

	private static void PostDrawHealthBars(On_Main.orig_DrawInterface_14_EntityHealthBars orig, Main self)
	{
		orig(self);
		DrawParticles(Main.spriteBatch, OverHealthBars, true);
	}
}