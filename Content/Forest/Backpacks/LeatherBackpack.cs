using SpiritReforged.Common.ItemCommon.Backpacks;

namespace SpiritReforged.Content.Forest.Backpacks;

[AutoloadEquip(EquipType.Back, EquipType.Front)]
public class LeatherBackpack : BackpackItem
{
	public override void SetDefaults()
	{
		Item.Size = new Vector2(38, 30);
		Item.value = Item.buyPrice(0, 0, 5, 0);
		Item.rare = ItemRarityID.Blue;

		slotCount = 4;
	}

	public override void AddRecipes() => CreateRecipe()
		.AddIngredient(ItemID.Leather, 10)
		.AddRecipeGroup("SilverBars")
		.AddTile(TileID.WorkBenches)
		.Register();
}
