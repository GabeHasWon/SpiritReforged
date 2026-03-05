using SpiritReforged.Content.Desert.Scarabeus.Items.Projectiles;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.Scarabeus.Items;

public class AdornedBow : ModItem
{
	public override void SetDefaults()
	{
		Item.damage = 28;
		Item.Size = new Vector2(48, 52);
		Item.useTime = Item.useAnimation = 120;
		Item.knockBack = 1f;
		Item.noMelee = true;
		Item.channel = true;
		Item.noUseGraphic = true;
		Item.DamageType = DamageClass.Ranged;
		Item.useTurn = false;
		Item.autoReuse = true;
		Item.rare = ItemRarityID.Green;
		Item.value = Item.sellPrice(gold: 2);
		Item.useStyle = ItemUseStyleID.Swing;
		Item.shoot = ModContent.ProjectileType<AdornedBowHeld>();
		Item.shootSpeed = 8;
		Item.useAmmo = AmmoID.Arrow;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		int useTime = (int)(Item.useTime / player.GetTotalAttackSpeed(DamageClass.Ranged));
		Projectile.NewProjectileDirect(source, position, Vector2.Zero, Item.shoot, damage, knockback, player.whoAmI, 0, useTime, type);
		return false;
	}

	public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] == 0;
}