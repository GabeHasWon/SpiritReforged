using Microsoft.Xna.Framework.Graphics;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.Visuals.RenderTargets;
using Terraria.DataStructures;
using Terraria.Graphics.Renderers;
using static SpiritReforged.Content.Forest.Glyphs.Storm.StormGlyph;
using static SpiritReforged.Content.Forest.Glyphs.Void.VoidGlyph;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SpiritReforged.Content.Particles;

public class LightningSystem : ModSystem
{
	private static readonly ModTarget2D LightningTarget = new(static () => particles.Count != 0, DrawLightningTarget);

	public static readonly List<LightningBoltParticle> particles = new();
	public override void Load() => On_Main.DrawProjectiles += Pixelate;
	private static void Pixelate(On_Main.orig_DrawProjectiles orig, Main self)
	{
		if (LightningTarget != null && LightningTarget.Active)
		{
			SpriteBatch spriteBatch = Main.spriteBatch;

			spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.EffectMatrix);

			Effect effect = AssetLoader.LoadedShaders["Pixelate"].Value;
			effect.Parameters["uImageSize"].SetValue(LightningTarget.Target.Size());
			effect.Parameters["uPixelSize"].SetValue(2f * Main.GameViewMatrix.Zoom.X);

			effect.CurrentTechnique.Passes[0].Apply();

			spriteBatch.Draw(LightningTarget, Vector2.Zero, Color.White);

			spriteBatch.End();
		}

		orig(self);
	}

	private static void DrawLightningTarget(SpriteBatch spriteBatch)
	{
		foreach (LightningBoltParticle particle in particles)
		{
			if (particle is null)
				continue;

			particle.LightningDraw(spriteBatch);
		}
	}
}

public class LightningBoltParticle : Particle
{

	private readonly ParticleRenderer _lightningParticleRenderer = new();
	private VertexTrail[] _trails;

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;

	public LightningBoltParticle(Vector2 position, Vector2 velocity, Color color, float rotation, float scale, int maxTime)
	{
		Position = position;
		Color = color;
		Rotation = rotation;
		Scale = scale;
		MaxTime = maxTime;
		Velocity = velocity;

		LightningSystem.particles.Add(this);
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
	}

	public override void OnKill() => LightningSystem.particles.Remove(this);
		
	private void CreateTrail()
	{
		ITrailCap tCap = new RoundCap();
		ITrailPosition tPos = new ParticleTrailPosition(this);
		ITrailShader tShader = new ImageShader(AssetLoader.LoadedTextures["GlowTrail"].Value, Vector2.One);

		_trails =
		[
			new VertexTrail(new GradientTrail(Color, Color.Transparent, EaseFunction.EaseQuarticOut), tCap, tPos, tShader, 15 * Scale, 60, 2),
			new VertexTrail(new GradientTrail(Color.White.Additive(), Color.Transparent, EaseFunction.EaseQuarticOut), tCap, tPos, tShader, 10 * Scale, 60, 2),
		];
	}

	public void LightningDraw(SpriteBatch spriteBatch)
	{
		if (_trails != null)
		{
			foreach (VertexTrail trail in _trails)
			{
				trail.Opacity = EaseBuilder.EaseCircularInOut.Ease(1f - Progress);
				trail?.Draw(TrailSystem.TrailShaders, AssetLoader.BasicShaderEffect, spriteBatch.GraphicsDevice);
			}
		}
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{

	}
}
