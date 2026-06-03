namespace SpiritReforged.Common.WorldGeneration.GenConfiguration;

internal class GenConfigPage(string name, LocalizedText display, LocalizedText tooltip)
{
	public string Name = name;
	public LocalizedText DisplayName = display;
	public LocalizedText Tooltip = tooltip;

	public List<LoadedConfig> Configs = [];
}
