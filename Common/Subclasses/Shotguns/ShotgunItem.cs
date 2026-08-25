using SpiritReforged.Common.Subclasses.Greatshields;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.DataStructures;

namespace SpiritReforged.Common.Subclasses.Shotguns;
public abstract class ShotgunItem(ShotgunStats stats) : ModItem
{
	public ShotgunStats shotgunStats = stats;

	public sealed override void SetDefaults()
	{
		SafeSetDefaults();

		Item.DamageType = ModContent.GetInstance<ShotgunClass>();

		Item.shoot = ProjectileID.PurificationPowder;
		Item.useAmmo = ModContent.ItemType<ShotgunAmmoType>();
	}

	public virtual void SafeSetDefaults()
	{

	}

	public sealed override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		Item ammoItem = source.AmmoItemIdUsed > 0 ? ContentSamples.ItemsByType[source.AmmoItemIdUsed] : null;

		if (ammoItem != null && ammoItem.ModItem is ShotgunAmmoItem ammo)
		{
			Vector2 direction = Vector2.Zero;
			if (Main.myPlayer == player.whoAmI)
				direction = position.DirectionTo(Main.MouseWorld);

			var shotgunPlayer = player.GetModPlayer<ShotgunPlayer>();

			ammo._behavior.Invoke(Item, player, source, position, direction, 
				shotgunPlayer.ModifyShotCount(ammo._shotCount, shotgunStats._additionalShots, shotgunStats._shotMultiplier),
				shotgunPlayer.ModifySpread(ammo._spreadAmount, shotgunStats._additionalSpread, shotgunStats._spreadMultiplier),
				shotgunPlayer.ModifySpeed(ammo._speed, shotgunStats._additionalSpeed, shotgunStats._speedMultiplier), 
				damage, knockback);
		}

		return false;
	}

	/// <summary>
	/// For any behavior that should happen on top of ammo shooting behavior.
	/// </summary>
	public virtual void AdditionalShoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) { }
}
