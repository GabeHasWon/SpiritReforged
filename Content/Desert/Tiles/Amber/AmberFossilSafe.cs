using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;

namespace SpiritReforged.Content.Desert.Tiles.Amber;

/// <summary> A placeable amber fossil. </summary>
public class AmberFossilSafe : AmberFossil, IAutoloadTileItem
{
	public void AddItemRecipes(ModItem item)
	{
		StartRecipe().AddRecipeGroup(RecipeGroupID.Fireflies).Register();
		StartRecipe().AddRecipeGroup(RecipeGroupID.Dragonflies).Register();
		StartRecipe().AddIngredient(ItemID.Grasshopper).Register();
		StartRecipe().AddIngredient(ItemID.Frog).Register();

		Recipe StartRecipe() => item.CreateRecipe(10).AddIngredient(AutoContent.ItemType<PolishedAmber>(), 10);
	}

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		RegisterItemDrop(this.AutoItemType());
		this.AutoItem().ResearchUnlockCount = 100;
	}

	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (!effectOnly && !fail)
			ModContent.GetInstance<FossilEntity>().Kill(i, j);
	}
}