using SpiritReforged.Common.MathHelpers;

namespace SpiritReforged.Common.WorldGeneration.Tools;

internal class SmoothTunnel
{
	public readonly struct VariationData(int flatVariance, (float, float) perpLerpRange, (float, float) perpLengthRange, float perpRandomAngle)
	{
		public readonly int FlatAreaVariance = flatVariance;

		public readonly (float min, float max) PerpLerpRange = perpLerpRange;
		public readonly (float min, float max) PerpLengthRange = perpLengthRange;
		public readonly float PerpRandomAngle = perpRandomAngle;
	}

	public static Vector2[] GeneratePoints(Vector2[] points, VariationData data)
	{
		points = AddVariationToPoints(points, data);

		if (HasNaNs(points))
		{
			int i = 0;
		}

		Vector2[] results = Spline.CreateSpline(points, 60);

		if (HasNaNs(points))
		{
			int i = 0;
		}

		return CreateEquidistantSet(results, 10);
	}

	public static bool HasNaNs(Vector2[] points)
	{
		foreach (var point in points)
		{
			if (point.HasNaNs())
			{
				return true;
			}
		}

		return false;
	}

	private static Vector2[] AddVariationToPoints(Vector2[] points, VariationData data)
	{
		List<Vector2> newPoints = [];

		for (int i = 0; i < points.Length; i++)
		{
			Vector2 item = points[i];
			newPoints.Add(item);

			if (i == points.Length - 1)
				continue;

			if (i < points.Length - 1 && WorldGen.genRand.NextBool())
			{
				var startLerp = Vector2.Lerp(item, points[i + 1], WorldGen.genRand.NextFloat(data.PerpLerpRange.min, data.PerpLerpRange.max));
				startLerp += item.DirectionTo(points[i + 1]).RotatedBy(MathHelper.Pi * (WorldGen.genRand.NextBool() ? -1 : 1)).RotatedByRandom(data.PerpRandomAngle)
					* WorldGen.genRand.NextFloat(data.PerpLengthRange.min, data.PerpLengthRange.max);
				newPoints.Add(startLerp);
			}
			else
			{
				int var = data.FlatAreaVariance;

				newPoints.Add(item + new Vector2(WorldGen.genRand.Next(-var, var), WorldGen.genRand.Next(var)));
			}
		}

		return [.. newPoints];
	}

	private static Vector2[] CreateEquidistantSet(Vector2[] results, float distance)
	{
		List<Vector2> points = [];
		Queue<Vector2> remainingPoints = new(results);
		Vector2 start = remainingPoints.Dequeue();
		Vector2 current = start;
		Vector2 next = remainingPoints.Dequeue();
		float factor = 0;

		foreach (var point in remainingPoints)
		{
			if (point.HasNaNs())
			{
				return [];
			}
		}

		while (true)
		{
			float dist = current.Distance(next);

			if (dist == 0)
			{
				factor++;
			}
			else
			{
				while (true)
				{
					points.Add(Vector2.Lerp(start, next, factor));
					factor += MathF.Min(1, distance / dist);

					if (factor > 1f)
						break;
				}
			}

			if (remainingPoints.Count == 0)
				return [.. points];

			start = next;
			next = remainingPoints.Dequeue();
			factor--;
		}
	}
}
