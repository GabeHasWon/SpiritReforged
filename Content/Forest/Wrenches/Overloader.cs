using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Subclasses.Wrenches;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Forest.Wrenches;

public class Overloader : CopperSpanner
{
	private class ToastedProjectile : GlobalProjectile, IWrenchGlobal
	{
		public const int DURATION_LIMIT = 5 * 60;

		public override bool InstancePerEntity => true;

		public int Duration { get; set; }

		public override void AI(Projectile projectile)
		{
			if (Duration-- > 0 && Main.rand.NextBool(10))
			{
				Vector2 position = projectile.Center + Main.rand.NextVector2Circular(30, 30) * Main.rand.NextFloat();

				ParticleHandler.SpawnParticle(new CompositeSmoke(position, -Vector2.UnitY, Color.Black, 40, false, false));
				ParticleHandler.SpawnParticle(new SmallCompositeSmoke(position, -Vector2.UnitY, Color.Gray, 40, false, false));
			}
		}

		public override Color? GetAlpha(Projectile projectile, Color lightColor) => (Duration > 0) 
			? lightColor.MultiplyRGB(Color.Lerp(Color.White, Color.DarkGray, Duration / (float)DURATION_LIMIT * 5)) * projectile.Opacity 
			: null;
	}

	public sealed class OverloaderExplosion : ModProjectile
	{
		public float Power
		{
			get => MathHelper.Min(Projectile.ai[0], 30);
			set => Projectile.ai[0] = value;
		}

		private bool _initialized;

		public override void SetStaticDefaults() => Main.projFrames[Type] = 9;

		public override void SetDefaults()
		{
			Projectile.Size = new Vector2(100);
			Projectile.ignoreWater = true;
			Projectile.tileCollide = false;
			Projectile.friendly = true;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 20;

			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override void AI()
		{
			if (!_initialized)
			{
				if (!Main.dedServ) //One-time explosion effects
				{
					Main.LocalPlayer.SimpleShakeScreen(4, 5, 10, 300);

					SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode, Projectile.Center);
					Vector2 stretch = Vector2.One;

					for (int i = 0; i < 8 + Power * 0.2f; i++)
					{
						Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(50, 50) * Main.rand.NextFloat();

						ParticleHandler.SpawnParticle(new CompositeSmoke(position, -Vector2.UnitY, Color.Black, 40, false, false));
						ParticleHandler.SpawnParticle(new SmallCompositeSmoke(position, -Vector2.UnitY, Color.Gray, 40, false, false));

						ParticleHandler.SpawnParticle(new EmberParticle(position, Projectile.Center.DirectionTo(position) * Main.rand.NextFloat(3), Color.OrangeRed, 0.8f, Main.rand.Next(20, 40), 5));
					}
				}

				Vector2 center = Projectile.Center;
				Projectile.Size += new Vector2((int)(Power * 5));
				Projectile.Center = center;

				_initialized = true;
			}

			Projectile.UpdateFrame(30);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.OnFire, 120 + 10 * (int)Power);

			ParticleHandler.SpawnParticle(new FireParticle(target.Center, Vector2.UnitY * -4f, [Color.White, Color.Orange, Color.Red], 1f, 0.12f, EaseFunction.EaseCircularOut, 35)
			{ PixelDivisor = 2 });

			for (int i = 0; i < 3; i++)
			{
				Vector2 position = target.Center + Main.rand.NextVector2Circular(20, 20) * Main.rand.NextFloat();

				ParticleHandler.SpawnParticle(new SmallCompositeSmoke(position, -Vector2.UnitY, Color.Black, 30, false, false));
				ParticleHandler.SpawnParticle(new SmallCompositeSmoke(position, -Vector2.UnitY, Color.Gray, 30, false, false));
			}

			ParticleHandler.SpawnParticle(new ImpactLinePrim(target.Center, Vector2.UnitX * Main.rand.NextFloat(-0.1f, 0.1f), Color.OrangeRed.Additive(), new Vector2(1, 2), 10, 0));
			ParticleHandler.SpawnParticle(new ImpactLinePrim(target.Center, Vector2.UnitX * Main.rand.NextFloat(-0.1f, 0.1f), Color.White.Additive(), Vector2.One, 10, 0));
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Rectangle source = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame, 0, -2);

			for (int i = 0; i < 3; i++)
			{
				Color color = Projectile.GetAlpha(Color.White.Additive(100) * (1f - i * 0.2f));
				Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, source, color, Projectile.rotation, source.Size() / 2, Projectile.scale + i * 0.1f, 0);
			}

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
			if (player.TryGetModPlayer(out WrenchPlayer wrenchPlayer))
			{
				int totalScrap = wrenchPlayer.StoredScrap;
				wrenchPlayer.StoredScrap = 0;

				if (player.whoAmI == Main.myPlayer) //EXPLODE
					Projectile.NewProjectile(sentry.GetSource_Misc("WrenchHit"), sentry.Center, Vector2.Zero, ModContent.ProjectileType<OverloaderExplosion>(), 999, 9, Projectile.owner, totalScrap);
			}

			sentry.GetGlobalProjectile<ToastedProjectile>().Duration = ToastedProjectile.DURATION_LIMIT;
			SetRecoil();
		}

		void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch)
		{
			if (!_recoiling)
				DrawPixelatedSmear(spriteBatch, new Color(187, 165, 124));
		}
	}

	public override void SetStaticDefaults() => NPCShopHelper.AddEntry(new NPCShopHelper.ConditionalEntry(static (shop) => shop.NpcType == NPCID.GoblinTinkerer, new NPCShop.Entry(Type)));

	public override void SetDefaults()
	{
		base.SetDefaults();

		Item.SetShopValues(ItemRarityColor.Orange3, Item.buyPrice(gold: 4, silver: 30));
		Item.damage = 20;
		Item.useTime = Item.useAnimation = 22;
		Item.shoot = ModContent.ProjectileType<OverloaderSwing>();
	}
}