using SpiritReforged.Common;
using SpiritReforged.Common.ItemCommon;
using TileHelper.Common;
using TileHelper.Content.Tiles;

namespace SpiritReforged.Content.Ziggurat.Tiles;

public class BronzeGrateDoor : DoorTile, ILoadItem
{
	public void AddItemRecipes(ModItem modItem) => DataStructures.Recipes[FurnitureName]?.Invoke(modItem, AutoContent.ItemType<BronzePlating>());

	public override void SetStaticDefaults()
	{
		SpiritSets.AllowsLiquid[Type] = true;
		TileID.Sets.BlocksWaterDrawingBehindSelf[Type] = false;

		base.SetStaticDefaults();
	}
}