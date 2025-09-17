using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.Savanna.NPCs;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Content.Savanna.Tiles;

public class Jarboa : CageTile, IAutoloadTileItem
{
	public override int NumFrames => 24;

	public void AddItemRecipes(ModItem item) => item.CreateRecipe().AddIngredient(ItemID.Bottle).AddIngredient(AutoContent.ItemType<Jerboa>()).Register();

	public override void AddObjectData()
	{
		TileObjectData.newTile.CopyFrom(TileObjectData.GetTileData(TileID.BlackDragonflyJar, 0));
		TileObjectData.addTile(Type);

		Main.tileTable[Type] = false;
		TileID.Sets.CritterCageLidStyle[Type] = -1;

		AnimationFrameHeight = 36;
	}

	public override int GetFrameIndex(int i, int j, int frameX, int frameY) => TileDrawing.GetSmallAnimalCageFrame(i, j, frameX, frameY);
	public override void AnimateCage(ref int frame, ref int frameCounter)
	{
		if (++frameCounter >= 10)
		{
			frameCounter = 0;

			if (frame is 10 or 0 && !Main.rand.NextBool(12))
				return;

			frame = ++frame % NumFrames;
		}
	}
}