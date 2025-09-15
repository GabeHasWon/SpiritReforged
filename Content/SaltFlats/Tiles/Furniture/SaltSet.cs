using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using Terraria.DataStructures;

namespace SpiritReforged.Content.SaltFlats.Tiles.Furniture;

public class SaltSet : FurnitureSet
{
	public override string Name => "Salt";
	public override FurnitureTile.IFurnitureData GetInfo(FurnitureTile tile) => new FurnitureTile.LightedInfo(tile.AutoModItem(), AutoContent.ItemType<SaltBlockDull>(), new(0.7f, 0.7f, 0.8f), DustID.Pearlsand);
	public override bool Autoload(FurnitureTile tile) => Excluding(tile, Types.Barrel, Types.Bench, Types.Clock, Types.Chandelier);
}

public class SaltClock : ClockTile
{
	private const int FrameHeight = 90;
	public override IFurnitureData Info => ModContent.GetInstance<SaltSet>().GetInfo(this);

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

public class SaltChandelier : ChandelierTile
{
	public override IFurnitureData Info => ModContent.GetInstance<SaltSet>().GetInfo(this);
	public override float Physics(Point16 topLeft) => 0;
}