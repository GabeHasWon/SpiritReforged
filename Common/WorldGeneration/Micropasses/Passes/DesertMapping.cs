using System.Linq;
using Terraria.DataStructures;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class DesertMapping : Micropass
{
	public readonly record struct DesertInstance(int Index, int BiomeConversionType, Rectangle Bounds);

	public override string WorldGenName => "Map Deserts";

	public static int ArbitraryYCutoff => (int)Main.worldSurface + 50;

	public static Dictionary<int, DesertInstance> instances = [];

	public override int GetWorldGenIndexInsert(List<GenPass> tasks, ref bool afterIndex) => tasks.FindIndex(x => x.Name == "Floating Islands");

	public override void Run(GenerationProgress progress, GameConfiguration config)
	{
		instances.Clear();
		HashSet<Point16> totalPoints = [];

		for (int i = WorldGen.beachDistance; i < Main.maxTilesX - WorldGen.beachDistance; ++i)
		{
			for (int j = (int)(Main.worldSurface * 0.35); j < Main.worldSurface + 20; ++j)
			{
				Tile tile = Main.tile[i, j];

				if (tile.HasTile && Main.tileSolid[tile.TileType])
				{
					if (Main.tileSand[tile.TileType] && !totalPoints.Contains(new Point16(i, j)))
						Traverse(i, j, totalPoints);
					else
						break;
				}
			}
		}
	}

	private static void Traverse(int i, int j, HashSet<Point16> totalPoints)
	{
		HashSet<Point16> traversalPoints = [];
		HashSet<Point16> nextPoints = [new(i, j)];
		HashSet<Point16> cachedNext = [];
		Vector4 edges = new(Main.maxTilesX, ArbitraryYCutoff, -1, -1);
		Dictionary<int, int> counts = [];

		while (nextPoints.Count > 0)
		{
			foreach (Point16 pos in nextPoints)
			{
				Tile tile = Main.tile[pos];

				if (tile.HasTile && Main.tileSand[tile.TileType])
				{
					int convId = BiomeConversionID.Purity;

					if (TileID.Sets.Corrupt[convId])
						convId = BiomeConversionID.Corruption;
					else if (TileID.Sets.Crimson[convId])
						convId = BiomeConversionID.Crimson;

					counts.TryAdd(convId, 0);
					counts[convId]++;

					traversalPoints.Add(pos);

					AddCache(new Point16(pos.X + 1, pos.Y));
					AddCache(new Point16(pos.X - 1, pos.Y));
					AddCache(new Point16(pos.X, pos.Y + 1));
					AddCache(new Point16(pos.X, pos.Y - 1));

					if (pos.X < edges.X)
						edges.X = pos.X;

					if (pos.Y < edges.Y)
						edges.Y = pos.Y;

					if (pos.X > edges.Z)
						edges.Z = pos.X;

					if (pos.X > edges.W)
						edges.W = pos.X;
				}
			}

			nextPoints.Clear();

			foreach (Point16 pos in cachedNext)
				nextPoints.Add(pos);

			cachedNext.Clear();
		}

		var bounds = new Rectangle((int)edges.X, (int)edges.Y, (int)(edges.Z - edges.X), (int)(edges.W - edges.Y));
		int max = counts.MaxBy(x => x.Value).Key;

		instances.Add(instances.Count, new DesertInstance(instances.Count, max, bounds));

		foreach (Point16 pos in traversalPoints)
			totalPoints.Add(pos);

		return;

		void AddCache(Point16 pos)
		{
			if (!totalPoints.Contains(pos) && !traversalPoints.Contains(pos) && pos.Y < ArbitraryYCutoff)
				cachedNext.Add(pos);
		}
	}
}
