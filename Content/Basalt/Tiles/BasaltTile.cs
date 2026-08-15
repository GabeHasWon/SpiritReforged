using TileHelper.Common;

namespace SpiritReforged.Content.Basalt.Tiles;

public class BasaltTile : ModTile, ILoadItem
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;

		AddMapEntry(new Color(60, 45, 40));
		DustType = DustID.DarkCelestial;
	}
}