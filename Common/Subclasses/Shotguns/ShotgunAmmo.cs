using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.DataStructures;

namespace SpiritReforged.Common.Subclasses.Shotguns;

// Dummy class for Item.ammo
// this might be silly but feels cleaner to me
public class ShotgunAmmoType : ModItem
{
	public override string Texture => AssetLoader.EmptyTexture;
}

public delegate void ShootBehavior(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 direction, int shotCount, float spreadAmount, float speed, int damage, float knockback);

/// <summary>
/// The behavior of shotgun shell ammo is completely contained in the ammo class, so any shotgun that uses the ammo does the same thing.
/// Different weapons can effect the behavior through addition and multiplication of stats, but there is general parity between them.
/// Furthermore <see cref="ShotgunPlayer"/> handles additional shotgun related stat boosts.
/// Both weapon and stat boosts are NOT handled in the ammo class. These bonuses are automatically passed into the delegate, <see cref="ShootBehavior"/>
/// Implementation of these stat changes are simply handled twice, once in the global item for vanilla / cross mod, and once in the abstract shotgun class, <see cref="ShotgunItem"/>
/// This is to avoid having to code in the stat changes in every inherited ShotgunAmmoItem.
/// </summary>
public abstract class ShotgunAmmoItem : ModItem
{
	public ShootBehavior _behavior;

	public int _shotCount;
	public float _spreadAmount;
	public float _speed;

	public ShotgunAmmoItem(ShootBehavior behavior, int shotCount, float spreadAmount, float speed)
	{
		_behavior = behavior;
		_shotCount = shotCount;
		_spreadAmount = spreadAmount;
		_speed = speed;
	}

	public sealed override void SetDefaults()
	{
		Item.ammo = ModContent.ItemType<ShotgunAmmoType>();

		Item.consumable = true;
		Item.maxStack = 9999;

		SafeSetDefaults();
	}

	public virtual void SafeSetDefaults()
	{

	}
}

// TODO: mod call for this
public class ShotgunGlobalItem : GlobalItem
{
	public static List<int> _shotgunIDs = 
		[ItemID.Boomstick,
		ItemID.QuadBarrelShotgun,
		ItemID.OnyxBlaster,
		ItemID.Shotgun,
		ItemID.TacticalShotgun,
		];

	public override bool AppliesToEntity(Item entity, bool lateInstantiation)
	{
		return _shotgunIDs.Contains(entity.type);
	}

	public override bool InstancePerEntity => true;

	public override bool? CanChooseAmmo(Item weapon, Item ammo, Player player) // allows shotguns to use vanilla behavior and our shotgun ammo
	{
		if (ammo.ammo == ModContent.ItemType<ShotgunAmmoType>())
			return true;

		return base.CanChooseAmmo(weapon, ammo, player);
	}

	public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		Item ammoItem = source.AmmoItemIdUsed > 0 ? ContentSamples.ItemsByType[source.AmmoItemIdUsed] : null;

		if (ammoItem != null && ammoItem.ModItem is ShotgunAmmoItem ammo) // override vanilla behavior
		{
			Vector2 direction = Vector2.Zero;
			if (Main.myPlayer == player.whoAmI)
				direction = position.DirectionTo(Main.MouseWorld);

			var shotgunPlayer = player.GetModPlayer<ShotgunPlayer>();

			ammo._behavior.Invoke(item, player, source, position, direction, 
				shotgunPlayer.ModifyShotCount(ammo._shotCount),
				shotgunPlayer.ModifySpread(ammo._spreadAmount),
				shotgunPlayer.ModifySpeed(ammo._speed), 
				damage, knockback);

			return false;
		}

		return base.Shoot(item, player, source, position, velocity, type, damage, knockback);
	}
}

public struct ShotgunStats
{
	public ShotgunStats(int additionalShots = 0, float shotMultiplier = 0, float additionalSpread = 0, float spreadMultiplier = 0, float additionalSpeed = 0, float speedMultiplier = 0)
	{
		_additionalShots = additionalShots;
		_shotMultiplier = shotMultiplier;
		_additionalSpread = additionalSpread;
		_spreadMultiplier = spreadMultiplier;
		_additionalSpeed = additionalSpeed;
		_speedMultiplier = speedMultiplier;
	}

	// TODO: better naming convention here
	public int _additionalShots; // can be negative to decrease shots
	public float _shotMultiplier;

	public float _additionalSpread; // can be negative to decrease spread
	public float _spreadMultiplier;

	public float _additionalSpeed;
	public float _speedMultiplier;
}

public class ShotgunPlayer : ModPlayer
{
	public ShotgunStats shotgunStats;

	public override void ResetEffects()
	{
		shotgunStats._additionalShots = 0;
		shotgunStats._shotMultiplier = 1;

		shotgunStats._additionalSpread = 0;
		shotgunStats._spreadMultiplier = 1;

		shotgunStats._additionalSpeed = 0;
		shotgunStats._speedMultiplier = 1;
	}

	/// <summary>
	/// Modifies the amount of shots a shotgun should shoot according to <see cref="ShotgunPlayer.shotgunStats"/>. Supports adding additional shots and additional multiplier (for item support as well)
	/// </summary>
	/// <param name="baseShots">The base amount of shots</param>
	/// <param name="additionalShots">Additional shots the item should add</param>
	/// <param name="additionalShotMultiplier">Additional shot multiplier the item should add</param>
	/// <returns></returns>
	public int ModifyShotCount(int baseShots, int additionalShots = 0, float additionalShotMultiplier = 0f) => Math.Max(1, (int)((baseShots + shotgunStats._additionalShots + additionalShots) * (shotgunStats._shotMultiplier + additionalShotMultiplier)));
	
	/// <summary>
	/// Modifies the amount of spread a shotgun has according to <see cref="ShotgunPlayer.shotgunStats"/>. Supports adding additional spread and additional spread multiplier (for item support as well)
	/// </summary>
	/// <param name="baseSpread">The base amount of spread</param>
	/// <param name="additionalSpread">Additional spread the item should add</param>
	/// <param name="additionalSpreadMultiplier">Additional spread multiplier the item should add</param>
	/// <returns></returns>
	public float ModifySpread(float baseSpread, float additionalSpread = 0, float additionalSpreadMultiplier = 0f) => Math.Max(0f, (baseSpread + shotgunStats._additionalSpread + additionalSpread) * (shotgunStats._spreadMultiplier + additionalSpreadMultiplier));
	
	/// <summary>
	/// Modifies the amount of speed a shotgun should add to its ammo according to <see cref="ShotgunPlayer.shotgunStats"/>. Supports adding additional speed and additional speed multiplier (for item support as well)
	/// </summary>
	/// <param name="baseSpeed">The base amount of speed</param>
	/// <param name="additionalSpeed">Additional speed the item should add</param>
	/// <param name="additionalSpeedMultiplier">Additional speed multiplier the item should add</param>
	/// <returns></returns>
	public float ModifySpeed(float baseSpeed, float additionalSpeed = 0, float additionalSpeedMultiplier = 0f) => Math.Max(1f, (baseSpeed + shotgunStats._additionalSpeed + additionalSpeed) * (shotgunStats._speedMultiplier + additionalSpeedMultiplier));
}
