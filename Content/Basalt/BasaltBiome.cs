using SpiritReforged.Common.Easing;
using SpiritReforged.Common.WorldGeneration.Micropasses;
using SpiritReforged.Common.WorldGeneration.Tools;
using SpiritReforged.Content.Basalt.Tiles;
using System.Diagnostics;
using System.Linq;
using Terraria.DataStructures;
using Terraria.IO;
using Terraria.Utilities;
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

	public readonly record struct Offsets(int[] Offs, int[] Widths)
	{
		/// <summary>
		/// Gets the actual height offset for this position.
		/// </summary>
		public int this[int index] => Offs[index + Widths[index]];
	}

	private static WeightedRandom<int> UsableWidths = null!;

	public override string WorldGenName => "Basalt";

	public override int GetWorldGenIndexInsert(List<GenPass> tasks, ref bool afterIndex) => tasks.FindIndex(x => x.Name == "Granite");

	public override void Run(GenerationProgress progress, GameConfiguration config)
	{
		// TBD
	}

	public static void Place(Point16 point)
	{
		UsableWidths = new WeightedRandom<int>();
		UsableWidths.Add(1, 0.2f);
		UsableWidths.Add(2, 0.8f);
		UsableWidths.Add(3, 1f);
		UsableWidths.Add(4, 0.7f);
		UsableWidths.Add(5, 0.4f);
		UsableWidths.Add(6, 0.1f);

		int reps = 1;// WorldGen.genRand.Next(5) + 1;

		for (int i = 0; i < reps; ++i)
			point = PlaceSingleTunnel(point);
	}

	private static Point16 PlaceSingleTunnel(Point16 point)
	{
		float halfWidth = WorldGen.genRand.NextFloat(50, 80);
		var flags = (BiomeFlags)WorldGen.genRand.Next(1 << (int)BiomeFlags.BIT_MAX);
		int circleWidth = 16;
		flags |= BiomeFlags.MutatingAngle;

		if (Main.getGoodWorld)
			halfWidth = WorldGen.genRand.NextBool(2) ? WorldGen.genRand.NextFloat(20, 30) : WorldGen.genRand.NextFloat(110, 140);

		Bezier bez = new Bezier(0.5f, BuildControl(point, halfWidth, flags));
		Offsets offsets = GenerateOffsets(point.X - halfWidth * 2, point.X + halfWidth * 2);
		HashSet<Point16> placements = [];

		foreach (Vector2 pos in bez.GeneratedPoints)
			OffsetCircleGeneration(pos, offsets, circleWidth, true, placements);

		placements.Clear();
		foreach (Vector2 pos in bez.GeneratedPoints)
			OffsetCircleGeneration(pos, offsets, circleWidth / 2, false, placements);

		return Point16.Zero;
	}

	private static void OffsetCircleGeneration(Vector2 pos, Offsets offsets, int circleWidth, bool place, HashSet<Point16> placements)
	{
		int lastOffset = -60_000;
		ushort type = (ushort)ModContent.TileType<BasaltTile>();
		int offsetIndex = 0;

		for (int i = (int)pos.X - circleWidth; i < (int)pos.X + circleWidth; ++i)
		{
			int offset = offsets[offsetIndex];
			offsetIndex++;

			if (lastOffset != offset)
				type = type == (ushort)ModContent.TileType<BasaltTile>() ? (ushort)ModContent.TileType<AccentBasalt>() : (ushort)ModContent.TileType<BasaltTile>();

			for (int j = (int)pos.Y - circleWidth; j < (int)pos.Y + circleWidth; ++j)
			{
				Point16 placePos = new(i, j + offset);

				if (placements.Contains(placePos))
					continue;

				if (Vector2.DistanceSquared(new Vector2(i, j), pos) < circleWidth * circleWidth)
				{
					Tile tile = Main.tile[i, j + offset];

					if (place)
					{
						tile.HasTile = true;
						tile.TileType = type;
					}
					else
						tile.ClearTile();

					placements.Add(placePos);
				}
			}
		}
	}

	private static Offsets GenerateOffsets(float minX, float maxX)
	{
		int widthRange = (int)(maxX - minX);
		int[] offsets = new int[widthRange];
		int[] widths = new int[widthRange];
		int currentWidth = UsableWidths.Get();
		int currentOffset = WorldGen.genRand.Next(-26, 27);
		int count = 0;

		for (int i = 0; i < widthRange; ++i)
		{
			offsets[i] = currentOffset;
			widths[i] = count;
			count--;

			if (-count == currentWidth)
			{
				currentWidth = UsableWidths.Get();
				currentOffset = WorldGen.genRand.Next(-6, 7);
				count = 0;
			}
		}

		return new Offsets(offsets, widths);
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
