namespace SpiritReforged.Common.Subclasses.Greatshields;

internal class GreatshieldClass : SubclassClass
{
	public override StatInheritanceData GetModifierInheritance(DamageClass damageClass)
	{
		if (damageClass == Generic || damageClass == Melee)
			return StatInheritanceData.Full;

		return StatInheritanceData.None;
	}

	public override bool GetEffectInheritance(DamageClass damageClass) => damageClass == Melee;

	public override void SetDefaultStats(Player player) => player.GetDamage<GreatshieldClass>().Flat += player.TryGetModPlayer(out GreatshieldPlayer shield) ? shield.LastDefense : 0;
}
