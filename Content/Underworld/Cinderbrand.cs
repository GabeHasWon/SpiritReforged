using SpiritReforged.Common;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Content.Forest.Rapiers;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underworld;

public class Cinderbrand : ModItem
{
	public class CinderbrandSwing : RapierProjectile, FreeDodgePlayer.IFreeDodge
	{
		public enum MoveType { Lunge, Stance, Swing }

		public MoveType Move { get => (MoveType)Projectile.ai[0]; set => Projectile.ai[0] = (int)value; }
		public int FlourishDirection => (int)Projectile.ai[1];

		public override string Texture => ModContent.GetInstance<SilverRapier>().Texture;
		public override LocalizedText DisplayName => ModContent.GetInstance<SilverRapier>().DisplayName;

		private BasicNoiseCone _motionCone;

		public override IConfiguration SetConfiguration() => new RapierConfiguration(null, 58, 12, ParryStretch, 12, 15);

		public Player.CompositeArmStretchAmount ParryStretch()
		{
			if (Move == MoveType.Stance)
				return Player.CompositeArmStretchAmount.Full;
			else
				return ProgressiveStretch();
		}

		public override void AI()
		{
			base.AI();

			if (!Main.dedServ && Move == MoveType.Lunge && Counter == 1)
				ParticleHandler.SpawnParticle(_motionCone = (BasicNoiseCone)new BasicNoiseCone(Projectile.Center - Projectile.velocity * 8, Projectile.velocity, 20, new(50, 150)).SetColors(Color.White.Additive(100), Color.SteelBlue).SetIntensity(2).AttachTo(Projectile));
		}

		public bool FreeDodge(Player.HurtInfo info)
		{
			if (Move != MoveType.Stance || Counter > FreeDodgeTime)
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

				SoundEngine.PlaySound(SoundID.Research with { Pitch = 0.9f }, Projectile.Center);
				SoundEngine.PlaySound(SoundID.Item35, Projectile.Center);
			}

			SwingArc = 2; //Initiate a swing
			Counter = 0;

			Projectile.timeLeft++;
			Projectile.knockBack *= 3;
			Move = MoveType.Swing;

			Player owner = Main.player[Projectile.owner];
			owner.velocity -= Projectile.velocity * 5;
			owner.SetImmuneTimeForAllTypes(30);

			if (Projectile.owner == Main.myPlayer)
			{
				Projectile.velocity = Projectile.DirectionTo(Main.MouseWorld);
				Projectile.netUpdate = true;
			}

			return true;
		}

		public override float GetRotation(out float armRotation)
		{
			if (Move == MoveType.Stance)
			{
				float value = GetAbsoluteAngle();
				armRotation = value - MathHelper.PiOver2;

				return value + (Projectile.direction == -1 ? MathHelper.Pi + MathHelper.PiOver2 : MathHelper.Pi);
			}
			else
			{
				return base.GetRotation(out armRotation);
			}
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			base.ModifyHitNPC(target, ref modifiers);

			if (hitSweetSpot)
			{
				for (int i = 0; i < 5; i++)
				{
					float magnitude = Main.rand.NextFloat();
					ParticleHandler.SpawnParticle(new EmberParticle(GetEndPosition(), Projectile.velocity.RotatedByRandom(0.5f) * magnitude * -5f, Color.PaleVioletRed, 0.4f * (1f - magnitude), 30, 3));
				}

				_motionCone?.SetColors(Color.White.Additive(100), Color.PaleVioletRed);
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float offset = Move == MoveType.Lunge ? Math.Max(30 * (0.5f - Progress * 2), -2) : 0;
			float mult = 1f - Counter / 5f;

			DrawHeld(Projectile.GetAlpha(lightColor), new Vector2(0, TextureAssets.Projectile[Type].Value.Height) + new Vector2(-offset, offset), Projectile.rotation);

			if (Move == MoveType.Swing)
			{
				int direction = Projectile.spriteDirection * Math.Sign(SwingArc);
				SpriteEffects effects = direction == -1 ? SpriteEffects.FlipVertically : default;
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
		Item.damage = 14;
		Item.knockBack = 3;
		Item.useTime = Item.useAnimation = 18;
		Item.DamageType = DamageClass.Melee;
		Item.width = Item.height = 46;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.value = Item.sellPrice(gold: 1);
		Item.rare = ItemRarityID.Blue;
		Item.UseSound = RapierProjectile.DefaultSwing;
		Item.shoot = ModContent.ProjectileType<CinderbrandSwing>();
		Item.shootSpeed = 1f;
		Item.autoReuse = true;
		Item.useTurn = true;
		Item.noUseGraphic = true;
		Item.noMelee = true;
		MoRHelper.SetSlashBonus(Item);
	}

	public override bool AltFunctionUse(Player player) => true;

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		CinderbrandSwing.MoveType moveType = (player.altFunctionUse == 2) ? CinderbrandSwing.MoveType.Stance : CinderbrandSwing.MoveType.Lunge;
		SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, 0, source, (int)moveType);

		return false;
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips) => tooltips.RemoveAll(static x => x.Mod == "Terraria" && x.Name == "CritChance"); //Remove the line indicating crit chance

	public override void AddRecipes() => CreateRecipe().AddRecipeGroup(RecipeGroupID.Wood, 4).AddIngredient(ItemID.SilverBar, 6).AddTile(TileID.Anvils).Register();
}