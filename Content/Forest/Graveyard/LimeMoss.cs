namespace SpiritReforged.Content.Forest.Graveyard;

public class LimeMoss : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoFail[Type] = true;
		Main.tileSolid[Type] = false;
		TileID.Sets.DrawTileInSolidLayer[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.newTile.AnchorBottom = new(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(172, 184, 40));
		DustType = DustID.JungleGrass;
	}

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
	{
		Tile tile = Main.tile[i, j];
		Tile left = Framing.GetTileSafely(i - 1, j);
		Tile right = Framing.GetTileSafely(i + 1, j);

		if (left.HasTile && left.TileType == Type && right.HasTile && right.TileType == Type)
			tile.TileFrameX = 18;
		else if (left.HasTile && left.TileType == Type)
			tile.TileFrameX = 36;
		else if (right.HasTile && right.TileType == Type)
			tile.TileFrameX = 0;

		tile.TileFrameY = (short)(18 * Main.rand.Next(3));

		return true;
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		Main.instance.TilesRenderer.AddSpecialPoint(i, j, Terraria.GameContent.Drawing.TileDrawing.TileCounterType.CustomSolid);
		return false;
	}

	public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch)
	{
		Tile tile = Main.tile[i, j];
		Texture2D texture = TextureAssets.Tile[Type].Value;
		Rectangle source = new(tile.TileFrameX, tile.TileFrameY, 16, 18);

		spriteBatch.Draw(texture, new Vector2(i, j) * 16 - Main.screenPosition + new Vector2(0, 2), source, Lighting.GetColor(i, j), 0, Vector2.Zero, 1, default, 0);
		DrawVine(i, j, spriteBatch);
	}

	private void DrawVine(int i, int j, SpriteBatch spriteBatch)
	{
		int length = i * j % 3;

		for (int l = 0; l < length; l++)
		{
			Texture2D texture = TextureAssets.Tile[Type].Value;
			Rectangle source = new((l == length - 1) ? 72 : 54, new Terraria.Utilities.FastRandom(i * j + l).Next(3) * 18, 16, 16);

			spriteBatch.Draw(texture, new Vector2(i, j + l + 1) * 16 - Main.screenPosition + new Vector2(0, 2), source, Lighting.GetColor(i, j + l + 1), 0, Vector2.Zero, 1, default, 0);
		}
	}
}