using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Forest.MagicPowder;

public class VexpowderBlue : Flarepowder
{
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<VexpowderRed>();
	}

	public override void SetDefaults()
	{
		base.SetDefaults();
		Item.shoot = ModContent.ProjectileType<VexpowderBlueDust>();
		Item.damage = 10;
		Item.crit = 2;
		Item.shootSpeed = 6.2f;
		Item.value = Item.sellPrice(copper: 7);
	}

	public override void AddRecipes()
	{
		CreateRecipe(25).AddIngredient(ModContent.ItemType<Flarepowder>(), 25).AddIngredient(ItemID.VileMushroom).Register();
		CreateRecipe(25).AddIngredient(ModContent.ItemType<Flarepowder>(), 25).AddIngredient(ItemID.VilePowder, 5).Register();
	}
}

internal class VexpowderBlueDust : FlarepowderDust
{
	public override Color[] Colors => [Color.LightGoldenrodYellow, Color.Violet, Color.BlueViolet, Color.Violet, Color.DarkViolet];

	public override void OnClientSpawn(bool doDustSpawn)
	{
		base.OnClientSpawn(false);

		if (Main.dedServ)
			return;

		for (int i = 0; i < 3; i++)
		{
			float mag = Main.rand.NextFloat(0.33f, 1);
			var velocity = (Projectile.velocity * mag).RotatedByRandom(0.2f);

			if (Main.rand.NextBool())
				ParticleHandler.SpawnParticle(new MagicParticle(Projectile.Center, velocity * 0.75f, Colors[1], Main.rand.NextFloat(0.1f, 1f), Main.rand.Next(20, 100)));

			Vector2 cloudPos = Projectile.Center + Vector2.Normalize(Projectile.velocity) * 10;
			var fireCloud = new SmokeCloud(cloudPos, velocity, Colors[1].Additive(), Main.rand.NextFloat(0.08f, 0.12f), EaseFunction.EaseCubicOut, Main.rand.Next(40, 60), false)
			{
				SecondaryColor = Colors[2].Additive(),
				TertiaryColor = Colors[4].Additive(),
				Intensity = 0.15f,
				Pixellate = true
			};

			ParticleHandler.SpawnParticle(fireCloud);

			var smokeCloud = new SmokeCloud(fireCloud.Position, fireCloud.Velocity * 1.25f, Color.Lerp(Color.Gray, Colors[1], 0.4f), fireCloud.Scale * 1.25f, EaseFunction.EaseCubicOut, Main.rand.Next(40, 70))
			{
				SecondaryColor = Color.Lerp(Color.DarkSlateGray, Colors[2], 0.4f),
				TertiaryColor = Color.Lerp(Color.Black, Colors[4], 0.4f),
				ColorLerpExponent = 2,
				Intensity = 0.33f,
				Layer = ParticleLayer.BelowProjectile,
				Pixellate = true
			};
			ParticleHandler.SpawnParticle(smokeCloud);
		}
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (Main.rand.NextBool(3))
			target.AddBuff(BuffID.Confused, 150);
	}

	public override void SpawnDust(Vector2 origin) => Dust.NewDustPerfect(origin, DustID.PurpleCrystalShard, Projectile.velocity * 0.5f).noGravity = true;
	public override void DoDeathEffects()
	{
		SoundEngine.PlaySound(SoundID.DD2_LightningBugZap with { Volume = 0.4f, PitchRange = (1f, 1.5f), MaxInstances = 5 }, Projectile.Center);
		SoundEngine.PlaySound(SoundID.DD2_DarkMageHealImpact with { Volume = 0.2f, Pitch = 0.9f }, Projectile.Center);

		SoundEngine.PlaySound(Impact with { Volume = 0.3f, Pitch = 0.9f }, Projectile.Center);

		float angle = Main.rand.NextFloat(MathHelper.Pi);

		var circle = new TexturedPulseCircle(Projectile.Center, (Colors[3] * 0.5f).Additive(), 2, 42, 20, "Bloom", new Vector2(1), EaseFunction.EaseCircularOut);
		circle.Angle = angle;
		ParticleHandler.SpawnParticle(circle);

		var circle2 = new TexturedPulseCircle(Projectile.Center, (Colors[0] * 0.5f).Additive(), 1, 40, 20, "Bloom", new Vector2(1), EaseFunction.EaseCircularOut);
		circle2.Angle = angle;
		ParticleHandler.SpawnParticle(circle2);

		ParticleHandler.SpawnParticle(new StarParticle(Projectile.Center, Vector2.Zero, Colors[2].Additive() * 0.5f, Colors[2].Additive() * 0.5f, 0.4f, 15, 0.1f));
		ParticleHandler.SpawnParticle(new StarParticle(Projectile.Center, Vector2.Zero, Color.White.Additive() * 0.5f, Colors[2].Additive() * 0.5f, 0.2f, 15));
	}
}