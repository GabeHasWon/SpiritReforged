namespace SpiritReforged.Content.Forest.MagicPowder;

public class VexpowderRed : Flarepowder
{
	public override void SetDefaults()
	{
		base.SetDefaults();
		Item.shoot = ModContent.ProjectileType<VexpowderRedDust>();
		Item.damage = 10;
	}

	public override void AddRecipes() => CreateRecipe(25).AddIngredient(ModContent.ItemType<Flarepowder>(), 25).AddIngredient(ItemID.ViciousMushroom);
}

internal class VexpowderRedDust : VexpowderBlueDust
{
	public override Color[] Colors => [Color.Red, Color.OrangeRed, Color.Goldenrod];
}