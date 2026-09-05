using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.RenderTargets;
using Terraria.Graphics.Renderers;

using static SpiritReforged.Content.Glyphs.Shock.ShockGlyph;

namespace SpiritReforged.Content.Particles;

public class ShockGlyphLightningSystem : ModSystem
{
	public interface ILightningProjectile
	{
		public bool Invalid { get; set; }
	}

	private static readonly ModTarget2D LightningTarget = new(static () => particles.Count != 0 || projectiles.Count != 0, DrawLightningTarget);

	public static readonly List<ShockBoltParticle> particles = [];
	public static readonly List<ShockGlyphLightningBolt> projectiles = [];
	public override void Load() => On_Main.DrawProjectiles += Pixelate;
	private static void Pixelate(On_Main.orig_DrawProjectiles orig, Main self)
	{
		orig(self);

		if (LightningTarget != null && LightningTarget.Active)
		{
			SpriteBatch spriteBatch = Main.spriteBatch;

			spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.EffectMatrix);
			
			var noise = AssetLoader.LoadedTextures["noise"].Value;

			Effect effect = AssetLoader.LoadedShaders["LightningGlyphShader"].Value;
			effect.Parameters["uImageSize"].SetValue(LightningTarget.Target.Size());
			effect.Parameters["uPixelSize"].SetValue(2f * Main.GameViewMatrix.Zoom.X);
			effect.Parameters["uTime"].SetValue((float)Main.timeForVisualEffects * 0.001f);
			effect.Parameters["uImage1"].SetValue(noise);

			effect.CurrentTechnique.Passes[0].Apply();

			spriteBatch.Draw(LightningTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f, 0f, 0f);

			spriteBatch.End();
		}
	}

	private static void DrawLightningTarget(SpriteBatch spriteBatch)
	{
		foreach (ShockBoltParticle particle in particles)
		{
			if (particle is null)
				continue;

			particle.LightningDraw(spriteBatch);
		}

		foreach (ShockGlyphLightningBolt projectile in projectiles)
		{
			if (projectile is null || !projectile.Projectile.active)
				continue;

			projectile.LightningDraw(spriteBatch);
		}

		projectiles.RemoveAll(x => x is null || !x.Projectile.active || x is not ILightningProjectile light || light.Invalid);
	}
}

public class ShockBoltParticle : Particle
{

	private readonly ParticleRenderer _lightningParticleRenderer = new();
	private VertexTrail[] _trails;

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;

	Color _startColor;
	Color _endColor;

	public ShockBoltParticle(Vector2 position, Vector2 velocity, Color startColor, Color endColor, float rotation, float scale, int maxTime)
	{
		Position = position;
		_startColor = startColor;
		_endColor = endColor;
		Rotation = rotation;
		Scale = scale;
		MaxTime = maxTime;
		Velocity = velocity;

		ShockGlyphLightningSystem.particles.Add(this);
	}

	public override void Update()
	{
		if (!Main.dedServ)
		{
			if (_trails == null)
				CreateTrail();

			foreach (VertexTrail trail in _trails)
				trail.Update();
		}

		if (Main.rand.NextBool())
			Velocity = Velocity.RotatedByRandom(3.14f);

		Position += Main.rand.NextVector2CircularEdge(0.4f, 0.4f);

		Velocity *= 0.965f;
		
		float progress = EaseBuilder.EaseCircularInOut.Ease(1f - Progress);

		Color color = _startColor;
		color *= 0.33f;

		Lighting.AddLight(Position, color.R / 255f * progress, color.G / 255f * progress, color.B / 255f * progress);
	}

	public override void OnKill() => ShockGlyphLightningSystem.particles.Remove(this);
		
	private void CreateTrail()
	{
		ITrailCap tCap = new RoundCap();
		ITrailPosition tPos = new ParticleTrailPosition(this);
		ITrailShader tShader = new ImageShader(AssetLoader.LoadedTextures["GlowTrail"].Value, Vector2.One);

		_trails =
		[
			new VertexTrail(new GradientTrail(_startColor, _endColor, EaseFunction.EaseCircularOut), tCap, tPos, tShader, 15 * Scale, 60, 2),
			new VertexTrail(new GradientTrail(Color.White.Additive(), Color.Transparent, EaseFunction.EaseQuarticOut), tCap, tPos, tShader, 12 * Scale, 60, 2),
		];
	}

	public void LightningDraw(SpriteBatch spriteBatch)
	{
		if (_trails != null)
		{
			foreach (VertexTrail trail in _trails)
			{
				trail.Opacity = EaseBuilder.EaseCircularInOut.Ease(1f - Progress);
				trail?.Draw(TrailSystem.TrailShaders, spriteBatch.GraphicsDevice);
			}
		}
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		var tex = ParticleHandler.GetTexture(Type);

		float progress = EaseBuilder.EaseCircularInOut.Ease(1f - Progress);

		spriteBatch.Draw(tex, Position - Main.screenPosition, null, _startColor with { A = 0 } * 0.05f * progress, 0, tex.Size() / 2, Scale * 0.3f, SpriteEffects.None, 0);
		spriteBatch.Draw(tex, Position - Main.screenPosition, null, _endColor with { A = 0 } * 0.03f * progress, 0, tex.Size() / 2, Scale * 0.25f, SpriteEffects.None, 0);
	}
}
