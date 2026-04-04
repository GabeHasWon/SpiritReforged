namespace SpiritReforged.Common.Subclasses;

#nullable enable

internal abstract class SubclassClass : DamageClass
{
	public override bool UseStandardCritCalcs => true;

	public LocalizedText DamageText = null!;

	public override void Load() => DamageText = Language.GetOrRegister("Mods.SpiritReforged." + Name + "_Class.DisplayName", () => Name.ToLower() + " damage");
}