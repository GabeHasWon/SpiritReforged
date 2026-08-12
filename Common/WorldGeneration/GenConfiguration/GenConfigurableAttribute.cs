namespace SpiritReforged.Common.WorldGeneration.GenConfiguration;

#nullable enable

/// <summary>
/// Marks a property or field as one that can be configured, alongside their minimum, maximum and step (if any).
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class GenConfigurableAttribute(object min, object max, object? step = null) : Attribute
{
	public readonly object Min = min;
	public readonly object Max = max;
	public readonly object? Step = step;

	/// <summary>
	/// Overload for simplifying booleans.
	/// </summary>
	public GenConfigurableAttribute(bool value) : this(false, true, true)
	{
		
	}
}

/// <summary>
/// Marks a <see cref="GenConfigurableAttribute"/> member as one that's a "denominator" - the value is 1/x, not just x.<br/>
/// This should clarify to players that the lower the value is, the more common it is, not the other way around.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class DenominatorAttribute : Attribute
{
}

/// <summary>
/// Allows a config to be sorted directly after the named field.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public class PriorityModifierAttribute(string parentName) : Attribute
{
	public readonly string ParentName = parentName;
}

/// <summary>
/// Reverses the min/max values for this <see cref="GenConfigurableAttribute"/> member for the Min/Max All buttons ONLY.<br/>
/// This means "minimum" values should be the "least often", so spawn chances should be high, and max amount should be low. Max is vise-versa.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class ReverseMinMaxAttribute : Attribute
{
}
