
namespace SpiritReforged.Common.WorldGeneration.GenConfiguration;

#nullable enable

public readonly record struct IndividualPreset(string Name, object Value);

/// <summary>
/// A 
/// </summary>
/// <param name="Name"></param>
/// <param name="Presets"></param>
public readonly record struct ConfigPreset(string Name, bool ResetNotIncluded, List<IndividualPreset> Presets)
{
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
	public IGenerationPage? CopiedPage { get; init; }
}

/// <summary>
/// Marks a class as one that has a generation page.
/// </summary>
public interface IGenerationPage
{
	public PageInfo Info { get; }
	public Mod Mod { get; }
}

public static class GenerationPageExtensions
{
	public static GenConfigPage GetPage(this IGenerationPage page) => GenConfigLoader.GetPage(page.GetType()); 
}