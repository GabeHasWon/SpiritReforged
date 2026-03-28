using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;

namespace SpiritReforged.Content.Forest.Misc;

public class CricketCage : CageTile, IAutoloadTileItem
{
	public override int NumFrames => 9;

	public virtual void AddItemRecipes(ModItem item) => item.CreateRecipe().AddIngredient(ItemID.Terrarium).AddIngredient(AutoContent.ItemType<Cricket>()).Register();

	public override void AddObjectData()
	{
		TileObjectData.newTile.CopyFrom(TileObjectData.GetTileData(TileID.GrasshopperCage, 0));
		TileObjectData.addTile(Type);

		TileID.Sets.CritterCageLidStyle[Type] = 3;
		AnimationFrameHeight = 36;
	}

	public override void AnimateCage(ref int frame, ref int frameCounter)
	{
		if (++frameCounter >= 10)
		{
			frameCounter = 0;
			frame = ++frame % NumFrames;
		}
	}
}