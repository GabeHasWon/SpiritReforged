namespace SpiritReforged.Common.WorldGeneration.GenConfiguration;

#nullable enable

public readonly record struct PageInfo(string PageName, Asset<Texture2D>? PageBack);

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