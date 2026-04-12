using SpiritReforged.Common;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Rapiers;

public class SilverRapier : ModItem
{
	public class SilverRapierSwing : RapierProjectile, FreeDodgePlayer.IFreeDodge
	{
		public enum MoveType { Lunge, Stance, Swing }

		public MoveType Move { get => (MoveType)Projectile.ai[0]; set => Projectile.ai[0] = (int)value; }

		public override float SwingTime => (Move == MoveType.Stance) ? FreeDodgeTime : base.SwingTime;

		public override string Texture => ModContent.GetInstance<SilverRapier>().Texture;
		public override LocalizedText DisplayName => ModContent.GetInstance<SilverRapier>().DisplayName;

		private BasicNoiseCone _motionCone;

		public override IConfiguration SetConfiguration() => new RapierConfiguration(EaseFunction.EaseCubicOut, 58, 12, 12, 15);

		public override void AI()
		{
			base.AI();

			if (!Main.dedServ && Move == MoveType.Lunge && Counter == 1)
			{
				Vector2 position = Projectile.Center - Projectile.velocity * 8;
				ParticleHandler.SpawnParticle(_motionCone = (BasicNoiseCone)new BasicNoiseCone(position, Projectile.velocity, 14, new(50, 150)).SetColors(Color.White.Additive(100), Color.SteelBlue).SetIntensity(2).AttachTo(Projectile));
			}
		}

		public bool FreeDodge(Player.HurtInfo info)
		{
			if (Move != MoveType.Stance)
				return false;

			if (!Main.dedServ)
			{
				Vector2 position = Projectile.Center + Projectile.velocity * (GetConfig<RapierConfiguration>().Reach - 12);

				if (info.DamageSource.TryGetCausingEntity(out Entity entity))
					position = entity.Center;

				float rotation = Projectile.AngleTo(position) + Main.rand.NextFloat(-1f, 1f);

				ParticleHandler.SpawnParticle(new ImpactLinePrim(position, Vector2.Zero, Color.PaleVioletRed.Additive() * 0.5f, new Vector2(0.5f, 1) * 2.5f, 5, 0) { Rotation = rotation, NoLight = true });
				ParticleHandler.SpawnParticle(new ImpactLinePrim(position, Vector2.Zero, Color.SteelBlue.Additive(), new Vector2(0.3f, 1) * 2, 5, 0) { Rotation = rotation, NoLight = true });
				ParticleHandler.SpawnParticle(new ImpactLinePrim(position, Vector2.Zero, Color.White.Additive(), new Vector2(0.3f, 1) * 1.5f, 5, 0) { Rotation = rotation, NoLight = true });
				ParticleHandler.SpawnParticle(new LightBurst(position, 0, Color.PaleVioletRed.Additive(), 0.4f, 10) { noLight = true });

				SoundEngine.PlaySound(SoundID.Research with { Pitch = 0.9f }, Projectile.Center);
				SoundEngine.PlaySound(SoundID.Item35, Projectile.Center);
			}

			SwingArc = 3; //Initiate a swing
			Counter = 0;

			Projectile.timeLeft++;
			Move = MoveType.Swing;

			Player owner = Main.player[Projectile.owner];
			owner.velocity -= Projectile.velocity * 8;
			owner.SetImmuneTimeForAllTypes(30);

			if (Projectile.owner == Main.myPlayer)
			{
				Projectile.velocity = Projectile.DirectionTo(Main.MouseWorld);
				Projectile.netUpdate = true;
			}

			return true;
		}

		public override float GetRotation(out float armRotation, out Player.CompositeArmStretchAmount stretch)
		{
			if (Move == MoveType.Stance)
			{
				float value = GetAbsoluteAngle();
				armRotation = value - MathHelper.PiOver2;
				stretch = Player.CompositeArmStretchAmount.Full;

				return value + ((Projectile.direction == -1) ? MathHelper.Pi + MathHelper.PiOver2 : MathHelper.Pi);
			}
			else
			{
				return base.GetRotation(out armRotation, out stretch);
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (hitSweetSpot)
			{
				for (int i = 0; i < 5; i++)
				{
					float magnitude = Main.rand.NextFloat();
					ParticleHandler.SpawnParticle(new EmberParticle(GetEndPosition(), Projectile.velocity.RotatedByRandom(0.5f) * magnitude * -5f, Color.PaleVioletRed, 0.4f * (1f - magnitude), 30, 3));
				}

				_motionCone?.SetColors(Color.White.Additive(100), Color.PaleVioletRed);
			}

			if (Move == MoveType.Swing)
				DuelistRose.ApplyEffect(Main.player[Projectile.owner], target, hit);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float offset = (Move == MoveType.Lunge) ? Math.Max(30 * (0.5f - Progress * 2), -2) : 0;
			float mult = 1f - Counter / 5f;

			DrawHeld(Projectile.GetAlpha(lightColor), new Vector2(0, TextureAssets.Projectile[Type].Value.Height) + new Vector2(-offset, offset), Projectile.rotation);

			if (Move == MoveType.Swing)
			{
				int direction = Projectile.spriteDirection * Math.Sign(SwingArc);
				SpriteEffects effects = (direction == -1) ? SpriteEffects.FlipVertically : default;
				float rotation = Projectile.rotation - MathHelper.PiOver4 - 0.5f * direction;

				DrawSmear(Projectile.GetAlpha(lightColor.MultiplyRGB(Color.PaleVioletRed)) * 0.5f, rotation, (int)(Progress * 8f), GetConfig<RapierConfiguration>().Reach + 10, effects: effects);
				DrawSmear(Projectile.GetAlpha(lightColor.MultiplyRGB(Color.LightGray)), rotation, (int)(Progress * 12f), GetConfig<RapierConfiguration>().Reach + 10, effects: effects);
				DrawSmear(Projectile.GetAlpha(lightColor.MultiplyRGB(Color.White)) * 0.7f * (1f - Progress), rotation, (int)(Progress * 15f), GetConfig<RapierConfiguration>().Reach + 12, effects: effects);
			}

			if (mult > 0)
				DrawStar(lightColor, 0.8f, mult);

			return false;
		}

		public override bool? CanDamage() => (Move == MoveType.Stance || Counter > 5) ? false : null;
	}

	public override void SetStaticDefaults() => SpiritSets.IsSword[Type] = true;

	public override void SetDefaults()
	{
		Item.DefaultToSpear(ModContent.ProjectileType<SilverRapierSwing>(), 1f, 18);
		Item.SetShopValues(ItemRarityColor.Blue1, Item.sellPrice(silver: 50));
		Item.damage = 14;
		Item.knockBack = 3;
		Item.UseSound = RapierProjectile.DefaultSwing;
		Item.autoReuse = true;
		MoRHelper.SetSlashBonus(Item);
	}

	public override bool AltFunctionUse(Player player) => true;

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		SilverRapierSwing.MoveType moveType = (player.altFunctionUse == 2) ? SilverRapierSwing.MoveType.Stance : SilverRapierSwing.MoveType.Lunge;
		SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, 0, source, (int)moveType);

		return false;
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips) => tooltips.RemoveAll(static x => x.Mod == "Terraria" && x.Name == "CritChance"); //Remove the line indicating crit chance

	public override void AddRecipes() => CreateRecipe().AddRecipeGroup(RecipeGroupID.Wood, 4).AddIngredient(ItemID.SilverBar, 6).AddTile(TileID.Anvils).Register();
}