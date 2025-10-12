using SpiritReforged.Common.TileCommon.Conversion;
using SpiritReforged.Common.TileCommon.TileSway;
using SpiritReforged.Common.Visuals.Glowmasks;
using Terraria.DataStructures;
using Terraria.GameContent.Metadata;

namespace SpiritReforged.Content.Forest.Stargrass.Tiles;

[AutoloadGlowmask("255,255,255", false)]
public class StargrassFlowers : ModTile, ISwayTile
{
	public const int StyleRange = 27;
	public const int TileHeight = 24;

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoFail[Type] = true;
		Main.tileCut[Type] = true;
		Main.tileLighted[Type] = true;

		TileID.Sets.SwaysInWindBasic[Type] = true;
		TileMaterials.SetForTileId(Type, TileMaterials._materialsByName["Plant"]);

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.LavaDeath = true;
		TileObjectData.newTile.WaterDeath = false;
		TileObjectData.newTile.CoordinatePadding = 2;
		TileObjectData.newTile.CoordinateWidth = 16;
		TileObjectData.newTile.CoordinateHeights = [TileHeight];
		TileObjectData.newTile.DrawYOffset = -(TileHeight - 18);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = StyleRange;
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<StargrassTile>()];
		TileObjectData.newTile.AnchorAlternateTiles = [TileID.ClayPot, TileID.PlanterBox];
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(20, 190, 130));
		DustType = DustID.Grass;
		HitSound = SoundID.Grass;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 2;
	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		int frame = Main.tile[i, j].TileFrameX / 18;
		if (frame >= 6)
			(r, g, b) = (0.025f, 0.1f, 0.25f);
	}

	public override IEnumerable<Item> GetItemDrops(int i, int j)
	{
		if (Main.player[Player.FindClosest(new Vector2(i, j).ToWorldCoordinates(0, 0), 16, 16)].HeldItem.type == ItemID.Sickle)
			yield return new Item(ItemID.Hay, Main.rand.Next(1, 3));

		if (Main.player[Player.FindClosest(new Vector2(i, j).ToWorldCoordinates(0, 0), 16, 16)].HasItem(ItemID.Blowpipe))
			yield return new Item(ItemID.Seed, Main.rand.Next(2, 4));
	}

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
	{
		ConversionHandler.CommonPlants(i, j, Type);
		return true;
	}

	public void DrawSway(int i, int j, SpriteBatch spriteBatch, Vector2 offset, float rotation, Vector2 origin)
	{
		var tile = Framing.GetTileSafely(i, j);
		int type = tile.TileType;
		var data = TileObjectData.GetTileData(tile);

		var drawPos = new Vector2(i * 16 - (int)Main.screenPosition.X, j * 16 - (int)Main.screenPosition.Y);
		var source = new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, TileHeight);
		var dataOffset = new Vector2(data.DrawXOffset, data.DrawYOffset);

		spriteBatch.Draw(TextureAssets.Tile[type].Value, drawPos + offset + dataOffset, source, Lighting.GetColor(i, j), rotation, origin, 1, default, 0);

		var glowmask = GlowmaskTile.TileIdToGlowmask[Type].Glowmask.Value;
		spriteBatch.Draw(glowmask, drawPos + offset + dataOffset, source, GetGlow(new(i, j)), rotation, origin, 1, default, 0);

		static Color GetGlow(Point16 coords)
		{
			const float maxDistance = 140 * 140;

			float distance = Main.player[Player.FindClosest(coords.ToWorldCoordinates(0, 0), 16, 16)].DistanceSQ(coords.ToWorldCoordinates());
			return StargrassTile.Glow(new Point(coords.X, coords.Y)) * MathHelper.Clamp(1f - distance / maxDistance, 0.4f, 1f);
		}
	}

	public float Physics(Point16 coords)
	{
		var data = TileObjectData.GetTileData(Framing.GetTileSafely(coords));
		float rotation = Main.instance.TilesRenderer.GetWindCycle(coords.X, coords.Y, TileSwaySystem.GrassWindCounter);

		if (!WorldGen.InAPlaceWithWind(coords.X, coords.Y, data.Width, data.Height))
			rotation = 0f;

		return rotation + Main.instance.TilesRenderer.GetWindGridPush(coords.X, coords.Y, 20, 0.35f) * 1.5f;
	}
}