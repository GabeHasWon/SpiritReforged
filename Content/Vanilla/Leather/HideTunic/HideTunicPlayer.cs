
namespace SpiritReforged.Content.Vanilla.Leather.HideTunic;

public class HideTunicPlayer : ModPlayer
{
	public bool active;
	public float increase = 1.25f;

	public override void ResetEffects() => active = false;

	public override void ModifyWeaponKnockback(Item item, ref StatModifier knockback)
	{
		if (active && item.DamageType == DamageClass.Melee)
			knockback *= increase;
	}

	public override void ModifyItemScale(Item item, ref float scale)
	{
		if (active && item.DamageType == DamageClass.Melee)
			scale *= increase;
	}
}
