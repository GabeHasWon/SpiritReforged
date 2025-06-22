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
	public override Color[] Colors => [Color.Violet, Color.BlueViolet, Color.DarkViolet];

	public override void OnClientSpawn(bool doDustSpawn)
	{
		base.OnClientSpawn(false);

		if (Main.dedServ)
			return;

		for (int i = 0; i < 4; i++)
		{
			float mag = Main.rand.NextFloat(0.33f, 1);
			var velocity = (Projectile.velocity * mag).RotatedByRandom(0.2f);

			if (Main.rand.NextBool())
				ParticleHandler.SpawnParticle(new MagicParticle(Projectile.Center, velocity * 0.75f, Colors[0], Main.rand.NextFloat(0.1f, 1f), Main.rand.Next(20, 100)));

			Vector2 cloudPos = Projectile.Center + Vector2.Normalize(Projectile.velocity) * 10;
			var fireCloud = new SmokeCloud(cloudPos, velocity, Colors[0].Additive(), Main.rand.NextFloat(0.08f, 0.12f), EaseFunction.EaseCubicOut, Main.rand.Next(40, 60), false)
			{
				SecondaryColor = Colors[1].Additive(),
				TertiaryColor = Colors[2].Additive(),
				Intensity = 0.15f,
				Pixellate = true
			};

			ParticleHandler.SpawnParticle(fireCloud);

			var smokeCloud = new SmokeCloud(fireCloud.Position, fireCloud.Velocity * 1.25f, Color.Lerp(Color.Gray, Colors[0], 0.4f), fireCloud.Scale * 1.25f, EaseFunction.EaseCubicOut, Main.rand.Next(40, 70))
			{
				SecondaryColor = Color.Lerp(Color.DarkSlateGray, Colors[1], 0.4f),
				TertiaryColor = Color.Lerp(Color.Black, Colors[2], 0.4f),
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

	public override void OnKill(int timeLeft)
	{
		base.OnKill(timeLeft);

		var lineScale = new Vector2(1f, 2f) * Projectile.scale * Main.rand.NextFloat(0.5f, 1.1f);
		ParticleHandler.SpawnParticle(new ImpactLine(Projectile.Center, Vector2.Zero, Colors[0].Additive() * 0.5f, lineScale, 15));
		ParticleHandler.SpawnParticle(new ImpactLine(Projectile.Center, Vector2.Zero, Color.White.Additive() * 0.5f, lineScale * 0.5f, 15));
	}

	public override void SpawnDust(Vector2 origin) => Dust.NewDustPerfect(origin, DustID.PurpleCrystalShard, Projectile.velocity * 0.5f).noGravity = true;
	public override void PlayDeathSound()
	{
		SoundEngine.PlaySound(SoundID.DD2_LightningBugZap with { PitchRange = (1f, 1.5f), Volume = 0.6f, MaxInstances = 5 }, Projectile.Center);
		SoundEngine.PlaySound(SoundID.DD2_DarkMageHealImpact with { Pitch = 0.9f }, Projectile.Center);

		SoundEngine.PlaySound(Impact with { Pitch = 0.9f, Volume = 0.4f }, Projectile.Center);
	}
}