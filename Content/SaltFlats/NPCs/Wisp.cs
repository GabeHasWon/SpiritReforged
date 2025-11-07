using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.SaltFlats.NPCs;

public class Wisp : ModNPC
{
	public class TwirlyParticle(NPC parent, Color tint) : ABasicParticle
	{
		public float Opacity;
		public int TimeToLive = 60;

		private readonly Color _colorTint = tint;
		private readonly NPC _parent = parent;
		private int _timeSinceSpawn;

		public override void Update(ref ParticleRendererSettings settings)
		{
			const int fadeTime = 30;

			base.Update(ref settings);

			if (++_timeSinceSpawn >= TimeToLive)
			{
				ShouldBeRemovedFromRenderer = true;
			}
			else
			{
				float lifeProgress = (float)_timeSinceSpawn / TimeToLive;
				Vector2 targetCenter = _parent.Center;
				Vector2 position = settings.AnchorPosition + LocalPosition;
				float distanceSQ = position.DistanceSQ(targetCenter);

				if (distanceSQ > 20 * 20 * (1 + Scale.Length()) * lifeProgress)
					Velocity = Vector2.Lerp(Velocity, position.DirectionTo(targetCenter) * (float)Math.Sqrt(distanceSQ) / 2, 0.05f);

				if (_timeSinceSpawn > TimeToLive - fadeTime)
					Opacity -= 1f / fadeTime;
				else
					Opacity = Math.Min(Opacity + 1f / fadeTime, 1);
			}
		}

		public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
		{
			Texture2D texture = TextureAssets.Extra[ExtrasID.SharpTears].Value;
			float underlayProgress = 0.5f + Math.Clamp(Velocity.X / 3, -0.5f, 0.5f);

			float opacity = Opacity * MathHelper.Lerp(0.5f, 1, underlayProgress);
			Color brightColor = (Color.White * opacity * 0.9f).Additive(125);
			Color tintColor = (Color.Lerp(Color.MediumVioletRed, _colorTint, underlayProgress) * opacity * 0.5f).Additive(100);

			Vector2 scale = Vector2.One * Scale * MathHelper.Lerp(0.5f, 1, underlayProgress);
			Vector2 position = settings.AnchorPosition + LocalPosition - Main.screenPosition;
			SpriteEffects effects = SpriteEffects.None;

			spritebatch.Draw(texture, position, null, tintColor, (float)Math.PI / 2f + Rotation, texture.Size() / 2, scale, effects, 0f);
			spritebatch.Draw(texture, position, null, tintColor, Rotation, texture.Size() / 2, scale, effects, 0f);
			spritebatch.Draw(texture, position, null, brightColor, (float)Math.PI / 2f + Rotation, texture.Size() / 2, scale * 0.6f, effects, 0f);
			spritebatch.Draw(texture, position, null, brightColor, Rotation, texture.Size() / 2, scale * 0.6f, effects, 0f);
		}
	}

	public override string Texture => AssetLoader.EmptyTexture;

	private readonly ParticleRenderer _twirlParticleRenderer = new();
	private VertexTrail _trail;

	public override void SetDefaults()
	{
		NPC.CloneDefaults(NPCID.Butterfly);
		//NPC.aiStyle = -1;
	}

	public override void AI()
	{
		if (!Main.dedServ)
		{
			if (_trail == null)
				CreateTrail();

			_trail.Update();

			if (Main.rand.NextBool(10))
				_twirlParticleRenderer.Add(new TwirlyParticle(NPC, Color.Cyan)
				{
					LocalPosition = NPC.Center + new Vector2(Main.rand.NextFloat(22f, 30f), 0).RotatedByRandom(1),
					Scale = Vector2.One * Main.rand.NextFloat(0.2f, 0.3f),
					TimeToLive = 100,
					RotationVelocity = 0.1f
				});

			_twirlParticleRenderer.Update();
		}

		NPC.rotation += NPC.velocity.Length() * 0.01f;
	}

	private void CreateTrail()
	{
		ITrailCap tCap = new RoundCap();
		ITrailPosition tPos = new EntityTrailPosition(NPC);
		ITrailShader tShader = new ImageShader(AssetLoader.LoadedTextures["GlowTrail"].Value, Vector2.One);

		_trail = new VertexTrail(new GradientTrail(Color.PaleVioletRed.Additive(150) * 0.5f, Color.Transparent, EaseFunction.EaseQuarticOut), tCap, tPos, tShader, 40, 150);
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		Lighting.AddLight(NPC.Center, new Vector3(0.5f, 0.4f, 0.5f));

		Texture2D star = TextureAssets.Projectile[ProjectileID.RainbowRodBullet].Value;
		Texture2D bloom = AssetLoader.LoadedTextures["Extra_49"].Value;
		float pulse = (float)Math.Sin(Main.timeForVisualEffects / 20f);
		float rotation = NPC.rotation;

		spriteBatch.Draw(bloom, NPC.Center - screenPos, null, NPC.DrawColor(Color.PaleVioletRed).Additive() * 0.3f, rotation, bloom.Size() / 2, NPC.scale * (0.2f + pulse * 0.05f), default, 0);
		spriteBatch.Draw(bloom, NPC.Center - screenPos, null, NPC.DrawColor(Color.Cyan).Additive() * 0.8f, rotation, bloom.Size() / 2, NPC.scale * 0.15f, default, 0);

		var starScale = new Vector2(1 + pulse * 0.2f, 0.5f) * NPC.scale * 0.5f;
		spriteBatch.Draw(star, NPC.Center - screenPos, null, NPC.DrawColor(Color.Cyan).Additive(200), rotation * 0.9f - MathHelper.PiOver2, star.Size() / 2, starScale, default, 0);

		spriteBatch.Draw(star, NPC.Center - screenPos, null, NPC.DrawColor(Color.PaleVioletRed) * 0.25f, rotation, star.Size() / 2, starScale * 2, default, 0);
		spriteBatch.Draw(star, NPC.Center - screenPos, null, NPC.DrawColor(Color.Cyan).Additive(200), rotation, star.Size() / 2, starScale, default, 0);
		spriteBatch.Draw(star, NPC.Center - screenPos, null, NPC.DrawColor(Color.White).Additive(), rotation, star.Size() / 2, starScale * 0.8f, default, 0);

		_twirlParticleRenderer.Draw(spriteBatch);
		_trail?.Draw(TrailSystem.TrailShaders, AssetLoader.BasicShaderEffect, spriteBatch.GraphicsDevice);

		return false;
	}
}