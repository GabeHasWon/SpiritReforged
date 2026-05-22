using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;

namespace SpiritReforged.Content.Forest.Misc;

public class GoldCricketCage : CricketCage, IAutoloadTileItem
{
	public override void AddItemRecipes(ModItem item) => item.CreateRecipe().AddIngredient(ItemID.Terrarium).AddIngredient(AutoContent.ItemType<GoldCricket>()).Register();

	public override void AddObjectData()
	{
		TileObjectData.newTile.CopyFrom(TileObjectData.GetTileData(TileID.GrasshopperCage, 0));
		TileObjectData.addTile(Type);

		TileID.Sets.CritterCageLidStyle[Type] = 4;
		AnimationFrameHeight = 36;
	}
}