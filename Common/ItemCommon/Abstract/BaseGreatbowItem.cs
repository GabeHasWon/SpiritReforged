using SpiritReforged.Common.Misc;
using Terraria.DataStructures;

namespace SpiritReforged.Common.ItemCommon.Abstract;

public abstract class BaseGreatbowItem : ModItem
{
	private static int ProjType;

	public override void SetStaticDefaults()
	{
		TryFindHeldProjectile(out ModProjectile shoot);
		if (shoot != null)
			ProjType = shoot.Type;

		SafeSetStaticDefaults();
	}

	public override void SetDefaults()
	{
		Item.useTime = Item.useAnimation = 60;
		Item.knockBack = 1f;
		Item.noMelee = true;
		Item.channel = true;
		Item.noUseGraphic = true;
		Item.DamageType = DamageClass.Ranged;
		Item.useTurn = false;
		Item.autoReuse = false;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.shootSpeed = 10;
		Item.useAmmo = AmmoID.Arrow;
		Item.shoot = ProjType;

		SafeSetDefaults();
	}

	private void TryFindHeldProjectile(out ModProjectile shoot)
	{
		string filePath = Name;
		if (filePath.Contains("Item"))
			filePath = filePath[..^4];

		ContentUtils.TryFindFromArray(Mod.Name, filePath, ["Held", "held", "Proj", "proj", "Projectile", "projectile"], out ModProjectile projectile);
		shoot = projectile;
	}

	internal virtual void SafeSetStaticDefaults() { }

	internal abstract void SafeSetDefaults();

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		int useTime = (int)(Item.useTime / player.GetTotalAttackSpeed(DamageClass.Ranged));
		Projectile.NewProjectileDirect(source, position, Vector2.Zero, Item.shoot, damage, knockback, player.whoAmI, 0, useTime, type);
		return false;
	}

	public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] == 0;
}
