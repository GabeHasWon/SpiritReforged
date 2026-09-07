using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Jungle.Misc;

public class HiveSmasher : ClubItem
{
	public class HiveSmasherProj : BaseClubProj
	{
		public HiveSmasherProj() : base(new Vector2(88, 92)) { }

		public override float WindupTimeRatio => 0.8f;

		public override void OnSwingStart()
		{
			if (!Main.dedServ)
				CreateTrail(TrailSystem.ProjectileRenderer);
		}

		public void CreateTrail(ProjectileTrailRenderer renderer)
		{
			float trailDist = 52 * MeleeSizeModifier;
			float trailWidth = 60 * MeleeSizeModifier;
			float angleRangeMod = 1f;
			float rotOffset = 0;

			if (FullCharge)
			{
				trailDist *= 1.1f;
				trailWidth *= 1.1f;
				angleRangeMod = 1.2f;
				rotOffset = -MathHelper.PiOver4 / 2;
			}

			SwingTrailParameters parameters = new(AngleRange * angleRangeMod, -HoldAngle_Final + rotOffset, trailDist, trailWidth)
			{
				Color = Color.White,
				SecondaryColor = Color.SandyBrown,
				TrailLength = 0.33f,
				Intensity = 0.75f,
			};

			renderer.CreateTrail(Projectile, new SwingTrail(Projectile, parameters, GetSwingProgressStatic, SwingTrail.BasicSwingShaderParams));
		}

		public override void OnSmash(Vector2 position)
		{
			TrailSystem.ProjectileRenderer.DissolveTrail(Projectile);
			Collision.HitTiles(Projectile.position, Vector2.UnitY, Projectile.width, Projectile.height);
			Vector2 top = Projectile.Top - new Vector2(0, 10);

			DustClouds(4);

			if (!Main.dedServ)
			{
				for (int i = 1; i < 4; i++)
					Gore.NewGoreDirect(Projectile.GetSource_FromAI(), top, Projectile.velocity, Mod.Find<ModGore>("Hive" + i).Type);

				for (int i = 1; i < Math.Clamp(Charge * 25, 5, 25); i++)
					Dust.NewDustDirect(Projectile.Bottom - new Vector2(10, 0), 20, 2, DustID.Honey2, 0, -1);
			}

			if (Main.myPlayer == Projectile.owner)
			{
				for (int i = 0; i < (FullCharge ? 5 : 3); i++)
					Projectile.NewProjectile(Projectile.GetSource_FromAI(), top, Projectile.velocity * Main.rand.NextFloat(-1f, 1f), ProjectileID.Wasp, Projectile.damage / 2, 1, Projectile.owner);
			}

			SoundEngine.PlaySound(SoundID.DD2_SkeletonHurt with { Pitch = 0.5f }, Projectile.Center);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			var position = Vector2.Lerp(Projectile.Center, target.Center, 0.6f);

			for (int i = 0; i < (FullCharge ? 3 : 1); i++)
			{
				ParticleHandler.SpawnParticle(new CompositeSmoke(position + Main.rand.NextVector2Circular(30, 30), -Vector2.UnitY, Color.SaddleBrown, 60, false, false));
				ParticleHandler.SpawnParticle(new SmallCompositeSmoke(position + Main.rand.NextVector2Circular(30, 30), -Vector2.UnitY, Color.SandyBrown, 60, false, false));
			}
		}
	}

	internal override float DamageScaling => 2f;
	internal override float KnockbackScaling => 1.4f;

	public override void SafeSetDefaults()
	{
		Item.damage = 90;
		Item.knockBack = 8;
		Item.width = 60;
		Item.height = 60;
		Item.crit = 4;
		Item.value = Item.sellPrice(0, 3, 50, 0);
		Item.rare = ItemRarityID.LightPurple;
		Item.shoot = ModContent.ProjectileType<HiveSmasherProj>();

		ChargeTime = 30;
		SwingTime = 24;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.SoulofMight, 10).AddIngredient(ItemID.BeeWax, 14).AddIngredient(ItemID.Stinger, 8).AddTile(TileID.MythrilAnvil).Register();
}