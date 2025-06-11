namespace SpiritReforged.Content.Forest.MagicPowder;

public class VexpowderRed : Flarepowder
{
	public override void SetDefaults()
	{
		base.SetDefaults();
		Item.shoot = ModContent.ProjectileType<VexpowderRedDust>();
		Item.damage = 10;
		Item.crit = 2;
		Item.shootSpeed = 6.2f;
	}

	public override void AddRecipes() => CreateRecipe(25).AddIngredient(ModContent.ItemType<Flarepowder>(), 25).AddIngredient(ItemID.ViciousMushroom).Register();
}

internal class VexpowderRedDust : VexpowderBlueDust
{
	public override Color[] Colors => [Color.Red, Color.OrangeRed, Color.Goldenrod];
	public override void SpawnDust(Vector2 origin) => Dust.NewDustPerfect(origin, DustID.RedTorch, Projectile.velocity * 0.5f, Scale: Main.rand.NextFloat(0.5f, 1.2f)).noGravity = true;
}