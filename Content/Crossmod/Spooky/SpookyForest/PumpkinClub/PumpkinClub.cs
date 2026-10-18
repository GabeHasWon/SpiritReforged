using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.Visuals.Glowmasks;

namespace SpiritReforged.Content.Crossmod.Spooky.SpookyForest.PumpkinClub;

[AutoloadGlowmask("255,255,255")]
public class PumpkinClub : ClubItem
{
	internal override float DamageScaling => 2f;
	internal override float KnockbackScaling => 2f;

	public override void SafeSetDefaults()
	{
		Item.damage = 8;
		Item.knockBack = 5f;
		ChargeTime = 60;
		SwingTime = 35;
		Item.width = 60;
		Item.height = 60;
		Item.crit = 4;
		Item.value = Item.sellPrice(gold: 5);
		Item.rare = ItemRarityID.Green;
		Item.shoot = ModContent.ProjectileType<PumpkinClubProj>();
	}
}