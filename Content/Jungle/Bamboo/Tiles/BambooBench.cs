using SpiritReforged.Common.ItemCommon;
using TileHelper.Common;
using TileHelper.Content.Tiles;

namespace SpiritReforged.Content.Jungle.Bamboo.Tiles;

public class BambooBench : BenchTile, ICreateItem
{
	public void AddItemRecipes(ModItem modItem) => DataStructures.Recipes[FurnitureName]?.Invoke(modItem, AutoContent.ItemType<StrippedBamboo>());
}