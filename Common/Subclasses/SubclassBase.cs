namespace SpiritReforged.Common.Subclasses;

#nullable enable

/// <summary>
/// Defines a subclass's damage type. This is meant to simplify usage and allow for subclass-specific damage to be modified easily.
/// </summary>
internal abstract class SubclassClass : DamageClass
{
	/// <summary>
	/// Workaround class solely for trimming the single space off of vanilla's damage text, which is formatted as " summon damage", including the space.
	/// </summary>
	internal class SubclassTooltipTrim : GlobalItem
	{
		public override bool AppliesToEntity(Item entity, bool lateInstantiation) => entity.DamageType is SubclassClass sub && sub.TrimDamageTextSpaces;
		
		public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
		{
			if (tooltips.FindIndex(x => x.Name == "Damage") is { } value and not -1 && item.DamageType is SubclassClass subclass && subclass.TrimDamageTextSpaces)
				tooltips[value].Text = item.damage + subclass.DisplayName.Value;
		}
	}

	/// <summary>
	/// Forces the subclass damage tooltip to be trimmed.
	/// </summary>
	protected virtual bool TrimDamageTextSpaces => true;
}