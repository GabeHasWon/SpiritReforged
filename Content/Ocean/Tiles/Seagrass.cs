using Terraria.DataStructures;

namespace SpiritReforged.Content.Ocean.Tiles;

internal class Seagrass : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileLavaDeath[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileSolidTop[Type] = false;
		Main.tileSolid[Type] = false;
		Main.tileCut[Type] = true;
		Main.tileNoFail[Type] = true;
		TileID.Sets.SwaysInWindBasic[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.CoordinateHeights = [18];
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, 1, 0);
		TileObjectData.newTile.AnchorValidTiles = [TileID.Sand, TileID.Crimsand, TileID.Ebonsand];
		TileObjectData.newTile.RandomStyleRange = 16;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.addTile(Type);

		DustType = DustID.JungleGrass;
		HitSound = SoundID.Grass;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;
	public override void SetSpriteEffects(int i, int j, ref SpriteEffects effects) => effects = (i % 2 == 0) ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
}