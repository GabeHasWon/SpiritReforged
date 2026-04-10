using SpiritReforged.Common;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Content.Particles;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Rapiers;

public class FencingFoil : ModItem
{
	public class FencingFoilSwing : RapierProjectile
	{
		public int FlourishDirection => (int)Projectile.ai[1];

		public override string Texture => ModContent.GetInstance<FencingFoil>().Texture;
		public override LocalizedText DisplayName => ModContent.GetInstance<FencingFoil>().DisplayName;

		private BasicNoiseCone _motionCone;

		public override IConfiguration SetConfiguration() => new RapierConfiguration(EaseFunction.EaseCubicOut, 58, 12, ProgressiveStretch, 12, 0);

		public override void AI()
		{
			base.AI();

			if (!Main.dedServ && Counter == 1)
				ParticleHandler.SpawnParticle(_motionCone = (BasicNoiseCone)new BasicNoiseCone(Projectile.Center - Projectile.velocity * 8, Projectile.velocity, 20, new(50, 150)).SetColors(Color.White.Additive(100), Color.Gray).SetIntensity(2).AttachTo(Projectile));
		}

		public override float GetRotation(out float armRotation)
		{
			float flourishRotation = (Counter > SwingTime / 2) ? (Counter - SwingTime / 2) * 0.06f * FlourishDirection : 0;
			float easedRotation = EaseFunction.EaseCubicIn.Ease(flourishRotation);
			float value = base.GetRotation(out armRotation) + easedRotation;

			armRotation += easedRotation;
			return value;
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			base.ModifyHitNPC(target, ref modifiers);

			if (!Main.dedServ && hitSweetSpot)
			{
				/*for (int i = 0; i < 5; i++)
				{
					float magnitude = Main.rand.NextFloat();
					var dust = Dust.NewDustPerfect(GetEndPosition(), DustID.Torch, Projectile.velocity.RotatedByRandom(0.5f) * magnitude * -5f, 0, default, 1.5f);
					dust.noGravity = true;
					dust.noLight = true;
				}*/

				_motionCone?.SetColors(Color.White.Additive(100), new(255, 185, 62));
				for (int i = 0; i < 5; i++)
				{
					float magnitude = Main.rand.NextFloat();
					ParticleHandler.SpawnParticle(new EmberParticle(GetEndPosition(), Projectile.velocity.RotatedByRandom(0.5f) * magnitude * -5f, Color.Goldenrod, 0.4f * (1f - magnitude), 30, 3));
				}
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			base.PreDraw(ref lightColor);

			float mult = 1f - Counter / 5f;
			if (mult > 0)
				DrawStar(lightColor, 0.8f, mult);

			return false;
		}
	}

	public override void SetStaticDefaults() => SpiritSets.IsSword[Type] = true;

	public override void SetDefaults()
	{
		Item.DefaultToSpear(ModContent.ProjectileType<FencingFoilSwing>(), 1f, 18);
		Item.SetShopValues(ItemRarityColor.Blue1, Item.sellPrice(silver: 25));
		Item.damage = 10;
		Item.knockBack = 2;
		Item.UseSound = RapierProjectile.DefaultSwing;
		Item.autoReuse = true;
		MoRHelper.SetSlashBonus(Item);
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, 0, source, 0, Main.rand.NextFromList(-1, 1));
		return false;
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips) => tooltips.RemoveAll(static x => x.Mod == "Terraria" && x.Name == "CritChance"); //Remove the line indicating crit chance
}