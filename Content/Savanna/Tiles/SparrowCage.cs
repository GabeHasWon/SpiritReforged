using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;

namespace SpiritReforged.Content.Savanna.Tiles;

public class SparrowCage : CageTile, IAutoloadTileItem
{
	public override int NumFrames => 35;
	public override void AddObjectData()
	{
		TileObjectData.newTile.CopyFrom(TileObjectData.GetTileData(TileID.BirdCage, 0));
		TileObjectData.addTile(Type);

		AnimationFrameHeight = 54;
	}

	public override void AnimateCage(ref int frame, ref int frameCounter)
	{
		if (++frameCounter >= 10)
		{
			frameCounter = 0;

			if (frame is 0 or 6 or 11 or 19 && !Main.rand.NextBool(12))
				return;

			frame = ++frame % NumFrames;
		}
	}
}