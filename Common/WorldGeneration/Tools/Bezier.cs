namespace SpiritReforged.Common.WorldGeneration.Tools;

internal class Bezier
{
	public readonly Vector2[] ControlPoints = [];
	public readonly Vector2[] GeneratedPoints = [];

	public Bezier(float distance, params Vector2[] controlPoints)
	{
		ControlPoints = controlPoints;
		List<Vector2> points = [];
		float step = distance / TotalDistance(controlPoints);
		float prog = 0;
		List<Vector2> list = [.. controlPoints];

		while (prog <= 1)
		{
			points.Add(BuildBezier(list, prog));
			prog += step;
		}

		GeneratedPoints = [.. points];
	}

	private static float TotalDistance(Vector2[] controlPoints)
	{
		float totalDist = 0;

		for (int i = 0; i < controlPoints.Length - 1; ++i)
			totalDist += controlPoints[i].Distance(controlPoints[i + 1]);

		return totalDist;
	}

	public static Vector2 BuildBezier(List<Vector2> points, float t)
	{
		if (points.Count < 2)
			throw new Exception("Curve must have at least two points.");

		return RecurseBezier(points, t);
	}

	private static Vector2 RecurseBezier(List<Vector2> points, float t)
	{
		if (points.Count == 2)
			return Vector2.Lerp(points[0], points[1], t);

		var lines = new List<Vector2>(points.Count - 1);

		for (int i = 0; i < points.Count - 1; i++)
			lines.Add(Vector2.Lerp(points[i], points[i + 1], t));

		return BuildBezier(lines, t);
	}
}
