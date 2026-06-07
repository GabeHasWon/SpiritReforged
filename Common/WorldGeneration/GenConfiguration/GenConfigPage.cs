namespace SpiritReforged.Common.WorldGeneration.GenConfiguration;

/// <summary>
/// The actual loaded generation configuration page for a defined area. Includes the info and localization, plus a helper tool.
/// </summary>
public class GenConfigPage(Mod mod, PageInfo info, LocalizedText display, LocalizedText tooltip)
{
	public Mod Mod = mod;
	public PageInfo PageInfo = info;
	public LocalizedText DisplayName = display;
	public LocalizedText Tooltip = tooltip;
	public List<(LocalizedText Name, LocalizedText Tooltip)> PresetLocalization = [];

	public Dictionary<string, LoadedConfig> ConfigsByName = [];

	/// <summary>
	/// Retrieves either the modified value for the config or the default value passed in.
	/// </summary>
	public T ValueOrDefault<T>(string configName, T defaultValue) => ConfigsByName[configName].Modified ? (T)ConfigsByName[configName].Get() : defaultValue;
}
