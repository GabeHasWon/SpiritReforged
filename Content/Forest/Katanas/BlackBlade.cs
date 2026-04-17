using SpiritReforged.Common;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;
using static SpiritReforged.Content.Forest.Katanas.DamascusKatana;

namespace SpiritReforged.Content.Forest.Katanas;

public class BlackBlade : ModItem
{
	public sealed class BlackBladePlayer : ModPlayer
	{
		public int swiftSwings;
	}

	public class BlackSmokeParticle : Particle
	{
		public override ParticleDrawType DrawType => ParticleDrawType.Custom;

		public BlackSmokeParticle(Vector2 position, int duration, float scale = 1, float rotation = 0, Vector2? velocity = null)
		{
			Position = position;
			Scale = scale;
			Rotation = rotation;
			MaxTime = duration;

			Color = Color.White;
			Velocity = velocity ?? new Vector2(-2).RotatedBy(rotation);
		}

		public override void CustomDraw(SpriteBatch spriteBatch)
		{
			Texture2D texture = Texture;
			Rectangle source = texture.Frame(1, 5, 0, (int)(Progress * 5), 0, -2);
			Velocity *= 0.98f;

			float rotation = Rotation;
			spriteBatch.Draw(texture, Position - Main.screenPosition, source, Lighting.GetColor(Position.ToTileCoordinates()).MultiplyRGB(Color.Gray), rotation, source.Size() / 2, Scale, 0, 0);
		}
	}

	public sealed class BlackBladeSwing : SwungProjectile
	{
		public bool Secondary { get => Projectile.ai[0] == 1; set => Projectile.ai[0] = value ? 1 : 0; }

		public BlackBladePlayer BlackBladePlayer => Main.player[Projectile.owner].TryGetModPlayer(out BlackBladePlayer blackBladePlayer) ? blackBladePlayer : default;

		public override float SwingTime => Secondary ? base.SwingTime * 1.2f : base.SwingTime * ((BlackBladePlayer.swiftSwings != 0) ? 0.7f : 1);

		public override LocalizedText DisplayName => ModContent.GetInstance<BlackBlade>().DisplayName;

		public override void SetStaticDefaults() => Main.projFrames[Type] = 3;

		public override IConfiguration SetConfiguration() => new BasicConfiguration(EaseFunction.EaseCubicOut, 68, 25);

		public override void AI()
		{
			base.AI();

			if (Secondary)
			{
				HoldDistance = Math.Max((1 - EaseFunction.EaseCubicOut.Ease(Progress) * 3) * 24, -8);
				Player owner = Main.player[Projectile.owner];

				DashSwordPlayer mp = owner.GetModPlayer<DashSwordPlayer>();
				mp.SetDash(40);

				if (Counter > SwingTime - 5)
				{
					owner.velocity *= 0.5f;
				}
				else
				{
					int magnitude = 20;
					owner.velocity = Vector2.Lerp(owner.velocity, Projectile.velocity * magnitude * 2, Progress * Progress * Progress * Progress);

					if (Counter == SwingTime / 2)
					{
						owner.velocity = Projectile.velocity * magnitude;
						SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown, Projectile.Center);
					}

					if (Counter == 0)
						SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -1 }, Projectile.Center);
				}

				owner.velocity.Y += owner.gravity * 8 * Progress;

				if (Progress > 0.4f)
				{
					for (int i = 0; i < 3; i++)
					{
						var dust = Dust.NewDustDirect(owner.position, owner.width, owner.height, DustID.Ash, 0, 0, 120, default, Main.rand.NextFloat() * 1.5f);
						dust.noGravity = true;
						dust.velocity = Projectile.velocity * 3;
					}

					ParticleHandler.SpawnParticle(new SmokeCloud(Projectile.Center, Projectile.velocity, Color.Black, 0.15f, EaseFunction.EaseCircularIn, 10)
					{
						TertiaryColor = Color.PaleVioletRed
					});
				}
			}
			else if (Progress < 0.8f && Main.rand.NextBool())
			{
				float intensity = Main.rand.NextFloat();
				var position = Vector2.Lerp(Projectile.Center, GetEndPosition(), intensity);
				Dust.NewDustPerfect(position, Main.rand.NextFromList(DustID.Smoke, DustID.Ash), Vector2.UnitX.RotatedBy(Projectile.rotation + MathHelper.PiOver2 * SwingDirection), 180, default, intensity * 1.5f).noGravity = true;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Secondary)
				BlackBladePlayer.swiftSwings = 3;

			if (!Main.dedServ)
			{
				Vector2 position = target.Hitbox.ClosestPointInRect(GetEndPosition());

				for (int i = 0; i < 3; i++)
				{
					float angle = Main.rand.NextFloat(-1f, 1f);
					Vector2 velocity = (Projectile.velocity * -Main.rand.NextFloat(2)).RotatedBy(angle);

					ParticleHandler.SpawnParticle(new BlackSmokeParticle(position, 20, Main.rand.NextFloat(0.5f, 1), velocity.ToRotation() + MathHelper.PiOver2, velocity));
					var dust = Dust.NewDustPerfect(position, DustID.Ash, Main.rand.NextVector2Circular(2, 2), 150);
					dust.noGravity = true;
					dust.fadeIn = 1.2f;
				}
			}
		}

		public override void OnKill(int timeLeft)
		{
			if (!Secondary)
				BlackBladePlayer.swiftSwings = Math.Max(BlackBladePlayer.swiftSwings - 1, 0);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			SpriteEffects effects = (SwingDirection == -1) ? SpriteEffects.FlipVertically : default;
			Vector2 origin = new(4, 30); //The handle
			Rectangle frame = Secondary ? TextureAssets.Projectile[Type].Value.Frame(1, Main.projFrames[Type], 0, Main.projFrames[Type] - 1, 0, -2) : default;
			bool empowered = BlackBladePlayer.swiftSwings != 0;

			if (empowered && !Secondary)
			{
				DrawSmear(Color.Black * 0.5f, Projectile.rotation, (SwingDirection == -1) ? SpriteEffects.FlipVertically : SpriteEffects.None);
			}

			if (Secondary || empowered)
			{
				Color color = Color.Black;

				DrawHelpers.DrawOutline(Main.spriteBatch, default, default, default, (offset) =>
					DrawHeld(color * (1f - Progress * 2) * 2, origin + offset, Projectile.rotation, effects, frame));
			}

			DrawHeld(lightColor, origin, Projectile.rotation, effects, frame);

			return false;
		}
	}

	private float _swingArc;

	public override void SetStaticDefaults() => SpiritSets.IsSword[Type] = true;

	public override void SetDefaults()
	{
		Item.DefaultToSpear(ModContent.ProjectileType<BlackBladeSwing>(), 1, 20);
		Item.SetShopValues(ItemRarityColor.Blue1, Item.sellPrice(silver: 10));
		Item.damage = 12;
		Item.crit = 2;
		Item.knockBack = 3;
		Item.autoReuse = true;
		MoRHelper.SetSlashBonus(Item);
	}

	public override bool AltFunctionUse(Player player) => player.GetModPlayer<DashSwordPlayer>().HasDashCharge;

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		_swingArc = _swingArc switch
		{
			3f => -4f,
			-4f => 5f,
			_ => 3f
		};

		float swingArc = (player.altFunctionUse == 2) ? 0.3f : _swingArc;
		SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, swingArc, source, player.altFunctionUse - 1);

		return false;
	}

	public override void AddRecipes() { }
}