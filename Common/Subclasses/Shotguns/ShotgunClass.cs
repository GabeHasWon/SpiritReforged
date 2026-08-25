namespace SpiritReforged.Common.Subclasses.Greatshields;

internal class ShotgunClass : SubclassClass
{
	public override StatInheritanceData GetModifierInheritance(DamageClass damageClass)
	{
		if (damageClass == Generic || damageClass == Melee)
			return StatInheritanceData.Full;

		return StatInheritanceData.None;
	}

	public override bool GetEffectInheritance(DamageClass damageClass) => damageClass == Ranged;
}