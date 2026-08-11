namespace SpiritReforged.Common.Misc;

public class SceneTileCounter : ModSystem
{
	public sealed class Survey(HashSet<int> types, int limit)
	{
		/// <summary> Checks whether <see cref="Count"/> has hit <see cref="limit"/>. </summary>
		public bool Success => Count >= limit;

		public int Count { get; private set; }

		public readonly int limit = limit;
		public readonly HashSet<int> typesToRead = types;
		public readonly Dictionary<int, int> countByType = [];

		public void SetCounts(ReadOnlySpan<int> tileCounts)
		{
			int count = 0;
			countByType.Clear();

			foreach (int type in typesToRead)
			{
				countByType.Add(type, tileCounts[type]);
				count += tileCounts[type];
			}

			Count = count;
		}
	}

	/// <summary> Stores Survey by <see cref="ModSceneEffect.Type"/>. </summary>
	public static readonly Dictionary<int, Survey> SurveyByType = [];

	public static Survey GetSurvey<T>() where T : ModSceneEffect => SurveyByType[ModContent.GetInstance<T>().Type];
	public override void TileCountsAvailable(ReadOnlySpan<int> tileCounts)
	{
		foreach (int key in SurveyByType.Keys)
			SurveyByType[key].SetCounts(tileCounts);
	}
}