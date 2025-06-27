using SpiritReforged.Common.TileCommon.TileSway;
using SpiritReforged.Content.Forest.Stargrass.Tiles;
using SpiritReforged.Content.Savanna.Tiles;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Botanist.Tiles;

public class Wheatgrass : ModTile, ISwayTile
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

	public void DrawSway(int i, int j, SpriteBatch spriteBatch, Vector2 offset, float rotation, Vector2 origin)
	{
		var t = Main.tile[i, j];
		var texture = TextureAssets.Tile[Type].Value;
		int sourceHeight = (t.TileFrameY == 18) ? 18 : 16;

		for (int x = 0; x < 3; x++)
		{
			var position = new Vector2(i, j) * 16 - Main.screenPosition + new Vector2(-4 + x * 4, 0);
			var source = new Rectangle((t.TileFrameX + 54 * x) % (18 * Styles), t.TileFrameY, 16, sourceHeight);
			var effects = ((i + x) % 2 == 0) ? SpriteEffects.FlipHorizontally : default;

			spriteBatch.Draw(texture, position + offset, source, Lighting.GetColor(i, j), rotation, origin, 1, effects, 0);
		}
	}

	public float Physics(Point16 topLeft)
	{
		var data = TileObjectData.GetTileData(Framing.GetTileSafely(topLeft));
		float rotation = Main.instance.TilesRenderer.GetWindCycle(topLeft.X, topLeft.Y, TileSwaySystem.Instance.SunflowerWindCounter);

		if (!WorldGen.InAPlaceWithWind(topLeft.X, topLeft.Y, data.Width, data.Height))
			rotation = 0f;

		return (rotation + TileSwayHelper.GetHighestWindGridPushComplex(topLeft.X, topLeft.Y, data.Width, data.Height, 30, 2f, 1, true)) * 1.5f;
	}
}
