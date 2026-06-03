namespace SpiritReforged.Common.WorldGeneration.GenConfiguration;

#nullable enable

/// <summary>
/// Marks a property or field as one that can be configured. The page associated is provided.<br/>
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
internal class GenConfigurableAttribute(object min, object max, object? step = null) : Attribute
{
	public readonly object Min = min;
	public readonly object Max = max;
	public readonly object? Step = step;
}
