using SpiritReforged.Common.ItemCommon.Backpacks;

namespace SpiritReforged.Content.Forest.Backpacks;

[AutoloadEquip(EquipType.Back, EquipType.Front)]
internal class LeatherBackpack : BackpackItem
{
	protected override int SlotCap => 4;

	public override void Defaults()
	{
		Item.Size = new Vector2(38, 30);
		Item.value = Item.buyPrice(0, 0, 5, 0);
		Item.rare = ItemRarityID.Blue;
	}

	public override void AddRecipes() => CreateRecipe()
		.AddIngredient(ItemID.Leather, 10)
		.AddRecipeGroup("SilverBars")
		.AddTile(TileID.WorkBenches)
		.Register();
}
