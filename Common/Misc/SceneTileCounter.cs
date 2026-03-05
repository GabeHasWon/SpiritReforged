namespace SpiritReforged.Common.Misc;

public class SceneTileCounter : ModSystem
{
	public sealed class Survey(HashSet<int> types, int limit)
	{
		public int count;
		public readonly int limit = limit;
		public readonly HashSet<int> tileTypes = types;

		/// <summary> Checks whether <see cref="count"/> has hit <see cref="limit"/>. </summary>
		public bool Success => count >= limit;
	}

	/// <summary> Stores Survey by <see cref="ModSceneEffect.Type"/>. </summary>
	public static readonly Dictionary<int, Survey> SurveyByType = [];

	public static Survey GetSurvey<T>() where T : ModSceneEffect => SurveyByType[ModContent.GetInstance<T>().Type];
	public override void TileCountsAvailable(ReadOnlySpan<int> tileCounts)
	{
		foreach (int key in SurveyByType.Keys)
		{
			int count = 0;

			foreach (int type in SurveyByType[key].tileTypes)
				count += tileCounts[type];

			SurveyByType[key].count = count;
		}
	}
}