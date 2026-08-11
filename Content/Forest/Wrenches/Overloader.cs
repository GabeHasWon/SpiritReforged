using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Subclasses.Wrenches;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Forest.Wrenches;

public class Overloader : CopperSpanner
{
	public sealed class OverloaderExplosion : ModProjectile
	{
		public const int EXPLOSION_TIME = 15;

		public ref float Power => ref Projectile.ai[0];

		public int WindupTime { get; private set; }

		public override void SetStaticDefaults() => Main.projFrames[Type] = 9;

		public override void SetDefaults()
		{
			Projectile.ignoreWater = true;
			Projectile.tileCollide = false;
			Projectile.Opacity = 0;
			Projectile.friendly = true;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			if (WindupTime == 0)
			{
				WindupTime = 30 + Math.Min((int)Power, 60); //90 ticks of windup maximum
				Projectile.timeLeft = WindupTime + EXPLOSION_TIME;
			} //Initialize times

			if (Projectile.timeLeft <= EXPLOSION_TIME)
			{
				if (!Main.dedServ && Projectile.Opacity == 0) //One-time explosion effects
				{
					SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode, Projectile.Center);
					EaseFunction ease = EaseFunction.EaseCircularOut;
					Vector2 stretch = Vector2.One;
					float power = Power * 5;

					ParticleHandler.SpawnParticle(new TexturedPulseCircle(Projectile.Center, Color.Goldenrod.Additive(), Color.OrangeRed.Additive(), 1f, 30 * power, 20, "Smoke", stretch, ease)
					{ Angle = Main.rand.NextFloat(-MathHelper.TwoPi, MathHelper.TwoPi) });

					ParticleHandler.SpawnParticle(new TexturedPulseCircle(Projectile.Center, Color.White.Additive(), Color.OrangeRed.Additive(), 0.5f, 30 * power, 20, "Smoke", stretch, ease)
					{ Angle = Main.rand.NextFloat(-MathHelper.TwoPi, MathHelper.TwoPi) });

					ParticleHandler.SpawnParticle(new SmokeCloud(Projectile.Center, Vector2.Zero, Color.Gray, 0.04f * power, EaseFunction.EaseCubicOut, 40));

					for (int i = 0; i < power * 2; i++)
					{
						float magnitude = Main.rand.NextFloat();

						Color color = Color.OrangeRed.Additive();
						Vector2 velocity = Main.rand.NextVector2Unit() * magnitude * 10f;
						float scale = (1f - magnitude) * 0.08f * power;

						ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center + velocity * 10, velocity, color, scale, 10, 3));
						ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center + velocity * 10, velocity, Color.White.Additive(), scale * 0.5f, 10, 3));

						var d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(16f * power), DustID.Torch, Scale: Main.rand.NextFloat() + 0.5f);
						d.noGravity = true;
					}
				}

				Projectile.Opacity = 1;
				Projectile.UpdateFrame(50);
			}
		}

		public override void OnKill(int timeLeft)
		{
			//Explode and die
		}

		public override bool? CanDamage() => (Projectile.timeLeft <= EXPLOSION_TIME) ? null : false;

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Rectangle source = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame, 0, -2);

			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, source, Projectile.GetAlpha(Color.White), Projectile.rotation, source.Size() / 2, Projectile.scale, 0);

			return false;
		}
	}

	public sealed class OverloaderSwing : CopperSpannerSwing, IDrawPixelated, IHitSentry
	{
		public override LocalizedText DisplayName => ModContent.GetInstance<Overloader>().DisplayName;
		public override string Texture => ModContent.GetInstance<Overloader>().Texture;

		public override IConfiguration SetConfiguration() => new BasicConfiguration(EaseFunction.EaseCubicOut, 50, 25);

		void IHitSentry.OnHitSentry(Player player, Projectile sentry, ref int cooldown)
		{
			IHitSentry.ClientHitEffects(sentry);

			if (player.TryGetModPlayer(out WrenchPlayer wrenchPlayer))
			{
				int totalScrap = wrenchPlayer.StoredScrap;
				//wrenchPlayer.StoredScrap = 0; //DEBUG

				if (player.whoAmI == Main.myPlayer) //EXPLODE
					Projectile.NewProjectile(sentry.GetSource_Misc("WrenchHit"), sentry.Center, Vector2.Zero, ModContent.ProjectileType<OverloaderExplosion>(), 999, 9, Projectile.owner, totalScrap);
			}

			SetRecoil();
		}

		void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch) => DrawPixelatedSmear(spriteBatch, new Color(187, 165, 124));
	}

	public override void SetDefaults()
	{
		base.SetDefaults();

		Item.damage = 20;
		Item.useTime = Item.useAnimation = 22;
		Item.shoot = ModContent.ProjectileType<OverloaderSwing>();
	}
}