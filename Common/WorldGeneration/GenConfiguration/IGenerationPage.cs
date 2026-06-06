
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
/// Info used to create a <see cref="GenConfigPage"/>.
/// </summary>
public readonly record struct PageInfo(string PageName, Asset<Texture2D>? PageBack, List<ConfigPreset>? Presets = null);

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