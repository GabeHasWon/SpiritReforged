namespace SpiritReforged.Common.WorldGeneration.GenConfiguration;

/// <summary>
/// Marks a class as one that has a generation page.
/// </summary>
internal interface IGenerationPage
{
	public string PageName { get; }
	public Mod Mod { get; }
}
