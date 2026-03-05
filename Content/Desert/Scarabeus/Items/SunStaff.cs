using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Desert.Scarabeus.Items.Projectiles;

namespace SpiritReforged.Content.Desert.Scarabeus.Items;

[AutoloadGlowmask("255,255,255")]
public class SunStaff : ModItem
{
	public override bool IsLoadingEnabled(Mod mod) => false;

	public override void SetStaticDefaults()
	{
		Item.staff[Type] = true;

		MoRHelper.AddElement(Item, MoRHelper.Fire);
		MoRHelper.AddElement(Item, MoRHelper.Holy, true);
	}

	public override void SetDefaults()
	{
		Item.damage = 20;
		Item.width = Item.height = 46;
		Item.useTime = Item.useAnimation = 40;
		Item.knockBack = 1f;
		Item.shootSpeed = 0;
		Item.noMelee = true;
		Item.channel = true;
		Item.noUseGraphic = true;
		Item.DamageType = DamageClass.Magic;
		Item.mana = 30;
		Item.rare = ItemRarityID.Green;
		Item.value = Item.sellPrice(gold: 2);
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.shoot = ModContent.ProjectileType<SunStaffHeld>();
	}

	public override bool CanUseItem(Player player)
	{
		int sunOrb = ModContent.ProjectileType<SunOrb>();
		return player.ownedProjectileCounts[Item.shoot] == 0 && player.ownedProjectileCounts[sunOrb] == 0;
	}
}