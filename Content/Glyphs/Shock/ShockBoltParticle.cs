using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.Visuals.RenderTargets;
using static SpiritReforged.Content.Glyphs.Shock.ShockGlyphLightningSystem;

namespace SpiritReforged.Content.Glyphs.Shock;

public class ShockGlyphLightningSystem : ModSystem
{
	public interface IDrawLightning
	{
		public void LightningDraw(SpriteBatch spriteBatch);
	}

	public static readonly List<IDrawLightning> DrawQueue = [];
	private static readonly EasyTarget LightningTarget = new();

	public override void Load()
	{
		TargetSetup.DrawIntoRendertargets += DrawLightningTarget;
		On_Main.DrawProjectiles += Pixelate;
	}

	private static void DrawLightningTarget()
	{
		GraphicsDevice graphics = Main.graphics.GraphicsDevice;
		if (DrawQueue.Count > 0)
		{
			graphics.SetRenderTarget(LightningTarget.Value);
			graphics.Clear(Color.Transparent);

			SpriteBatch spriteBatch = Main.spriteBatch;
			HashSet<IDrawLightning> queuedForRemoval = [];

			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, Main.Rasterizer, null);

			foreach (IDrawLightning draw in DrawQueue)
			{
				if (draw != null)
					draw.LightningDraw(spriteBatch);
				else
					queuedForRemoval.Add(draw); //The item is no longer valid, prepare to remove
			}

			spriteBatch.End();

			foreach (IDrawLightning draw in queuedForRemoval)
				DrawQueue.Remove(draw);

			graphics.SetRenderTarget(null);
		}
	}

	private static void Pixelate(On_Main.orig_DrawProjectiles orig, Main self)
	{
		orig(self);

		if (DrawQueue.Count > 0 && LightningTarget?.Value != null)
		{
			SpriteBatch spriteBatch = Main.spriteBatch;
			Texture2D noise = AssetLoader.LoadedTextures["noise"].Value;
			Effect effect = AssetLoader.LoadedShaders["LightningGlyphShader"].Value;

			effect.Parameters["uImageSize"].SetValue(LightningTarget.Value.Size());
			effect.Parameters["uPixelSize"].SetValue(2f * Main.GameViewMatrix.Zoom.X);
			effect.Parameters["uTime"].SetValue((float)Main.timeForVisualEffects * 0.001f);
			effect.Parameters["uImage1"].SetValue(noise);

			spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, effect, Main.GameViewMatrix.EffectMatrix);
			spriteBatch.Draw(LightningTarget.Value, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f, 0f, 0f);
			spriteBatch.End();
		}
	}
}

public class ShockBoltParticle : Particle, IDrawLightning
{
	public override ParticleDrawType DrawType => ParticleDrawType.Custom;

	private VertexTrail[] _trails;
	private Color _startColor;
	private Color _endColor;

	public ShockBoltParticle(Vector2 position, Vector2 velocity, Color startColor, Color endColor, float rotation, float scale, int maxTime)
	{
		Position = position;
		_startColor = startColor;
		_endColor = endColor;
		Rotation = rotation;
		Scale = scale;
		MaxTime = maxTime;
		Velocity = velocity;

		DrawQueue.Add(this);
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
		
		float progress = EaseFunction.EaseCircularInOut.Ease(1f - Progress);
		Color color = _startColor * 0.33f;
		Lighting.AddLight(Position, color.R / 255f * progress, color.G / 255f * progress, color.B / 255f * progress);
	}

	public override void OnKill() => DrawQueue.Remove(this);
		
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
			foreach (VertexTrail trail in _trails)
			{
				trail.Opacity = EaseFunction.EaseCircularInOut.Ease(1f - Progress);
				trail?.Draw(TrailSystem.TrailShaders, spriteBatch.GraphicsDevice);
			}
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		Texture2D texture = ParticleHandler.GetTexture(Type);
		float progress = EaseFunction.EaseCircularInOut.Ease(1f - Progress);

		spriteBatch.Draw(texture, Position - Main.screenPosition, null, _startColor.Additive() * 0.05f * progress, 0, texture.Size() / 2, Scale * 0.3f, SpriteEffects.None, 0);
		spriteBatch.Draw(texture, Position - Main.screenPosition, null, _endColor.Additive() * 0.03f * progress, 0, texture.Size() / 2, Scale * 0.25f, SpriteEffects.None, 0);
	}
}