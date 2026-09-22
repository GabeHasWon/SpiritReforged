using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Subclasses.Greatshields;
using SpiritReforged.Content.Particles;
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

/// <summary>
/// The shoot behavior of the ammo item
/// </summary>
/// <param name="item">The item using the ammo</param>
/// <param name="player">The player using the ammo</param>
/// <param name="source">Passed in from ModItem.Shoot</param>
/// <param name="position">Where the projectiles will be spawned</param>
/// <param name="direction">The direction of the projectiles velocity</param>
/// <param name="shotCount">The amount of shots to shoot</param>
/// <param name="spreadAmount">The max spread of the shots</param>
/// <param name="speed">The speed of the shots</param>
/// <param name="damage">The damage of the shots</param>
/// <param name="knockback">The knockback of the shots</param>
/// <returns>A list of the projectiles spawned</returns>
public delegate List<Projectile> ShootBehavior(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 direction, int shotCount, float spreadAmount, float speed, int damage, float knockback);

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
		Item.DamageType = ModContent.GetInstance<ShotgunClass>();
		Item.ammo = ModContent.ItemType<ShotgunAmmoType>();

		Item.consumable = true;
		Item.maxStack = 9999;

		SafeSetDefaults();
	}

	public virtual void SafeSetDefaults()
	{

	}
}

// TODO: mod call to add things to this
public class ShotgunGlobalItem : GlobalItem
{
	public static List<int> _shotgunIDs = 
		[ItemID.Boomstick,
		ItemID.QuadBarrelShotgun,
		ItemID.OnyxBlaster,
		ItemID.Shotgun,
		ItemID.TacticalShotgun,
		];

	public Dictionary<int, ShotgunStats> _shotgunStats = [];

	public override void Load()
	{
		// default stats
		_shotgunStats.Add(ItemID.Boomstick, new());
		// 50% more shots, 50% more spread
		_shotgunStats.Add(ItemID.QuadBarrelShotgun, new(shotMultiplier: 0.5f, spreadMultiplier: 0.5f));
		// 25% more speed, 20% less spread
		_shotgunStats.Add(ItemID.OnyxBlaster, new(speedMultiplier: 0.25f, spreadMultiplier: -0.2f));
		// default stats
		_shotgunStats.Add(ItemID.Shotgun, new());
		// 35% less spread
		_shotgunStats.Add(ItemID.TacticalShotgun, new(spreadMultiplier: -0.35f));
	}

	public override void Unload() => _shotgunStats = null;

	public override bool AppliesToEntity(Item entity, bool lateInstantiation) => _shotgunIDs.Contains(entity.type);

	public override bool InstancePerEntity => true;

	public override bool? CanChooseAmmo(Item weapon, Item ammo, Player player) // allows shotguns to only use shot
	{
		if (ammo.ammo == ModContent.ItemType<ShotgunAmmoType>())
			return true;

		return false;
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

			bool found = _shotgunStats.TryGetValue(item.type, out var stats);

			var shotgunStats = found ? stats : new ShotgunStats();

			ammo._behavior.Invoke(item, player, source, position, direction,
				shotgunPlayer.ModifyShotCount(ammo._shotCount, shotgunStats._additionalShots, shotgunStats._shotMultiplier),
				shotgunPlayer.ModifySpread(ammo._spreadAmount, shotgunStats._additionalSpread, shotgunStats._spreadMultiplier),
				shotgunPlayer.ModifySpeed(ammo._speed, shotgunStats._additionalSpeed, shotgunStats._speedMultiplier),
				damage, knockback);

			Vector2 normalized = velocity.SafeNormalize(Vector2.UnitX);
			Vector2 shellPos = position + new Vector2(item.width / 2, -8 * player.direction).RotatedBy(velocity.ToRotation());

			ParticleHandler.SpawnParticle(new ShotgunShellParticle(shellPos,
				-normalized * Main.rand.NextFloat(3f, 5f) - Vector2.UnitY * Main.rand.NextFloat(2f), 1f, 60, ammo));

			for (int i = 0; i < 4; i++)
			{
				Dust.NewDustPerfect(shellPos, DustID.Torch, -normalized * Main.rand.NextFloat(3f, 5f) - Vector2.UnitY * Main.rand.NextFloat(2f), 0, default, Main.rand.NextFloat(2f));
			}

			return true;
		}

		return base.Shoot(item, player, source, position, velocity, type, damage, knockback);
	}
}

public class ShotgunGlobalProjectile : GlobalProjectile
{
	// list of all projectiles used by ammos with the musket ball ammo type
	// we use this to manually delete shotgun bullets from vanilla as we override them but want to keep additional behavior (such as onyx blaster)
	public static List<int> musketBallProjectiles = new();

	public override void SetStaticDefaults()
	{
		foreach ((int id, Item item) in ContentSamples.ItemsByType)
		{
			if (id == AmmoID.Bullet)
				musketBallProjectiles.Add(item.shoot);
		}

		musketBallProjectiles.Add(ProjectileID.PurificationPowder);
	}

	public override void OnSpawn(Projectile projectile, IEntitySource source)
	{
		// if we are a (vanilla / modded opt in) shotgun
		if (source is EntitySource_ItemUse_WithAmmo ammoSource && ShotgunGlobalItem._shotgunIDs.Contains(ammoSource.Item.type))
		{
			// Destroy all musket ball projectiles
			if (musketBallProjectiles.Contains(projectile.type))
				projectile.Kill();
		}
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
	/// Calculates the amount of shots the players held shotgun item would shoot when used with <paramref name="ammo"/>, taking bonuses into account.
	/// </summary>
	/// <param name="ammo"></param>
	/// <returns>-1, if no shotgun item was found, otherwise the amount of shots</returns>
	public int GetShotCount(ShotgunAmmoItem ammo)
	{
		if (Player.HeldItem.ModItem is not ShotgunItem shotGunItem)
			return -1;

		return ModifyShotCount(ammo._shotCount, shotGunItem.shotgunStats._additionalShots, shotGunItem.shotgunStats._shotMultiplier);
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
	/// Calculates the spread amount the players held shotgun item would have when used with <paramref name="ammo"/>, taking bonuses into account.
	/// </summary>
	/// <param name="ammo"></param>
	/// <returns>-1, if no shotgun item was found, otherwise the amount of spread</returns>
	public float GetSpreadAmount(ShotgunAmmoItem ammo)
	{
		if (Player.HeldItem.ModItem is not ShotgunItem shotGunItem)
			return -1;

		return ModifySpread(ammo._spreadAmount, shotGunItem.shotgunStats._additionalSpread, shotGunItem.shotgunStats._spreadMultiplier);
	}

	/// <summary>
	/// Modifies the amount of spread a shotgun has according to <see cref="ShotgunPlayer.shotgunStats"/>. Supports adding additional spread and additional spread multiplier (for item support as well)
	/// </summary>
	/// <param name="baseSpread">The base amount of spread</param>
	/// <param name="additionalSpread">Additional spread the item should add</param>
	/// <param name="additionalSpreadMultiplier">Additional spread multiplier the item should add</param>
	/// <returns></returns>
	public float ModifySpread(float baseSpread, float additionalSpread = 0, float additionalSpreadMultiplier = 0f) => Math.Max(0f, (baseSpread + shotgunStats._additionalSpread + additionalSpread) * (shotgunStats._spreadMultiplier + additionalSpreadMultiplier));
	
	/// <summary>
	/// Calculates the shoot speed the players held shotgun item would have when used with <paramref name="ammo"/>, taking bonuses into account.
	/// </summary>
	/// <param name="ammo"></param>
	/// <returns>-1, if no shotgun item was found, otherwise the amount of spread</returns>
	public float GetSpeedAmount(ShotgunAmmoItem ammo)
	{
		if (Player.HeldItem.ModItem is not ShotgunItem shotGunItem)
			return -1;

		return ModifySpread(ammo._speed, shotGunItem.shotgunStats._additionalSpeed, shotGunItem.shotgunStats._speedMultiplier);
	}

	/// <summary>
	/// Modifies the amount of speed a shotgun should add to its ammo according to <see cref="ShotgunPlayer.shotgunStats"/>. Supports adding additional speed and additional speed multiplier (for item support as well)
	/// </summary>
	/// <param name="baseSpeed">The base amount of speed</param>
	/// <param name="additionalSpeed">Additional speed the item should add</param>
	/// <param name="additionalSpeedMultiplier">Additional speed multiplier the item should add</param>
	/// <returns></returns>
	public float ModifySpeed(float baseSpeed, float additionalSpeed = 0, float additionalSpeedMultiplier = 0f) => Math.Max(1f, (baseSpeed + shotgunStats._additionalSpeed + additionalSpeed) * (shotgunStats._speedMultiplier + additionalSpeedMultiplier));
}
