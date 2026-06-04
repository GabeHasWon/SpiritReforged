namespace SpiritReforged.Common.WorldGeneration.GenConfiguration;

/// <summary>
/// Marks a class as one that has a generation page.
/// </summary>
public interface IGenerationPage
{
	public string PageName { get; }
	public Mod Mod { get; }
}

public static class GenerationPageExtensions
{
	public static GenConfigPage GetPage(this IGenerationPage page) => GenConfigLoader.GetPage(page.GetType()); 
}