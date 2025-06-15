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

	public override void SetDefaults()
	{
		base.SetDefaults();
		randomTimeLeft = (0.2f, 0.4f);
	}

	public override void OnClientSpawn(bool doDustSpawn)
	{
		base.OnClientSpawn(false);

		for (int i = 0; i < 4; i++)
		{
			float mag = Main.rand.NextFloat();
			var velocity = (Projectile.velocity * mag).RotatedByRandom(0.4f) * 1.33f;

			if(Main.rand.NextBool())
				ParticleHandler.SpawnParticle(new MagicParticle(Projectile.Center, velocity * 0.375f, Colors[0], Main.rand.NextFloat(0.1f, 1f), Main.rand.Next(20, 200)));

			var fireCloud = new SmokeCloud(Projectile.Center + Vector2.Normalize(Projectile.velocity) * 10, velocity, Colors[0].Additive(), Main.rand.NextFloat(0.05f, 0.1f), EaseFunction.EaseCubicOut, Main.rand.Next(40, 60));
			fireCloud.SecondaryColor = Colors[1].Additive();
			fireCloud.TertiaryColor = Colors[2].Additive();
			fireCloud.ColorLerpExponent = 0.8f;
			fireCloud.Intensity = 0.3f;
			fireCloud.UseLightColor = false;
			ParticleHandler.SpawnParticle(fireCloud);
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