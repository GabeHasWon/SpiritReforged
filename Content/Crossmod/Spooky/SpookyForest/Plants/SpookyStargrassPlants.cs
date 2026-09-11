using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Forest.Stargrass.Tiles;
using Terraria.DataStructures;
using TileHelper.Common;

namespace SpiritReforged.Content.Crossmod.Spooky.SpookyForest.Plants;

internal class OrangeStargrassPlants : StargrassFlowers
{
	protected override bool HasGlowmask => false;

	public override bool IsLoadingEnabled(Mod mod) => CrossMod.Spooky.Enabled;

	protected override void ModifyObjectData(TileObjectData newTile)
	{
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<OrangeSpookyStargrass>()];

		//TileHelperSets.TileGlowmask[Type] = Helpers.RequestGlowmask(this, static (i, j) =>
		//{
		//	const float max_distance = 140;

		//	Point coords = new(i, j);
		//	float distance = Main.player[Player.FindClosest(coords.ToWorldCoordinates(0, 0), 16, 16)].DistanceSQ(coords.ToWorldCoordinates());

		//	return StargrassTile.GetGlowColor(coords.X, coords.Y) * MathHelper.Clamp(1f - distance / (max_distance * max_distance), 0.4f, 1f);
		//});
	}

	//public override void DrawSway(int i, int j, SpriteBatch spriteBatch, Vector2 offset, float rotation, Vector2 origin)
	//{
	//	var tile = Framing.GetTileSafely(i, j);
	//	var data = TileObjectData.GetTileData(tile);

	//	if (!TileExtensions.GetVisualInfo(i, j, out Color color, out Texture2D tex))
	//		return;

	//	var drawPos = new Vector2(i * 16 - (int)Main.screenPosition.X, j * 16 - (int)Main.screenPosition.Y);
	//	var source = new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, TileHeight);
	//	var dataOffset = new Vector2(data.DrawXOffset, data.DrawYOffset);

	//	spriteBatch.Draw(tex, drawPos + offset + dataOffset, source, Lighting.GetColor(i, j, color), rotation, origin, 1, default, 0);

	//	source = new Rectangle(tile.TileFrameX, tile.TileFrameY + 22, 16, TileHeight);
	//	spriteBatch.Draw(tex, drawPos + offset + dataOffset, source, GetGlow(new(i, j)), rotation, origin, 1, default, 0);

	//	static Color GetGlow(Point16 coords)
	//	{
	//		const float max_distance = 140 * 140;

	//		float distance = Main.player[Player.FindClosest(coords.ToWorldCoordinates(0, 0), 16, 16)].DistanceSQ(coords.ToWorldCoordinates());
	//		return StargrassTile.GetGlowColor(coords.X, coords.Y) * MathHelper.Clamp(1f - distance / max_distance, 0.4f, 1f);
	//	}
	//}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		int frame = Main.tile[i, j].TileFrameX / 18;

		if (frame >= 6)
			(r, g, b) = (0.2f, 0.2f, 0.05f);
	}
}

internal class GreenStargrassPlants : OrangeStargrassPlants
{
	protected override void ModifyObjectData(TileObjectData newTile) => TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<GreenSpookyStargrass>()];

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		int frame = Main.tile[i, j].TileFrameX / 18;

		if (frame >= 6)
			(r, g, b) = (0.1f, 0.25f, 0.05f);
	}
}