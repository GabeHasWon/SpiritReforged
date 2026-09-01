namespace SpiritReforged.Common.Misc;

internal static class ContentUtils
{
	/// <summary>
	/// Helper method that functions as <see cref="ModContent.TryFind{T}(string, out T)"/> with multiple allowed possible name inputs, while only returning one valid output.
	/// Use when arbitrarily looking for a piece of content where the naming scheme or capitalization may be inconsistent.
	/// </summary>
	/// <returns> The first valid instance of type <typeparamref name="T"/> found. </returns>
	public static bool TryFindFromArray<T>(string modName, string[] possibleNames, out T item) where T : IModType
	{
		item = default;

		for (int i = 0; i < possibleNames.Length; i++)
			if (ModContent.TryFind(modName, possibleNames[i], out T value))
			{
				item = value;
				return true;
			}

		return false;
	}

	/// <summary>
	/// Helper method that functions as <see cref="ModContent.TryFind{T}(string, out T)"/> with multiple allowed possible name inputs, while only returning one valid output.
	/// Use when arbitrarily looking for a piece of content where the naming scheme or capitalization may be inconsistent.
	/// </summary>
	/// <returns> The first valid instance of type <typeparamref name="T"/> found. </returns>
	public static bool TryFindFromArray<T>(string modName, string baseName, string[] possibleEndings, out T item) where T : IModType
	{
		item = default;

		for (int i = 0; i < possibleEndings.Length; i++)
			if (ModContent.TryFind(modName, baseName + possibleEndings[i], out T value))
			{
				item = value;
				return true;
			}

		return false;
	}
}