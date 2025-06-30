using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.GenerationModifiers;

/// <summary>
/// Continues generation if the given tile is not exposed to air, or, the open air is walled.
/// </summary>
public class NotOpenOrWalled() : GenAction
{
	public override bool Apply(Point origin, int x, int y, params object[] args)
	{
		if (TileIsExposedToOpenAir(x, y))
			return Fail();

		return UnitApply(origin, x, y, args);
	}

	private static bool TileIsExposedToOpenAir(int x, int y)
	{
		if (!WorldGen.InWorld(x, y, 2))
			return false;

		for (int i = x - 1; i <= x + 1; i++)
		{
			for (int j = y - 1; j <= y + 1; j++)
			{
				if (i == x && j == y)
					continue;

				Tile tile = Main.tile[i, j];

				if (tile.WallType == WallID.None && (!tile.HasTile || !Main.tileSolid[tile.TileType] || TileID.Sets.Platforms[tile.TileType]))
					return true;
			}
		}

		return false;
	}
}
