namespace SpiritReforged.Common.WorldGeneration.GenConfiguration;

public class GenConfigPage(string name, LocalizedText display, LocalizedText tooltip)
{
	public string Name = name;
	public LocalizedText DisplayName = display;
	public LocalizedText Tooltip = tooltip;

	public Dictionary<string, LoadedConfig> ConfigsByName = [];

	public bool ConfigModified(string name) => ConfigsByName[name].Modified;
	public void ModifyConfig(string name) => ConfigsByName[name].Modified = true;

	public T ValueOrDefault<T>(string configName, T defaultValue) => ConfigsByName[configName].Modified ? (T)ConfigsByName[configName].Get() : defaultValue;

	/// <summary>
	/// Sets all the values on the page to the defaults automatically. This should be run once during generation.
	/// </summary>
	public void SetupPage()
	{
		foreach (var config in ConfigsByName.Values)
		{
			if (!config.Modified)
				config.Set(config.Default);
		}
	}
}
