
namespace SpiritReforged.Content.Ocean.Items.Driftwood;

public class DriftwoodDoorItem : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 32;
		Item.height = 28;
		Item.value = 500;
		Item.maxStack = Item.CommonMaxStack;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.useTime = 10;
		Item.useAnimation = 15;
		Item.useTurn = true;
		Item.autoReuse = true;
		Item.consumable = true;
		//Item.createTile = ModContent.TileType<DriftwoodDoorClosed>();
	}

	public override void AddRecipes()
	{
		Recipe recipe = CreateRecipe();
		recipe.AddIngredient(ModContent.ItemType<DriftwoodTileItem>(), 8);
		recipe.AddTile(TileID.WorkBenches);
		recipe.Register();
	}
}