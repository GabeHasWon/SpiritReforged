using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Forest.MagicPowder;

public class VexpowderBlue : Flarepowder
{
	public override void SetDefaults()
	{
		base.SetDefaults();
		Item.shoot = ModContent.ProjectileType<VexpowderBlueDust>();
		Item.damage = 10;
		Item.crit = 2;
		Item.shootSpeed = 6.2f;
	}

	public override void AddRecipes() => CreateRecipe(25).AddIngredient(ModContent.ItemType<Flarepowder>(), 25).AddIngredient(ItemID.VileMushroom).Register();
}

internal class VexpowderBlueDust : FlarepowderDust
{
	public override Color[] Colors => [Color.Violet, Color.BlueViolet, Color.Goldenrod];

	public override void SetDefaults()
	{
		base.SetDefaults();
		randomTimeLeft = (0.2f, 0.4f);
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
	}
}