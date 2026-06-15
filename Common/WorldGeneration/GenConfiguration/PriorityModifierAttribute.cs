namespace SpiritReforged.Common.WorldGeneration.GenConfiguration;

/// <summary>
/// Allows a config to be sorted directly after the named field.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
internal class PriorityModifierAttribute(string parentName) : Attribute
{
	public readonly string ParentName = parentName;
}
