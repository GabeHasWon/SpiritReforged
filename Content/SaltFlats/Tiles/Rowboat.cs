using RubbleAutoloader;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using Terraria.DataStructures;

namespace SpiritReforged.Content.SaltFlats.Tiles;

public class Rowboat : ModTile, IAutoloadRubble
{
	public IAutoloadRubble.RubbleData Data => new(AutoContent.ItemType<SaltBlockDull>(), IAutoloadRubble.RubbleSize.Large);

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileMergeDirt[Type] = false;
		Main.tileBlockLight[Type] = false;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoFail[Type] = true;

		TileID.Sets.BreakableWhenPlacing[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style5x4);
		TileObjectData.newTile.Height = 2;
		TileObjectData.newTile.CoordinateHeights = [16, 16];
		TileObjectData.newTile.Origin = new(3, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = 10;
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(190, 150, 150));
		DustType = DustID.Pearlwood;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;
}