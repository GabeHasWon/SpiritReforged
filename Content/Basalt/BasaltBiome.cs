using SpiritReforged.Common.Easing;
using SpiritReforged.Common.WorldGeneration.Micropasses;
using SpiritReforged.Common.WorldGeneration.Tools;
using SpiritReforged.Content.Basalt.Tiles;
using System.Linq;
using Terraria.DataStructures;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Content.Basalt;

#nullable enable

internal class BasaltBiome : Micropass
{
	[Flags]
	public enum BiomeFlags : byte
	{
		None,
		HookLeft = 1 << 1,
		HookRight = 1 << 2,
		ProportionateHeightVariance = 1 << 3,
		MutatingAngle = 1 << 4,
		Waterway = 1 << 5,

		BIT_MAX = 6,
	}

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
		float halfWidth = WorldGen.genRand.NextFloat(50, 80);
		var flags = (BiomeFlags)WorldGen.genRand.Next(1 << (int)BiomeFlags.BIT_MAX);
		flags |= BiomeFlags.MutatingAngle;

		if (Main.getGoodWorld)
			halfWidth = WorldGen.genRand.NextBool(2) ? WorldGen.genRand.NextFloat(20, 30) : WorldGen.genRand.NextFloat(110, 140);

		Bezier bez = new Bezier(0.5f, BuildControl(point, halfWidth, flags));

		foreach (Vector2 pos in bez.GeneratedPoints)
			WorldGen.PlaceTile((int)pos.X, (int)pos.Y, ModContent.TileType<BasaltTile>());

		return Point16.Zero;
	}

	private static Vector2[] BuildControl(Point16 pos, float halfWidth, BiomeFlags flags)
	{
		const float MAX_ANGLE = 0.4f;

		Vector2 center = pos.ToVector2();
		List<Vector2> points = [center];
		float originAngle = WorldGen.genRand.NextFloat(-MAX_ANGLE, MAX_ANGLE);
		float mutationAngle;
		float angle = originAngle;
		bool mutateAngle = flags.HasFlag(BiomeFlags.MutatingAngle);
		EaseFunction? function = mutateAngle ? WorldGen.genRand.Next(EaseFunction.AllFunctions) : EaseFunction.Linear;

		do
		{
			mutationAngle = WorldGen.genRand.NextFloat(-MAX_ANGLE, MAX_ANGLE);
		} while (mutateAngle && MathF.Abs(mutationAngle - originAngle) < MAX_ANGLE * 0.9f);

		int count = WorldGen.genRand.Next(0, 5);

		points.Add(GeneratePoint(-halfWidth));
		points.Add(GeneratePoint(halfWidth));

		for (int i = 0; i < count; ++i)
		{
			points.Add(GeneratePoint(WorldGen.genRand.NextFloat(-halfWidth, halfWidth)));

			if (mutateAngle)
				originAngle = MathHelper.Lerp(originAngle, mutationAngle, i / (float)count);
		}

		points = [.. points.OrderBy(x => x.X)];

		if (flags.HasFlag(BiomeFlags.HookLeft))
		{
			float targetY = WorldGen.genRand.NextFloat(30, 60) * (WorldGen.genRand.NextBool() ? 1 : -1);
			Vector2 edgePos = GeneratePoint(-halfWidth);
			points.Insert(0, edgePos - new Vector2(WorldGen.genRand.Next(-30, 30), targetY * WorldGen.genRand.NextFloat(0.3f, 0.7f)));
			points.Insert(0, edgePos - new Vector2(WorldGen.genRand.Next(-45, 40), targetY));
		}

		if (flags.HasFlag(BiomeFlags.HookRight))
		{
			float targetY = WorldGen.genRand.NextFloat(20, 60) * (WorldGen.genRand.NextBool() ? 1 : -1);
			Vector2 edgePos = GeneratePoint(halfWidth);
			points.Add(edgePos + new Vector2(WorldGen.genRand.Next(-30, 30), targetY * WorldGen.genRand.NextFloat(0.3f, 0.7f)));
			points.Add(edgePos + new Vector2(WorldGen.genRand.Next(-45, 40), targetY));
		}

		return [.. points];

		Vector2 GeneratePoint(float xOff) => center + new Vector2(xOff, WorldGen.genRand.NextFloat(-40, 40)).RotatedBy(originAngle);
	}
}
