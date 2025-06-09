using SpiritReforged.Common.WorldGeneration.Noise;
using SpiritReforged.Common.WorldGeneration.Tools;
using System.Diagnostics;
using System.Linq;
using Terraria.DataStructures;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.CaveEntrances;

[Obsolete("Will not be implemented for Caves & Clubs for impl. reasons.")]
internal class ArchwayEntrance : CaveEntrance
{
	public override CaveEntranceType Type => CaveEntranceType.Vanilla;

	public override void Generate(int x, int y)
	{
		List<Vector2> arches = [];
		int repeats = WorldGen.genRand.NextBool(3) ? 2 : 1;
		//int floorY = WorldMethods.FindNearestBelow(x, y) + 1;

		for (int i = 0; i < repeats; ++i)
			GenerateEntireArchway(x, y, arches, repeats == 1 ? null : i == 1);

		//foreach (var ar in arches)
		//	PlaceArches(ar);

		//PlaceArches(new Vector2(x, floorY), (new Vector2(FindSideways(x, floorY, -1), floorY), new Vector2(FindSideways(x, floorY, 1), floorY)));
	}

	internal static int FindSideways(int x, int y, int dir)
	{
		while (!WorldGen.SolidTile(x, y))
			x += dir;

		return x;
	}

	private static void GenerateEntireArchway(int x, int y, List<Vector2> arches, bool? flip)
	{
		FastNoiseLite noise = new();
		Vector2[] points = TunnelBetween(new Point16(x, y), new Point16(x, y + 50), noise, flip);
		int archCount = WorldGen.genRand.Next(2, 5);
		int len = points.Length / 8;

		for (int i = 0; i < archCount; i++)
		{
			int approx = points.Length / archCount * i;
			approx = Math.Clamp(approx + WorldGen.genRand.Next(-len, len), 0, points.Length);
			arches.Add(points[approx]);
		}
	}

	private static void PlaceArches(Vector2 spot, (Vector2, Vector2)? forceEdges = null)
	{
		float angle = WorldGen.genRand.NextFloat(-MathHelper.PiOver4, MathHelper.PiOver4);
		Vector2 dir = angle.ToRotationVector2();
		float factor = 0;

		Vector2 start = spot;
		Vector2 end = spot;

		if (forceEdges is null)
		{
			while (true)
			{
				if (!WorldGen.SolidTile((int)start.X, (int)start.Y))
					start += dir;

				if (!WorldGen.SolidTile((int)end.X, (int)end.Y))
					end -= dir;

				if (end.DistanceSQ(spot) > 120 * 120 || start.DistanceSQ(spot) > 120 * 120)
					break;

				if (WorldGen.SolidTile((int)start.X, (int)start.Y) && WorldGen.SolidTile((int)end.X, (int)end.Y))
					break;
			}

			if (end.DistanceSQ(spot) > 120 * 120 || start.DistanceSQ(spot) > 120 * 120)
				return;
		}
		else
		{
			start = forceEdges.Value.Item1;
			end = forceEdges.Value.Item2;
		}

		Vector2 top = Vector2.Lerp(start, end, 0.5f) - new Vector2(0, WorldGen.genRand.NextFloat(5, 12));
		var holes = new Vector2[Main.rand.Next(1, 3)];

		if (forceEdges is not null)
			top.Y -= 20;

		for (int i = 0; i < holes.Length; ++i)
			holes[i] = GetPoint(Main.rand.NextFloat(0.3f, 0.7f));

		while (true)
		{
			Vector2 pos = GetPoint(factor);
			DirtCircle(pos, 3, holes.Any(x => Vector2.DistanceSquared(x, pos) < 5 * 5));

			factor += 0.04f;

			if (factor > 1)
				break;
		}

		Vector2 GetPoint(float factor) => Vector2.Lerp(Vector2.Lerp(start, top, factor), Vector2.Lerp(top, end, factor), factor);
	}

	private static Vector2[] TunnelBetween(Point16 entrance, Point16 exit, FastNoiseLite noise, bool? flipDefault = null)
	{
		var current = entrance.ToVector2();
		var end = exit.ToVector2();
		List<Vector2> positions = [];
		bool flip = flipDefault ?? WorldGen.genRand.NextBool();
		float scale = WorldGen.genRand.NextFloat(0.8f, 1.3f);

		while (true)
		{
			positions.Add(current);

			current.X = entrance.X + WorldGen.genRand.Next(30, 60) * (flip ? -1 : 1);
			current.Y += WorldGen.genRand.Next(15, 25);
			flip = !flip;

			if (current.Y > end.Y)
			{
				current = end;
				positions.Add(current);
				break;
			}
		}

		var points = SmoothTunnel.GeneratePoints([.. positions], new SmoothTunnel.VariationData(5, (0.2f, 0.4f), (5, 10), 0.05f));

		for (int i = 0; i < points.Length; i++)
		{
			Vector2 pos = points[i];
			TunnelDig(noise, pos, MathHelper.Lerp(scale, 0.5f, i / (float)points.Length));
		}

		return points;
	}

	private static void TunnelDig(FastNoiseLite noise, Vector2 item, float scale)
	{
		float mul = 1.2f + MathF.Abs(noise.GetNoise(item.X, item.Y));

		CircleOpening(item, 4.5f * mul * scale);
		CircleOpening(item, WorldGen.genRand.Next(3, 6) * mul * scale);

		if (WorldGen.genRand.NextBool(11))
			WallCircleOpening(item, WorldGen.genRand.Next(4, 6) * mul * scale);
	}

	/// <summary>
	/// Digs a perfectly circular space into tiles.
	/// </summary>
	/// <param name="pos">Center of the circle.</param>
	/// <param name="size">Radius of the circle.</param>
	public static void CircleOpening(Vector2 pos, float size)
	{
		for (int i = (int)(pos.X - size); i < (int)pos.X + size; ++i)
			for (int j = (int)(pos.Y - size); j < (int)pos.Y + size; ++j)
			{
				float distance = Vector2.Distance(pos, new Vector2(i, j));

				if (distance < size)
				{
					Tile tile = Main.tile[i, j];
					tile.HasTile = false;

					if (tile.WallType > 0)
						tile.WallType = distance > size * 0.9f ? WallID.Dirt : WallID.GrassUnsafe;
				}
			}
	}

	/// <summary>
	/// Digs a perfectly circular space into walls.
	/// </summary>
	/// <param name="pos">Center of the circle.</param>
	/// <param name="size">Radius of the circle.</param>
	public static void WallCircleOpening(Vector2 pos, float size)
	{
		for (int i = (int)(pos.X - size); i < (int)pos.X + size; ++i)
			for (int j = (int)(pos.Y - size); j < (int)pos.Y + size; ++j)
				if (Vector2.DistanceSquared(pos, new Vector2(i, j)) < size * size)
					WorldGen.KillWall(i, j);
	}

	/// <summary>
	/// Digs a perfectly circular space into walls.
	/// </summary>
	/// <param name="pos">Center of the circle.</param>
	/// <param name="size">Radius of the circle.</param>
	public static void DirtCircle(Vector2 pos, float size, bool isWall)
	{
		for (int i = (int)(pos.X - size); i < (int)pos.X + size; ++i)
			for (int j = (int)(pos.Y - size); j < (int)pos.Y + size; ++j)
				if (Vector2.DistanceSquared(pos, new Vector2(i, j)) < size * size)
					if (isWall)
						WorldGen.PlaceWall(i, j, WallID.DirtUnsafe, true);
					else
						WorldGen.PlaceTile(i, j, TileID.Dirt, true);
	}

	public override bool ModifyOpening(ref int x, ref int y, bool isCavinator)
	{
		if (!isCavinator)
			return false;

		y += 50;
		return true;
	}
}
