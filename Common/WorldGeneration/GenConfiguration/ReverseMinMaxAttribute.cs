namespace SpiritReforged.Common.WorldGeneration.GenConfiguration;

/// <summary>
/// Reverses the min/max values for this <see cref="GenConfigurableAttribute"/> member for the Min/Max All buttons ONLY.<br/>
/// This means "minimum" values should be the "least often", so spawn chances should be high, and max amount should be low. Max is vise-versa.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
internal class ReverseMinMaxAttribute : Attribute
{
}
