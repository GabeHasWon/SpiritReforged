namespace SpiritReforged.Common.WorldGeneration.Tools;

/// <summary>
/// Builds a minimal-allocation bezier based on the given points.
/// </summary>
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
			points.Add(QuickBezier(list, prog));
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

	public static unsafe Vector2 QuickBezier(List<Vector2> list, float t)
	{
		if (list.Count < 2)
			throw new Exception("Curve must have at least two points.");

		Vector2* points = stackalloc Vector2[list.Count];
		
		for (int i = 0; i < list.Count; ++i)
			points[i] = list[i]; 

		return RecurseQuick(points, list.Count, t);
	}

	private static unsafe Vector2 RecurseQuick(Vector2* points, int length, float t)
	{
		if (length == 2)
			return Vector2.Lerp(points[0], points[1], t);

		length--;
		var lines = stackalloc Vector2[length];

		for (int i = 0; i < length; i++)
			lines[i] = Vector2.Lerp(points[i], points[i + 1], t);

		return RecurseQuick(lines, length, t);
	}
}
