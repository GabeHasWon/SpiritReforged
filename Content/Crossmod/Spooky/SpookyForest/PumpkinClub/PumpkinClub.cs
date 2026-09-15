using SpiritReforged.Common.ItemCommon.Abstract;

namespace SpiritReforged.Content.Crossmod.Spooky.SpookyForest.PumpkinClub;

// TODO: obtainment, balance
public class PumpkinClub : ClubItem
{
	internal override float DamageScaling => 1.95f;
	internal override float KnockbackScaling => 2f;

	public override void SafeSetDefaults()
	{
		Item.damage = 35;
		Item.knockBack = 5f;
		ChargeTime = 60;
		SwingTime = 35;
		Item.width = 60;
		Item.height = 60;
		Item.crit = 4;
		Item.value = Item.sellPrice(gold: 5);
		Item.rare = ItemRarityID.Orange;
		Item.shoot = ModContent.ProjectileType<PumpkinClubProj>();
	}
}