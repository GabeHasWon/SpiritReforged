using SpiritReforged.Common.ItemCommon.Abstract;

namespace SpiritReforged.Content.Crossmod.Spooky.Items.ShovelClub;

public class ShovelClub : ClubItem
{
	internal override float DamageScaling => 2f;
	internal override float KnockbackScaling => 1f;

	public override void SafeSetDefaults()
	{
		Item.damage = 15;
		Item.knockBack = 5f;
		ChargeTime = 60;
		SwingTime = 25;
		Item.width = 60;
		Item.height = 60;
		Item.crit = 4;
		Item.value = Item.sellPrice(gold: 1);
		Item.rare = ItemRarityID.Green;
		Item.shoot = ModContent.ProjectileType<ShovelClubProj>();
	}
}
