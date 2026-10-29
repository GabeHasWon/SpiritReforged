using SpiritReforged.Common.WorldGeneration.Micropasses;
using SpiritReforged.Common.WorldGeneration.Tools;
using SpiritReforged.Content.Basalt.Tiles;
using Terraria.DataStructures;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Content.Basalt;

internal class BasaltBiome : Micropass
{
	public override string WorldGenName => "Basalt";

	public override int GetWorldGenIndexInsert(List<GenPass> tasks, ref bool afterIndex) => tasks.FindIndex(x => x.Name == "Granite");

	public override void Run(GenerationProgress progress, GameConfiguration config)
	{
		// TBD
	}

	public static void Place(Point16 point)
	{
		int reps = 1;// WorldGen.genRand.Next(5) + 1;

		for (int i = 0; i < reps; ++i)
			point = PlaceSingleTunnel(point);
	}

	private static Point16 PlaceSingleTunnel(Point16 point)
	{
		Vector2[] bezier = GenerateBezier(point);

		foreach (Vector2 pos in bezier)
			WorldGen.PlaceTile((int)pos.X, (int)pos.Y, ModContent.TileType<BasaltTile>());

		return Point16.Zero;
	}

	private static Vector2[] GenerateBezier(Point16 point)
	{
		Bezier bez = new Bezier(0.5f, [point.ToVector2(), point.ToVector2() + new Vector2(80, WorldGen.genRand.NextFloat(-60, 60)), 
			point.ToVector2() + new Vector2(WorldGen.genRand.NextFloat(60, 80), WorldGen.genRand.NextFloat(-160, 160)), 
			point.ToVector2() + new Vector2(WorldGen.genRand.NextFloat(100, 110), WorldGen.genRand.NextFloat(-60, 60)), 
			point.ToVector2() + new Vector2(WorldGen.genRand.NextFloat(120, 130), 0)]);
		return bez.GeneratedPoints;
	}
}
