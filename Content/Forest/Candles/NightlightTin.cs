namespace SpiritReforged.Content.Forest.Candles;

public class NightlightTin : NightlightLead
{
	public override void SetDefaults()
	{
		base.SetDefaults();

		Item.DefaultToMagicWeapon(ModContent.ProjectileType<NightlightFireball>(), 15, 10, true);
		Item.damage = 9;
	}
}