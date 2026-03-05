namespace SpiritReforged.Content.Forest.MagicPowder;

public class VexpowderRed : Flarepowder
{
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<VexpowderBlue>();
	}

	public override void SetDefaults()
	{
		base.SetDefaults();
		Item.shoot = ModContent.ProjectileType<VexpowderRedDust>();
		Item.damage = 10;
		Item.crit = 2;
		Item.shootSpeed = 6.2f;
		Item.value = Item.sellPrice(copper: 7);
	}

	public override void AddRecipes()
	{
		CreateRecipe(25).AddIngredient(ModContent.ItemType<Flarepowder>(), 25).AddIngredient(ItemID.ViciousMushroom).Register();
		CreateRecipe(25).AddIngredient(ModContent.ItemType<Flarepowder>(), 25).AddIngredient(ItemID.ViciousPowder, 5).Register();
	}
}

internal class VexpowderRedDust : VexpowderBlueDust
{
	public override Color[] Colors => [new Color(255, 230, 180), Color.DarkGoldenrod, new Color(205, 52, 52), Color.DarkOrange, Color.Magenta * 0.5f];
	public override void SpawnDust(Vector2 origin) => Dust.NewDustPerfect(origin, DustID.RedTorch, Projectile.velocity * 0.5f, Scale: Main.rand.NextFloat(0.5f, 1.2f)).noGravity = true;
}