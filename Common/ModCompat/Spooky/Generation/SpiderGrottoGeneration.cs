using SpiritReforged.Content.Crossmod.Spooky.Tiles.Pots;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.ModCompat.Spooky.Generation;

internal class SpiderGrottoGeneration : SpookyMicropass
{
	public override string WorldGenName => "Uncommon Spider Grotto";
	public override string ModifyType => "SpiderCave";
	public override string MatchPass => "Spider Grotto";

	public override void Run(GenerationProgress progress, GameConfiguration config)
	{
		if (!CrossMod.Spooky.TryCall(out Vector2 BiomePosition, "BiomePositions", "SpiderGrottoCenter"))
			return;

		Point pos = BiomePosition.ToTileCoordinates();
		Vector2 center = pos.ToVector2() * 16f + new Vector2(8f);
		float angle = MathHelper.Pi * 0.15f;
		float otherAngle = MathHelper.PiOver2 - angle;

		int InitialSize = Main.maxTilesY >= 1800 ? 240 : 150;
		int biomeSize = InitialSize + Main.maxTilesX / 180;
		float actualSize = biomeSize * 16f;
		float constant = actualSize * 2f / (float)Math.Sin(angle);

		float biomeSpacing = actualSize * (float)Math.Sin(otherAngle) / (float)Math.Sin(angle);
		int verticalRadius = (int)(constant / 16f);

		Vector2 biomeOffset = Vector2.UnitY * biomeSpacing;
		Vector2 biomeTop = center - biomeOffset;
		Vector2 biomeBottom = center + biomeOffset;

		for (int X = pos.X - biomeSize - 2; X <= pos.X + biomeSize + 2; X++)
		{
			for (int Y = (int)(pos.Y - verticalRadius * 0.4f) - 3; Y <= pos.Y + verticalRadius + 3; Y++)
			{
				if (CheckInsideOval(new Point(X, Y), biomeTop, biomeBottom, constant, center, out _))
				{
					Tile tile = Main.tile[X, Y];
					Tile tileAbove = Main.tile[X, Y - 1];

					if (tile.TileType == SpookyTile("DampStone") || tile.TileType == SpookyTile("DampGrass") || tile.TileType == SpookyTile("DampMushroomGrass"))
					{
						if (WorldGen.genRand.NextBool(25) && !tileAbove.HasTile)
							WorldGen.PlaceObject(X, Y - 1, ModContent.TileType<UncommonSpookyPots>(), true, 21 + WorldGen.genRand.Next(0, 3));
					}
				}
			}
		}

		static int SpookyTile(string name) => CrossMod.Spooky.Instance.Find<ModTile>(name).Type;
	}

	public static bool CheckInsideOval(Point tile, Vector2 focus1, Vector2 focus2, float distanceConstant, Vector2 center, out float distance)
	{
		Vector2 point = tile.ToWorldCoordinates();
		float distY = center.Y - point.Y;

		//squish the circle vertically to create an oval shape
		point.Y -= distY * 2.5f;

		float distance1 = Vector2.Distance(point, focus1);
		float distance2 = Vector2.Distance(point, focus2);
		distance = distance1 + distance2;

		return distance <= distanceConstant;
	}
}
