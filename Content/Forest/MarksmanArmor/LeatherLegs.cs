namespace SpiritReforged.Content.Forest.MarksmanArmor;

[AutoloadEquip(EquipType.Legs)]
public class LeatherLegs : ModItem
{

	public override void SetDefaults()
	{
		Item.width = 22;
		Item.height = 18;
		Item.value = 3500;
		Item.rare = ItemRarityID.Blue;
		Item.defense = 1;
	}

	public override void AddRecipes()
	{
		Recipe recipe = CreateRecipe();
		recipe.AddIngredient(ItemID.Leather, 7);
		recipe.AddIngredient(ItemID.IronBar, 2);
		recipe.AddTile(TileID.Anvils);
		recipe.Register();
	}
}
