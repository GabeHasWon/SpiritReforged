using SpiritReforged.Common.TileCommon.PresetTiles;

namespace SpiritReforged.Common.TileCommon.Tree;

/// <summary> Applies the effects of fertilizer to <see cref="CustomTree"/> saplings. </summary>
internal class FertilizerGlobalProjectile : GlobalProjectile
{
	public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.type == ProjectileID.Fertilizer;

	public override void AI(Projectile projectile)
	{
		Point start = projectile.TopLeft.ToTileCoordinates();
		Point end = projectile.BottomRight.ToTileCoordinates();

		for (int x = start.X; x < end.X + 1; x++)
		{
			for (int y = start.Y; y < end.Y + 1; y++)
			{
				if (WorldGen.InWorld(x, y) && TileLoader.GetTile(Main.tile[x, y].TileType) is SaplingTile)
					GrowTree(x, y);
			}
		}
	}

	public static bool GrowTree(int x, int y)
	{
		Tile tile = Main.tile[x, y];
		if (TileLoader.GetTile(tile.TileType) is SaplingTile || TileID.Sets.TreeSapling[tile.TileType])
		{
			bool result = false;

			result |= CustomTree.GrowTree(x, y);
			result |= WorldGen.GrowTree(x, y);

			return result;
		}

		return false;
	}
}