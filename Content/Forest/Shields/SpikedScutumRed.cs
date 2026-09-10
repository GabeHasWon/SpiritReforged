namespace SpiritReforged.Content.Forest.Shields;

public class SpikedScutumRed : SpikedScutumPurple
{
	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.CrimtaneBar, 5).AddIngredient(ItemID.Shadewood, 18).AddTile(TileID.Anvils).Register();
}