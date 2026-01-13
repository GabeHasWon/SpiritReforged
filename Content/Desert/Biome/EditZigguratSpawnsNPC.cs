using SpiritReforged.Content.Desert.Tiles;
using SpiritReforged.Content.Desert.Walls;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.Biome;

internal class EditZigguratSpawnsNPC : GlobalNPC
{
	public static HashSet<int> TileTypes = [];
	public static HashSet<int> WallTypes = [];

	public override void SetStaticDefaults()
	{
		TileTypes = [ModContent.TileType<RedSandstoneBrick>(), ModContent.TileType<RedSandstoneBrickCracked>(), ModContent.TileType<RedSandstoneSlab>(),
			ModContent.TileType<CarvedLapis>(), ModContent.TileType<PaleHive>(), ModContent.TileType<CrackedSandstone>(), ModContent.TileType<TallSandstoneShelf>()];
		WallTypes = [ModContent.WallType<RedSandstoneBrickCrackedWall>(), ModContent.WallType<RedSandstoneBrickWall>(), ModContent.WallType<RedSandstoneBrickForegroundWall>(),
			ModContent.WallType<CarvedLapisWall>(), ModContent.WallType<PaleHiveWall>()];
	}

	public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
	{
		if (InZiggurat(new Rectangle((spawnInfo.SpawnTileX - 1) * 16, (spawnInfo.SpawnTileY - 1) * 16, 32, 32)))
		{
			pool[0] = 0;
			pool[NPCID.SandSlime] = 0.05f;
		}
	}

	internal static bool InZiggurat(Rectangle rectangle)
	{
		Point16 topLeft = rectangle.TopLeft().ToTileCoordinates16();
		Point16 bottomRight = rectangle.BottomRight().ToTileCoordinates16();

		for (int i = topLeft.X - 3; i < bottomRight.X + 4; ++i)
		{
			for (int j = topLeft.Y - 3; j < bottomRight.Y + 4; ++j)
			{
				Tile tile = Main.tile[i, j];

				if ((TileTypes.Contains(tile.TileType) && tile.HasTile) || WallTypes.Contains(tile.WallType))
					return true;
			}
		}

		return false;
	}

	internal static bool InZiggurat(NPCSpawnInfo info) => InZiggurat(new Rectangle((info.SpawnTileX - 1) * 16, (info.SpawnTileY - 1) * 16, 32, 32));
}
