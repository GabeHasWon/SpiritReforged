using SpiritReforged.Common.TileCommon;

namespace SpiritReforged.Content.SaltFlats.Tiles;

public class CobbledBrick : ModTile, IAutoloadTileItem
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;
		Main.tileBlendAll[Type] = true;

		AddMapEntry(new Color(140, 140, 140));
		DustType = DustID.Stone;
	}
}