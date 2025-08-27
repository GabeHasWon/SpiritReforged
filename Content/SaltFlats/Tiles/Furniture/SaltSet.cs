using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;

namespace SpiritReforged.Content.SaltFlats.Tiles.Furniture;

public class SaltSet : FurnitureSet
{
	public override string Name => "Salt";
	public override FurnitureTile.IFurnitureData GetInfo(FurnitureTile tile) => new FurnitureTile.LightedInfo(tile.AutoModItem(), AutoContent.ItemType<SaltBlockDull>(), new(0.5f, 0.5f, 0.5f), DustID.Pearlsand);
}

public class SaltClock : ClockTile
{
	private const int FrameHeight = 90;
	public override IFurnitureData Info => new BasicInfo(this.AutoModItem(), AutoContent.ItemType<SaltBlockDull>());

	public override void StaticDefaults()
	{
		base.StaticDefaults();
		AnimationFrameHeight = FrameHeight;
	}

	public override void AnimateTile(ref int frame, ref int frameCounter)
	{
		if (++frameCounter >= 4)
		{
			frameCounter = 0;
			frame = ++frame % 5;
		}
	}
}