using SpiritReforged.Common;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Rapiers;

public class SilverRapier : ModItem
{
	public class SilverRapierSwing : RapierProjectile
	{
		public int FlourishDirection => (int)Projectile.ai[1];

		public override string Texture => ModContent.GetInstance<SilverRapier>().Texture;
		public override LocalizedText DisplayName => ModContent.GetInstance<SilverRapier>().DisplayName;

		public static readonly SoundStyle Slash = new("SpiritReforged/Assets/SFX/Projectile/SwordSlash1");

		private BasicNoiseCone _motionCone;

		public override IConfiguration SetConfiguration() => new RapierConfiguration(EaseFunction.EaseCubicOut, 58, 12, ProgressiveStretch, 12);

		public override void AI()
		{
			base.AI();

			if (SecondaryUse)
			{
				Global.parryState = ParryPlayer.ParryState.Active;
			}
			else if (!Main.dedServ && SwingArc == 0 && Counter == 1)
			{
				ParticleHandler.SpawnParticle(_motionCone = (BasicNoiseCone)new BasicNoiseCone(Projectile.Center - Projectile.velocity * 8, Projectile.velocity, 20, new(50, 150)).SetColors(Color.White.Additive(100), Color.SteelBlue).SetIntensity(2).AttachTo(Projectile));
			}
		}

		public override void OnParry(Player.HurtInfo info)
		{
			SoundEngine.PlaySound(SoundID.Research with { Pitch = 0.5f }, Projectile.Center);
			SoundEngine.PlaySound(SoundID.Item21, Projectile.Center);

			SwingArc = 2;
			Counter = 0;

			Projectile.timeLeft++;
			Projectile.knockBack *= 3;
			SecondaryUse = false;

			Main.player[Projectile.owner].velocity -= Projectile.velocity * 5;

			if (Projectile.owner == Main.myPlayer)
			{
				Projectile.velocity = Projectile.DirectionTo(Main.MouseWorld);
				Projectile.netUpdate = true;
			}
		}

		public override float GetRotation(out float armRotation)
		{
			if (SecondaryUse)
			{
				float value = GetAbsoluteAngle() - MathHelper.PiOver4;
				armRotation = value - MathHelper.PiOver4;

				return value;
			}
			else
			{
				float flourishRotation = (Counter > SwingTime / 2) ? (Counter - SwingTime / 2) * 0.08f * FlourishDirection : 0;
				float easedRotation = EaseFunction.EaseCubicIn.Ease(flourishRotation);
				float value = base.GetRotation(out armRotation) + easedRotation;

				armRotation += easedRotation;
				return value;
			}
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			base.ModifyHitNPC(target, ref modifiers);

			if (hitSweetSpot)
			{
				SoundEngine.PlaySound(SoundID.DD2_CrystalCartImpact, target.Center);

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
			float offset = (!SecondaryUse && SwingArc == 0) ? Math.Max(30 * (0.5f - Progress * 2), -2) : 0;
			float mult = 1f - Counter / 5f;

			DrawHeld(Projectile.GetAlpha(lightColor), new Vector2(0, TextureAssets.Projectile[Type].Value.Height) + new Vector2(-offset, offset), Projectile.rotation);

			if (SwingArc != 0)
			{
				int direction = Projectile.spriteDirection * Math.Sign(SwingArc);
				SpriteEffects effects = (direction == -1) ? SpriteEffects.FlipVertically : default;
				float rotation = Projectile.rotation - MathHelper.PiOver4 - 0.5f * direction;

				DrawSmear(Projectile.GetAlpha(lightColor.MultiplyRGB(Color.PaleVioletRed)) * 0.5f, rotation, (int)(Progress * 8f), ((RapierConfiguration)Config).Reach + 10, effects: effects);
				DrawSmear(Projectile.GetAlpha(lightColor.MultiplyRGB(Color.LightGray)), rotation, (int)(Progress * 12f), ((RapierConfiguration)Config).Reach + 10, effects: effects);
				DrawSmear(Projectile.GetAlpha(lightColor.MultiplyRGB(Color.White)) * 0.7f * (1f - Progress), rotation, (int)(Progress * 15f), ((RapierConfiguration)Config).Reach + 12, effects: effects);
			}
			else if (!SecondaryUse && mult > 0)
			{
				DrawStar(lightColor, 0.8f, mult);
			}

			return false;
		}
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
		Item.UseSound = SoundID.Item1;
		Item.shoot = ModContent.ProjectileType<SilverRapierSwing>();
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
		if (player.altFunctionUse != 2)
			SoundEngine.PlaySound(SilverRapierSwing.Slash with { Pitch = 1f, PitchVariance = 0.15f });

		SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, 0, source, player.altFunctionUse - 1, Main.rand.NextFromList(-1, 1));
		return false;
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips) => tooltips.RemoveAll(static x => x.Mod == "Terraria" && x.Name == "CritChance"); //Remove the line indicating crit chance

	public override void AddRecipes() => CreateRecipe().AddRecipeGroup(RecipeGroupID.Wood, 4).AddIngredient(ItemID.SilverBar, 6).AddTile(TileID.Anvils).Register();
}