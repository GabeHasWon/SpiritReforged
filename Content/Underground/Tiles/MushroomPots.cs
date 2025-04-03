using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underground.Tiles;

public class MushroomPots : ModTile
{
	public override void SetStaticDefaults()
	{
		const int row = 3;

		Main.tileSolid[Type] = false;
		Main.tileBlockLight[Type] = false;
		Main.tileCut[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileSpelunker[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = new(0, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.StyleWrapLimit = row;
		TileObjectData.newTile.RandomStyleRange = row;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.UsesCustomCanPlace = true;
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(100, 90, 35), Language.GetText("MapObject.Pot"));
		DustType = DustID.Pot;
	}

	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (effectOnly || fail)
			return;

		//Do vanilla pot break effects
		var t = Main.tile[i, j];
		short oldFrameY = t.TileFrameY;

		t.TileFrameY = t.TileFrameX;
		WorldGen.CheckPot(i, j);
		t.TileFrameY = oldFrameY;
	}

	public override bool KillSound(int i, int j, bool fail)
	{
		if (!fail)
		{
			var pos = new Vector2(i, j).ToWorldCoordinates(16, 16);
			SoundEngine.PlaySound(SoundID.Shatter, pos);

			return false;
		}

		return true;
	}
}