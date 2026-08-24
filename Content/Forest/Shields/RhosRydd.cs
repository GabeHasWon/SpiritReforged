using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Subclasses.Greatshields;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Shields;

public class RhosRydd : GreatshieldItem
{
	public class RhosRyddBash : SwungProjectile
	{
		public override string Texture => AssetLoader.EmptyTexture;

		public int ChargeTimeMax
		{
			get
			{
				Player owner = Main.player[Projectile.owner];
				return _chargeTimeMax = (_chargeTimeMax == 0) ? _chargeTimeMax = (int)(SwingTime * 1.2f * owner.GetTotalAttackSpeed(DamageClass.Melee)) : _chargeTimeMax;
			}
			set => _chargeTimeMax = value;
		}

		public bool FullyCharged => _chargeTime >= ChargeTimeMax;

		private int _chargeTimeMax;
		private int _chargeTime;
		private bool _released;

		public override IConfiguration SetConfiguration() => new BasicConfiguration(EaseFunction.MultistepEase(EaseFunction.EaseSine, EaseFunction.EaseCubicOut, 0.3f), 40, 25);

		public override void AI()
		{
			base.AI();
			Player owner = Main.player[Projectile.owner];

			if (owner.channel && !_released)
			{
				if (++_chargeTime == ChargeTimeMax && Main.myPlayer == Projectile.owner)
					SoundEngine.PlaySound(SoundID.MaxMana, Projectile.Center);

				Counter--; //Freeze counter
				Projectile.velocity = owner.DirectionTo(PlayerMouseHandler.GetMouse(Projectile.owner));
			}
			else 
			{
				if (FullyCharged)
				{
					if (Counter > SwingTime - 10)
					{
						owner.velocity *= 0.8f;
					}
					else
					{
						int magnitude = 20;
						owner.velocity = Vector2.Lerp(owner.velocity, Projectile.velocity * magnitude * 2, Progress);

						if (Counter == SwingTime / 2)
						{
							owner.velocity = Projectile.velocity * magnitude;
							SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown, Projectile.Center);
						}

						if (Counter == 0)
							SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -1 }, Projectile.Center);

						for (int i = 0; i < 2; i++)
						{
							Dust dust = Dust.NewDustDirect(owner.position, owner.width, owner.height, Main.rand.NextFromList(DustID.Smoke, DustID.CopperCoin));
							dust.noGravity = true;
							dust.velocity = Projectile.velocity;
							dust.fadeIn = 1.2f;
						}
					}

					DashSwordPlayer mp = owner.GetModPlayer<DashSwordPlayer>();
					mp.SetDash();

					owner.armorEffectDrawShadowEOCShield = true;

					if (owner.TryGetModPlayer(out GreatshieldPlayer shieldPlayer))
						shieldPlayer.shieldHealth = 0; //Set shield health to zero until the projectile dies
				}

				_released = true;

				if (Progress < 0.5f)
				{
					Dust dust = Dust.NewDustPerfect(Projectile.Center + Projectile.velocity * 10 + Main.rand.NextVector2Circular(30, 30) * Main.rand.NextFloat(), DustID.Copper, Projectile.velocity, 100);
					dust.noGravity = true;
				}
			}

			owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
		}

		public override bool? CanDamage() => _released ? base.CanDamage() : false;

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (_released && FullyCharged)
			{
				Player owner = Main.player[Projectile.owner];
				owner.velocity = new Vector2(hit.HitDirection * -3f, -4);

				Projectile.Kill();
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int direction = Projectile.direction;
			Color color = Projectile.GetAlpha(lightColor);
			BasicConfiguration config = GetConfig<BasicConfiguration>();

			if (Main.player[Projectile.owner].HeldItem.ModItem is GreatshieldItem shieldItem)
			{
				Texture2D texture = shieldItem.HeldTexture;
				SpriteEffects effects = (direction == -1) ? SpriteEffects.FlipVertically : SpriteEffects.None;
				float rotation = Projectile.rotation + EaseFunction.EaseSine.Ease(Progress * 2) * 0.2f * Projectile.direction;
				float reach = _released ? config.Reach / 2f * config.Easing.Ease(1f - Progress) : 6;

				Vector2 position = Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY)
					+ (Vector2.UnitX * reach).RotatedBy(Projectile.rotation);

				Main.EntitySpriteDraw(texture, position, null, color, rotation, texture.Size() / 2, Projectile.scale, effects);

				if (!_released && FullyCharged) //Charge visual
				{
					Color additiveColor = Color.White.Additive() * EaseFunction.EaseSine.Ease((_chargeTime - ChargeTimeMax) / 30f) * 0.5f;
					Main.EntitySpriteDraw(texture, position, null, additiveColor, rotation, texture.Size() / 2, Projectile.scale, effects);
				}
			}

			if (_released) //Draw a wave
			{
				Main.instance.LoadProjectile(ProjectileID.DD2SquireSonicBoom);

				Texture2D waveTexture = TextureAssets.Projectile[ProjectileID.DD2SquireSonicBoom].Value;
				Vector2 wavePosition = Projectile.Center - Main.screenPosition + (Vector2.UnitX * config.Reach * Progress).RotatedBy(Projectile.rotation);

				Main.EntitySpriteDraw(waveTexture, wavePosition, null, color * (1f - Progress) * 0.3f, Projectile.rotation + MathHelper.PiOver2,
					waveTexture.Size() / 2, new Vector2(0.7f + Progress * 0.3f, 1f - Progress * 0.3f) * Projectile.scale * 0.7f, 0);
			}

			return false;
		}
	}

	public override ShieldInfo SetInfo()
	{
		Item.defense = 2;
		Item.rare = ItemRarityID.Blue;
		Item.damage = 12;
		Item.useTime = Item.useAnimation = 20;
		Item.knockBack = 12;
		Item.channel = true;
		Item.shoot = ModContent.ProjectileType<RhosRyddBash>();

		return new ShieldInfo(25, 60);
	}

	public override void OnBlockDamage(Player player, Player.HurtInfo info) { }

	public override void DrawShield(ref PlayerDrawSet drawInfo, bool guarding)
	{
		if (drawInfo.drawPlayer.ownedProjectileCounts[ModContent.ProjectileType<RhosRyddBash>()] == 0) //Don't draw while performing a shield bash
			base.DrawShield(ref drawInfo, guarding);
	}
}