using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;

namespace SpiritReforged.Content.Forest.MagicPowder;

public class VexpowderBlue : Flarepowder
{
	public override void SetDefaults()
	{
		base.SetDefaults();
		Item.shoot = ModContent.ProjectileType<VexpowderBlueDust>();
		Item.damage = 10;
	}

	public override void AddRecipes() => CreateRecipe(25).AddIngredient(ModContent.ItemType<Flarepowder>(), 25).AddIngredient(ItemID.VileMushroom).Register();
}

internal class VexpowderBlueDust : FlarepowderDust
{
	public override Color[] Colors => [Color.Violet, Color.BlueViolet, Color.Goldenrod];

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.Confused, 600);
	public override void OnKill(int timeLeft)
	{
		base.OnKill(timeLeft);

		var lineScale = new Vector2(1f, 2f) * Projectile.scale * Main.rand.NextFloat(0.5f, 1.1f);
		ParticleHandler.SpawnParticle(new ImpactLine(Projectile.Center, Vector2.Zero, Colors[0].Additive() * 0.5f, lineScale, 15));
		ParticleHandler.SpawnParticle(new ImpactLine(Projectile.Center, Vector2.Zero, Color.White.Additive() * 0.5f, lineScale * 0.5f, 15));
	}

	public override void SpawnDust(Vector2 origin) => Dust.NewDustPerfect(origin, DustID.PurpleCrystalShard, Projectile.velocity * 0.5f).noGravity = true;
}