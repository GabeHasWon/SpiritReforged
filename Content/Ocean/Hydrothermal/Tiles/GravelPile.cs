using RubbleAutoloader;
using SpiritReforged.Common.ItemCommon;
using Terraria.DataStructures;
using TileHelper.Common;

namespace SpiritReforged.Content.Ocean.Hydrothermal.Tiles;

public class GravelPile : ModTile, IAutoloadRubble
{
	public IAutoloadRubble.RubbleData Data => new(AutoContent.ItemType<Gravel>(), IAutoloadRubble.RubbleSize.Medium);

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileMergeDirt[Type] = false;
		Main.tileBlockLight[Type] = false;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoFail[Type] = true;

		TileID.Sets.BreakableWhenPlacing[Type] = true;
		Sets.TileGlowmask[Type] = Helpers.RequestGlowmask(this, Magmastone.GetGlowColor);

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = 6;
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(90, 90, 90));
		DustType = DustID.Asphalt;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;
}