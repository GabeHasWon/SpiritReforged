using SpiritReforged.Common.ItemCommon.Abstract;

namespace SpiritReforged.Content.Crossmod.Spooky.SpookyForest.LookerClub;

// TODO: obtainment
public class LookerClub : ClubItem
{
	internal override float DamageScaling => 2.5f;
	internal override float KnockbackScaling => 2f;

	public override void SafeSetDefaults()
	{
		Item.damage = 30;
		Item.knockBack = 5;
		ChargeTime = 70;
		SwingTime = 27;
		Item.width = 60;
		Item.height = 60;
		Item.crit = 4;
		Item.value = Item.sellPrice(0, 0, 0, 5);
		Item.rare = ItemRarityID.White;
		Item.shoot = ModContent.ProjectileType<LookerClubProj>();
	}
}