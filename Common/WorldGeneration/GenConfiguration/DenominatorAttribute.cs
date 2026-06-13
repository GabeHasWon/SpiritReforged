namespace SpiritReforged.Common.WorldGeneration.GenConfiguration;

/// <summary>
/// Marks a <see cref="GenConfigurableAttribute"/> member as one that's a "denominator" - the value is 1/x, not just x.<br/>
/// This should clarify to players that the lower the value is, the more common it is, not the other way around.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
internal class DenominatorAttribute : Attribute
{
}
