namespace SpiritReforged.Common.WorldGeneration.GenConfiguration;

#nullable enable

/// <summary>
/// An individual config's preset value, by name. If the name doesn't match the name of a config, this will throw.
/// </summary>
public readonly record struct IndividualPreset(string Name, object Value);

/// <summary>
/// An individual full-config preset. <paramref name="ResetNotIncluded"/> will reset values that aren't included in the preset.
/// </summary>
public readonly record struct ConfigPreset(string Name, bool ResetNotIncluded, List<IndividualPreset> Presets)
{
	/// <summary>
	/// Applies this preset to the given page.
	/// </summary>
	/// <param name="page"></param>
	internal readonly void Apply(GenConfigPage page)
	{
		HashSet<string> names = [];

		foreach (IndividualPreset preset in Presets)
		{
			page.ConfigsByName[preset.Name].Set(preset.Value);
			page.ConfigsByName[preset.Name].Modified = true;
			names.Add(preset.Name);
		}

		if (ResetNotIncluded)
		{
			foreach (var config in page.ConfigsByName.Values)
				if (!names.Contains(config.Name))
					config.Set(config.Default);
		}
	}
}

/// <summary>
/// Info used to create a <see cref="GenConfigPage"/>.<br/>
/// If a page already exists (or will already exist) and you want to use it, use <see cref="CopiedPage"/> to clone that type's page.
/// </summary>
public readonly record struct PageInfo(string PageName, Asset<Texture2D>? PageBack, Asset<Texture2D>? PageButton, List<ConfigPreset>? Presets = null)
{
	/// <summary>
	/// Copies the given instance's <see cref="PageInfo"/> if it is already defined.
	/// </summary>
	public IGenerationPage? CopiedPage { get; init; }
}

/// <summary>
/// Marks a class as one that has a generation page.
/// </summary>
public interface IGenerationPage
{
	/// <summary>
	/// Info used to generate a page.
	/// </summary>
	public PageInfo Info { get; }

	/// <summary>
	/// The mod associated with this page.
	/// </summary>
	public Mod Mod { get; }
}

public static class GenerationPageExtensions
{
	/// <summary>
	/// Retrieves the page associated with this <see cref="IGenerationPage"/>.
	/// </summary>
	public static GenConfigPage GetPage(this IGenerationPage page) => GenConfigLoader.GetPage(page.GetType()); 
}