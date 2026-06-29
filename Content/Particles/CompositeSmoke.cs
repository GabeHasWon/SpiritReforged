using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals.RenderTargets;
using System.Linq;
using static SpiritReforged.Common.TileCommon.DrawOrderAttribute;

namespace SpiritReforged.Content.Particles;

public class SmokeTargetSystem : ModSystem
{
	private readonly static BlendState Max = new()
	{
		AlphaBlendFunction = BlendFunction.Max,
		ColorBlendFunction = BlendFunction.Max,
		ColorSourceBlend = Blend.One,
		ColorDestinationBlend = Blend.One,
		AlphaSourceBlend = Blend.One,
		AlphaDestinationBlend = Blend.One
	};

	public static readonly List<CompositeSmoke> particles = [];

	// there are NINE particle layers! so we need a render target with 9 "frames"

	private static readonly ModTarget2D SmokeTarget = new(static () => particles.Count != 0, BuildTarget, scale: new Vector2(0.5f, 9 * 0.5f));

	private static void BuildTarget(SpriteBatch spriteBatch)
	{
		if (particles.Count == 0) // Don't restart the spritebatch if there are no particles present
			return;
		
		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);

		for (int i = 0; i < 9; i++)
		{
			var layer = (ParticleLayer)i;

			foreach (CompositeSmoke p in particles.Where(p => p.DrawLayer == layer))
			{
				if (p is null)
					continue;

				p.TargetDraw(spriteBatch, Color.Black, (int)(SmokeTarget.Target.Size().Y / 9) * i);
			}
		}

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Immediate, Max, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
		
		for (int i = 0; i < 9; i++)
		{
			var layer = (ParticleLayer)i;

			foreach (CompositeSmoke p in particles.Where(p => p.DrawLayer == layer))
			{
				if (p is null)
					continue;

				p.TargetDraw(spriteBatch, p.Color, (int)(SmokeTarget.Target.Size().Y / 9) * i);
			}
		}
	}

	/// <summary>
	/// Draws the composite smoke with the frame dependent on the layer
	/// Called in ParticleDetours.cs
	/// </summary>
	/// <param name="frameY">0-8, corresponds to each layer of ParticleLayer</param>
	public static void DrawCompositeSmoke(int frameY, bool startBatch)
	{
		if (SmokeTarget != null && SmokeTarget.Active)
		{
			SpriteBatch spriteBatch = Main.spriteBatch;

			if (startBatch)
				spriteBatch.BeginDefault();

			var sourceRectangle = SmokeTarget.Target.Frame(1, 9, 0, frameY);

			spriteBatch.Draw(SmokeTarget.Target, Vector2.Zero, sourceRectangle, Color.White * 0.8f, 0f, Vector2.Zero, 2f, 0f, 0f);

			if (startBatch)
				spriteBatch.End();
		}
	}
}

public class CompositeSmoke : Particle
{
	internal int _variant;

	internal float _opacity;

	internal bool _addLight;
	internal bool _addBloom;

	private readonly Action<Particle> _action;

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;
	public ParticleLayer Layer { get; set; } = ParticleLayer.BelowProjectile;
	public override ParticleLayer DrawLayer => Layer;
	public CompositeSmoke(Vector2 position, Vector2 velocity, Color color, float scale, int maxTime, bool addLight = true, bool addBloom = true, Action<Particle> extraUpdateAction = null)
	{
		Position = position;
		Velocity = velocity;
		Rotation = Main.rand.NextFloat(6.28f);
		Scale = scale;
		MaxTime = maxTime;

		Color = color;

		_addLight = addLight;
		_addBloom = addBloom;
		_action = extraUpdateAction;

		SmokeTargetSystem.particles.Add(this);
	}

	public override void Update()
	{
		Rotation += Velocity.Length() * 0.01f;
		Velocity *= 0.98f;

		if (_addLight)
			Lighting.AddLight(Position, Color.R / 255f, Color.G / 255f, Color.B / 255f);

		_action?.Invoke(this);
	}

	public override void OnKill() => SmokeTargetSystem.particles.Remove(this);

	public void TargetDraw(SpriteBatch spriteBatch, Color color, int yOffset)
	{
		var texture = Texture;

		float progress = Progress;

		var frame = Texture.Frame(1, 5, 0, (int)MathHelper.Lerp(0, 4, progress));

		float fadeOut = 1f;

		if (progress > 0.5f)
			fadeOut = 1f - (progress - 0.5f) / 0.5f;

		spriteBatch.Draw(texture, (Position - Main.screenPosition) / 2 + Vector2.UnitY * yOffset, frame, color * fadeOut, Rotation, frame.Size() / 2, Scale / 2, SpriteEffects.None, 0);
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		var bloom = AssetLoader.LoadedTextures["Bloom"].Value;

		float progress = Progress;

		float fadeOut = 1f;

		if (progress > 0.5f)
			fadeOut = 1f - (progress - 0.5f) / 0.5f;

		if (_addBloom)
			spriteBatch.Draw(bloom, Position - Main.screenPosition, null, Color with { A = 0 } * 0.33f * fadeOut, Rotation, bloom.Size() / 2, Scale * 0.5f, SpriteEffects.None, 0);
	}
}
