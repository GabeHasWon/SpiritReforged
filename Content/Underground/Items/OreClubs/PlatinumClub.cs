using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.ProjectileCommon.Abstract;

namespace SpiritReforged.Content.Underground.Items.OreClubs;

public class PlatinumClub : ClubItem
{
	internal override float DamageScaling => 1.66f;

	public override void SafeSetDefaults()
	{
		Item.damage = 26;
		Item.knockBack = 8;
		ChargeTime = 20;
		SwingTime = 30;
		Item.width = 60;
		Item.height = 60;
		Item.crit = 4;
		Item.value = Item.sellPrice(0, 0, 36, 0);
		Item.rare = ItemRarityID.White;
		Item.shoot = ModContent.ProjectileType<PlatinumClubProj>();
		Item.UseSound = BaseClubProj.DefaultSwing;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.PlatinumBar, 16).AddTile(TileID.Anvils).Register();
}