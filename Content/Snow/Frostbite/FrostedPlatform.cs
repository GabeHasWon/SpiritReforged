using SpiritReforged.Common.TileCommon;

namespace SpiritReforged.Content.Snow.Frostbite;

public class FrostedPlatform : ModTile
{
	private enum Merge
	{
		Hard,
		Soft,
		None
	}

	public override void SetStaticDefaults()
	{
		Main.tileLighted[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileSolidTop[Type] = true;
		Main.tileSolid[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileTable[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileID.Sets.Platforms[Type] = true;
		TileID.Sets.DisableSmartCursor[Type] = true;

		TileObjectData.newTile.CoordinateHeights = [16];
		TileObjectData.newTile.CoordinateWidth = 16;
		TileObjectData.newTile.CoordinatePadding = 2;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.StyleMultiplier = 27;
		TileObjectData.newTile.StyleWrapLimit = 27;
		TileObjectData.newTile.UsesCustomCanPlace = false;
		TileObjectData.newTile.LavaDeath = true;
		TileObjectData.addTile(Type);

		AddToArray(ref TileID.Sets.RoomNeeds.CountsAsDoor);
		AddMapEntry(new Color(179, 146, 107));
		this.Merge(TileID.Platforms);

		DustType = DustID.Ice;
		AdjTiles = [TileID.Platforms];
	}

	public override void PostSetDefaults() => Main.tileNoSunLight[Type] = false;
	public override void NumDust(int i, int j, bool fail, ref int num) => num = 4;

	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (!WorldGen.generatingWorld)
			ThawOut(i, j);

		fail = true;
	}

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
	{
		var t = Main.tile[i, j];
		t.TileFrameY = 0;

		if (Connection(-1) is Merge.Hard && Connection(1) is Merge.Hard)
		{
			t.TileFrameX = 36;
		}
		else if (Connection(-1) is Merge.Hard)
		{
			t.TileFrameX = (short)((Connection(1) is Merge.None) ? 72 : 54);
		}
		else if (Connection(1) is Merge.Hard)
		{
			t.TileFrameX = (short)((Connection(-1) is Merge.None) ? 0 : 18);
		}
		else
		{
			t.TileFrameX = (short)((Connection(-1) is Merge.None && Connection(1) is Merge.None) ? 108 : 90);
		}

		return false;

		Merge Connection(int dir)
		{
			var c = Framing.GetTileSafely(i + Math.Sign(dir), j);

			if (c.TileType == Type)
				return Merge.Hard;
			else if (c.TileType == TileID.Platforms)
				return Merge.Soft;
			else
				return Merge.None;
		}
	}

	public override bool Slope(int i, int j)
	{
		ThawOut(i, j);
		return true;
	}

	private static void ThawOut(int i, int j)
	{
		var t = Main.tile[i, j];

		t.TileType = TileID.Platforms;
		t.TileFrameY = 18 * 19; //Convert into a boreal platform

		WorldGen.Reframe(i, j);
		NetMessage.SendTileSquare(-1, i, j);
	}
}