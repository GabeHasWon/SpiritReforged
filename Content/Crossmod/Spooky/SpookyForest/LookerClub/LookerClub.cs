using SpiritReforged.Common.ItemCommon.Abstract;

namespace SpiritReforged.Content.Crossmod.Spooky.SpookyForest.LookerClub;

// TODO: obtainment
public class LookerClub : ClubItem
{
	internal override float DamageScaling => 2f;
	internal override float KnockbackScaling => 1.4f;

	public override void SafeSetDefaults()
	{
		Item.damage = 20;
		Item.knockBack = 5;
		ChargeTime = 40;
		SwingTime = 24;
		Item.width = 60;
		Item.height = 60;
		Item.crit = 4;
		Item.value = Item.sellPrice(0, 0, 0, 5);
		Item.rare = ItemRarityID.White;
		Item.shoot = ModContent.ProjectileType<LookerClubProj>();
	}
}