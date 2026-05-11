namespace SpiritReforged.Common.Subclasses.Magazine;

internal class MagazineClass : SubclassClass
{
	public override StatInheritanceData GetModifierInheritance(DamageClass damageClass)
	{
		if (damageClass == Generic || damageClass == Ranged)
			return StatInheritanceData.Full;

		return StatInheritanceData.None;
	}

	public override bool GetEffectInheritance(DamageClass damageClass) => damageClass == Ranged;
}

