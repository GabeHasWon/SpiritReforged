using SpiritReforged.Common.Subclasses;

namespace SpiritReforged.Common.ItemCommon.MagazineSystem;
internal class MagazineDamageClass : SubclassClass
{
	public override StatInheritanceData GetModifierInheritance(DamageClass damageClass)
	{
		if (damageClass == Generic || damageClass == Ranged)
			return StatInheritanceData.Full;
		return StatInheritanceData.None;
	}

	public override bool GetEffectInheritance(DamageClass damageClass) => damageClass == Ranged;
}
