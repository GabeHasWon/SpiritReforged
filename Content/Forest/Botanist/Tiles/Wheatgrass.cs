using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Forest.Stargrass.Tiles;
using SpiritReforged.Content.Savanna.Tiles;
using Terraria.DataStructures;
using TileHelper.Common;

namespace SpiritReforged.Content.Forest.Botanist.Tiles;

public class Wheatgrass : ModTile, ICutAttempt, WindTileRenderer.IDrawInWind
{
	public const int Styles = 9;

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoFail[Type] = true;
		Main.tileCut[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2);
		TileObjectData.newTile.CoordinateHeights = [16, 18];
		TileObjectData.newTile.Origin = new(0, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.AnchorValidTiles = [TileID.Grass, TileID.Dirt, ModContent.TileType<StargrassTile>(), ModContent.TileType<SavannaGrass>()];
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = 6;
		TileObjectData.addTile(Type);

		AddMapEntry(Color.Yellow);
		DustType = DustID.Hay;
		HitSound = SoundID.Grass;
	}

	public override IEnumerable<Item> GetItemDrops(int i, int j)
	{
		if (Main.player[Player.FindClosest(new Vector2(i, j).ToWorldCoordinates(0, 0), 16, 16)].HeldItem.type == ItemID.Sickle)
			yield return new Item(ItemID.Hay, Main.rand.Next(4, 9));
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;

	void WindTileRenderer.IDrawInWind.DrawInWind(SpriteBatch spriteBatch, int i, int j, float rotation, Vector2 position, Vector2 origin)
	{
		Tile tile = Main.tile[i, j];
		int sourceHeight = (tile.TileFrameY == 18) ? 18 : 16;

		for (int x = 0; x < 3; x++)
		{
			Vector2 offset = new(-4 + x * 4, 0);
			Rectangle source = new((tile.TileFrameX + 54 * x) % (18 * Styles), tile.TileFrameY, 16, sourceHeight);
			SpriteEffects effects = ((i + x) % 2 == 0) ? SpriteEffects.FlipHorizontally : 0;

			spriteBatch.Draw(Helpers.GetTileTextureValue(tile), position + offset, source, Lighting.GetColor(i, j), rotation, origin, 1, effects, 0);
		}
	}

	float WindTileRenderer.IDrawInWind.GetWindStrength(int i, int j)
	{
		if (TileObjectData.GetTileData(Framing.GetTileSafely(i, j)) is TileObjectData tileObjectData)
		{
			float rotation = WorldGen.InAPlaceWithWind(i, j, tileObjectData.Width, tileObjectData.Height) ? Main.instance.TilesRenderer.GetWindCycle(i, j, WindTileRenderer.SunflowerWindCounter) : 0f;
			return (rotation + WindTileRenderer.GetHighestWindGridPushComplex(i, j, tileObjectData.Width, tileObjectData.Height, 30, 2f, 1, true)) * 1.5f;
		}

		return 0f;
	}

	public bool OnCutAttempt(int i, int j)
	{
		var p = Main.player[Player.FindClosest(new Vector2(i, j) * 16, 16, 16)];
		return p.HeldItem.type is ItemID.Sickle or ItemID.LawnMower; //Only allow this tile to be cut using a sickle or lawnmower
	}
}