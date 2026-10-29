namespace SpiritReforged.Common.WorldGeneration.Tools;

internal class Bezier
{
	public readonly Vector2[] ControlPoints = [];
	public readonly Vector2[] GeneratedPoints = [];

	public Bezier(float distance, params Vector2[] controlPoints)
	{
		ControlPoints = controlPoints;

		List<Vector2> points = [];
		int current = 0;
		float progress = 0;
		float dist = DistanceFactor(distance, current);

		while (true)
		{
			points.Add(Interpolate(current, progress));

			progress += dist;

			if (progress >= 1)
			{
				current += (int)progress;
				progress -= (int)progress;

				if (current >= ControlPoints.Length - 2)
					break;
			}
		}

		GeneratedPoints = [.. points];
	}

	private Vector2 Interpolate(int currentIndex, float factor) => Vector2.Lerp(Vector2.Lerp(ControlPoints[currentIndex], ControlPoints[currentIndex + 1], factor), 
		Vector2.Lerp(ControlPoints[currentIndex + 1], ControlPoints[currentIndex + 2], factor), factor);

	private float DistanceFactor(float distance, int currentIndex) => distance / ControlPoints[currentIndex].Distance(ControlPoints[currentIndex + 1]);
}
