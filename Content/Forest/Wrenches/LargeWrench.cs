using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Subclasses.Wrenches;
using SpiritReforged.Common.Visuals;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Wrenches;

public class LargeWrench : ModItem
{
	private class LargeBoostProjectile : GlobalProjectile, IWrenchGlobal
	{
		public const float DAMAGE_BOOST = 0.4f; //40%

		public override bool InstancePerEntity => true;

		public int Duration { get; set; }
		public int DurationMax { get; set; }

		public override void AI(Projectile projectile)
		{
			if (Duration > 0)
			{
				Duration--;
				projectile.GetGlobalProjectile<SpeedModifierProjectile>().SpeedModifier -= 0.15f;

				IWrenchGlobal.ClientPassiveEffects(projectile, 0.5f);
			}
		}

		public override void PostDraw(Projectile projectile, Color lightColor)
		{
			if (Duration > 0)
				IWrenchGlobal.DrawDurationBar(projectile, Duration / (5 * 60f));
		}
	}

	public class LargeWrenchSwing : SwungProjectile, IDrawPixelated, IHitSentry
	{
		public override LocalizedText DisplayName => ModContent.GetInstance<LargeWrench>().DisplayName;

		public override string Texture => ModContent.GetInstance<LargeWrench>().Texture;

		public override float SwingTime => FullyCharged ? base.SwingTime * 0.75f : base.SwingTime;

		public bool FullyCharged => _chargeTime >= CHARGE_TIME_MAX;

		private const int CHARGE_TIME_MAX = 40;
		private int _chargeTime;
		private bool _released;
		private bool _didStrikeSentry;

		public override IConfiguration SetConfiguration() => new BasicConfiguration(EaseFunction.EaseCubicOut, 80, 25);

		public override void AI()
		{
			base.AI();

			Player owner = Main.player[Projectile.owner];
			if (owner.channel && !_released)
			{
				Counter--; //Freeze the animation

				if (++_chargeTime == CHARGE_TIME_MAX)
					SoundEngine.PlaySound(SoundID.MaxMana, Projectile.Center);
			}
			else
			{
				if (Main.myPlayer == Projectile.owner && !_released)
				{
					Projectile.velocity = owner.DirectionTo(Main.MouseWorld);
					Projectile.netUpdate = true;
				}

				_released = true;
			}

			if (_released && FullyCharged && !_didStrikeSentry)
			{
				//Fully charged swing
				foreach (Projectile projectile in Main.ActiveProjectiles)
				{
					if (projectile.owner == Projectile.owner && projectile.sentry && Projectile.Colliding(Projectile.Hitbox, projectile.Hitbox))
					{
						for (int i = 0; i < 15; i++)
						{
							Dust dust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, DustID.Smoke, 0, -1);
							dust.noGravity = true;
							dust.fadeIn = 2;
						}

						projectile.velocity = Vector2.UnitY * -3;
						projectile.Center = Main.MouseWorld;

						Main.player[projectile.owner].FindSentryRestingSpot(projectile.whoAmI, out int worldX, out int worldY, out _);
						projectile.Bottom = new Vector2(worldX, worldY); //Teleport the sentry to a surface position

						_didStrikeSentry = true;
						break;
					}
				}
			}
		}

		public override float GetRotation(out float armRotation, out Player.CompositeArmStretchAmount stretch)
		{
			float value = base.GetRotation(out armRotation, out stretch);
			return value + (MathHelper.PiOver4 - Progress - Math.Min(_chargeTime / (float)CHARGE_TIME_MAX, 1) * 0.1f) * SwingDirection;
		}

		public override bool? CanDamage() => _released ? base.CanDamage() : false;

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			if (FullyCharged)
				modifiers.FinalDamage *= 1.5f;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => IHitSentry.DropScrap(Main.player[Projectile.owner], target);

		void IHitSentry.OnHitSentry(Player player, Projectile sentry, ref int cooldown)
		{
			IHitSentry.ClientHitEffects(sentry);

			player.GetModPlayer<WrenchPlayer>().StoredScrap--;
			sentry.GetGlobalProjectile<LargeBoostProjectile>().Duration = 5 * 60;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			SpriteEffects effects = (SwingDirection == -1) ? SpriteEffects.FlipVertically : default;
			Vector2 origin = new(6, (effects == SpriteEffects.FlipVertically) ? (TextureAssets.Projectile[Type].Value.Height - 60) : 60); //The handle

			DrawHeld(lightColor, origin, Projectile.rotation, effects);
			return false;
		}

		void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch)
		{
			if (_released)
				DrawPixelatedSmear(spriteBatch, Color.Gray);
		}

		public void DrawPixelatedSmear(SpriteBatch spriteBatch, Color color)
		{
			Player owner = Main.player[Projectile.owner];

			//Draw a custom smear
			Main.instance.LoadProjectile(985);
			Texture2D smear = TextureAssets.Projectile[985].Value;

			SpriteEffects effects = (SwingDirection == -1) ? SpriteEffects.FlipVertically : default;
			Rectangle source = smear.Frame(1, 4, 0, (int)(Progress * 14f));
			float rotation = Projectile.rotation - MathHelper.PiOver2 * SwingDirection + SwingDirection * Progress;

			Color lightColor = Lighting.GetColor(Projectile.Center.ToTileCoordinates());
			Vector2 origin = new(source.Width, source.Height / 2);
			Vector2 smearWorldPosition = owner.Center + (Vector2.UnitX * (GetConfig<BasicConfiguration>().Reach + 20)).RotatedBy(rotation);
			Vector2 smearDrawPosition = smearWorldPosition - Main.screenPosition;

			IDrawPixelated.PixelateDrawPosition(ref smearDrawPosition);

			spriteBatch.Draw(smear, smearDrawPosition, source, Projectile.GetAlpha(lightColor.MultiplyRGB(color)), rotation, origin, 0.4f, effects, 0);
			spriteBatch.Draw(smear, smearDrawPosition, source, Projectile.GetAlpha(lightColor.MultiplyRGB(color)).Additive(100), rotation, origin, 0.3f, effects, 0);
		}
	}

	public override void SetDefaults()
	{
		Item.Size = new(38, 40);
		Item.damage = 50;
		Item.knockBack = 6.5f;
		Item.channel = true;
		Item.useTime = Item.useAnimation = 40;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.noUseGraphic = true;
		Item.noMelee = true;
		Item.DamageType = ModContent.GetInstance<WrenchClass>();
		Item.useTurn = true;
		Item.rare = ItemRarityID.Blue;
		Item.shootSpeed = 1;
		Item.shoot = ModContent.ProjectileType<LargeWrenchSwing>();
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, 4.2f, source);
		return false;
	}
}