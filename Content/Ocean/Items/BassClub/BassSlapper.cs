using SpiritReforged.Common.ItemCommon;

namespace SpiritReforged.Content.Ocean.Items.BassClub;

public class BassSlapper : ClubItem
{
	internal override float DamageScaling => 1.25f;

	public override void SafeSetDefaults()
	{
		Item.damage = 26;
		Item.knockBack = 14;
		ChargeTime = 50;
		SwingTime = 26;
		Item.width = 60;
		Item.height = 60;
		Item.crit = 4;
		Item.value = Item.sellPrice(0, 0, 60, 0);
		Item.rare = ItemRarityID.Green;
		Item.shoot = ModContent.ProjectileType<BassSlapperProj>();
	}
}