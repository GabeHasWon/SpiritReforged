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
				if (!WorldGen.InWorld(x, y))
					continue;

				var t = Main.tile[x, y];

				if (TileLoader.GetTile(t.TileType) is SaplingTile)
					CustomTree.GrowTree(x, y);
			}
		}
	}
}