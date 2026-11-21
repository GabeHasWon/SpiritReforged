using Mono.Cecil;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Forest.Ammo;
using SpiritReforged.Content.Particles;
using Terraria;
using Terraria.DataStructures;
using Terraria.WorldBuilding;
using static System.Net.Mime.MediaTypeNames;

namespace SpiritReforged.Common.Subclasses.Shotguns;

/*/// <summary>
/// The behavior of shotgun shell ammo is completely contained in the ammo class, so any shotgun that uses the ammo does the same thing.
/// Different weapons can effect the behavior through addition and multiplication of stats, but there is general parity between them.
/// Furthermore <see cref="ShotgunPlayer"/> handles additional shotgun related stat boosts.
/// Both weapon and stat boosts are NOT handled in the ammo class. These bonuses are automatically passed into the delegate, <see cref="ShootBehavior"/>
/// Implementation of these stat changes are simply handled twice, once in the global item for vanilla / cross mod, and once in the abstract shotgun class, <see cref="ShotgunItem"/>
/// This is to avoid having to code in the stat changes in every inherited ShotgunAmmoItem.
/// </summary>
public abstract class ShotgunAmmoItem(ShootBehavior behavior, int shotCount, float spreadAmount, float speed) : ModItem
{
	public readonly ShootBehavior Behaviour = behavior;

	public int shotCount = shotCount;
	public float spreadAmount = spreadAmount;

	public override void SetDefaults()
	{
		Item.DamageType = ModContent.GetInstance<ShotgunClass>();
		Item.ammo = ModContent.ItemType<Shot>();
		Item.maxStack = Item.CommonMaxStack;
		Item.consumable = true;
	}
}*/

public readonly record struct StatModifiers
{
	public readonly StatModifier ShotCount;
	public readonly StatModifier ShotSpread;

	/// <summary> Creates a new instance designed for modifying existing stats. </summary>
	public StatModifiers(StatModifier ShotCount, StatModifier ShotSpread)
	{
		this.ShotCount = ShotCount;
		this.ShotSpread = ShotSpread;
	}

	/// <summary> Creates a new instance designed for initializing base stats. </summary>
	public StatModifiers(int ShotCount, float ShotSpread)
	{
		this.ShotCount = new StatModifier(1, 1, 0, ShotCount);
		this.ShotSpread = new StatModifier(1, 1, 0, ShotSpread);
	}

	public readonly int GetShotCount(Player player, params StatModifiers[] additionalModifiers)
	{
		StatModifier initial = ShotCount;

		for (int i = 0; i < additionalModifiers.Length; i++)
			initial = initial.CombineWith(additionalModifiers[i].ShotCount);

		return (int)player.GetModPlayer<ShotgunStatsPlayer>().shotCount.CombineWith(initial).ApplyTo(1);
	}

	public readonly float GetShotSpread(Player player, params StatModifiers[] additionalModifiers)
	{
		StatModifier initial = ShotSpread;

		for (int i = 0; i < additionalModifiers.Length; i++)
			initial = initial.CombineWith(additionalModifiers[i].ShotSpread);

		return player.GetModPlayer<ShotgunStatsPlayer>().shotSpread.CombineWith(initial).ApplyTo(1);
	}
}

public interface IShotgunAmmo
{
	public StatModifiers Modifiers { get; }

	/// <summary> Called for <b>EVERY</b> shot according to <see cref="Modifiers"/> when <paramref name="item"/> is fired.<para/>
	/// Can be used to create shoot effects unique to this ammo item. <see cref="Modifiers"/> should be used to retrieve shot counts and spread if needed. </summary>
	/// <param name="item"> The item that created the projectile. </param>
	/// <param name="player"> The player who created the projectile. </param>
	/// <param name="source"> The entity source. </param>
	/// <param name="position"> The spawn position. </param>
	/// <param name="velocity"> The velocity of the projectile, automatically adjusted by spread. </param>
	/// <param name="type"> The type of projectile. </param>
	/// <param name="damage"> The projectile damage. </param>
	/// <param name="knockback"> The projectile knockback. </param>
	/// <param name="index"> The index of projectile created (by zero) relative to <see cref="StatModifiers.GetShotCount"/>. </param>
	public Projectile OnShoot(Item item, Player player, IEntitySource source, Vector2 position, Vector2 velocity, int type, int damage, float knockback, int index);
}

[ReinitializeDuringResizeArrays]
public class ShotgunGlobalItem : GlobalItem
{
	/// <summary> The shotgun stats of this item.<para/>Only necessary for vanilla types to exhibit shotgun behaviour.<br/>Modded items may dynamically call <see cref="ShotgunShoot"/> instead.</summary>
	public static readonly StatModifiers?[] Shotgun = SpiritSets.ItemFactory.CreateNamedSet(nameof(Shotgun)).Description("Whether this item will be considered a shotgun and use shotgun stats").RegisterCustomSet<StatModifiers?>(null);

	public override void SetStaticDefaults() //Set vanilla shotgun defaults
	{
		Shotgun[ItemID.Boomstick] = new();
		Shotgun[ItemID.QuadBarrelShotgun] = new(new StatModifier(1.5f, 1), new StatModifier(1.5f, 1));
		Shotgun[ItemID.OnyxBlaster] = new(StatModifier.Default, new StatModifier(0.8f, 1)); //Projectile speed mult = +0.25f
		Shotgun[ItemID.Shotgun] = new();
		Shotgun[ItemID.TacticalShotgun] = new(StatModifier.Default, new StatModifier(0.65f, 1));
	}

	public override void SetDefaults(Item entity)
	{
		if (entity.type < ItemID.Count && Shotgun[entity.type] != null) //Force all vanilla shotguns to use the appropriate ammo type
		{
			entity.DamageType = ModContent.GetInstance<ShotgunClass>();
			entity.useAmmo = ModContent.ItemType<Shot>();
		}
	}

	public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (item.type < ItemID.Count && Shotgun[item.type] != null)
		{
			StatModifiers itemStats = Shotgun[item.type].Value;
			Projectile[] projectiles = ShotgunShoot(itemStats, source,  position, velocity, type, damage, knockback);

			if (projectiles.Length > 0 && ContentSamples.ItemsByType[source.AmmoItemIdUsed].ModItem is IShotgunAmmo ammo)
			{
				Vector2 normalized = velocity.SafeNormalize(Vector2.UnitX);
				Vector2 shellPos = position + new Vector2(item.width / 2, -8 * player.direction).RotatedBy(velocity.ToRotation());

				ParticleRenderers.OverPlayers.Add(new ShotgunShellParticle(shellPos, -normalized * Main.rand.NextFloat(3f, 5f) - Vector2.UnitY * Main.rand.NextFloat(2f), 1f, 60, ammo));

				for (int i = 0; i < 4; i++)
					Dust.NewDustPerfect(shellPos, DustID.Torch, -normalized * Main.rand.NextFloat(3f, 5f) - Vector2.UnitY * Main.rand.NextFloat(2f), 0, default, Main.rand.NextFloat(2f));
			}
		}

		return true;
	}

	/// <summary> Shoots a spread of projectile indicated by the provided <paramref name="itemStats"/>. </summary>
	/// <param name="itemStats"></param>
	/// <param name="source"></param>
	/// <param name="position"></param>
	/// <param name="velocity"></param>
	/// <param name="type"></param>
	/// <param name="damage"></param>
	/// <param name="knockback"></param>
	/// <returns></returns>
	public static Projectile[] ShotgunShoot(StatModifiers itemStats, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		Player player = source.Player;
		Item item = source.Item;
		List<Projectile> projectiles = [];

		if (ContentSamples.ItemsByType[source.AmmoItemIdUsed].ModItem is IShotgunAmmo ammo)
		{
			StatModifiers ammoStats = ammo.Modifiers;

			for (int i = 0; i < ammoStats.GetShotCount(player, itemStats); i++)
				projectiles.Add(ammo.OnShoot(item, player, source, position, velocity.RotatedByRandom(ammoStats.GetShotSpread(player, itemStats)), type, damage, knockback, i));

			if (item.type < ItemID.Count) //Vanilla-only effects
			{
				Vector2 normalized = velocity.SafeNormalize(Vector2.UnitX);
				Vector2 shellPos = position + new Vector2(item.width / 2, -8 * player.direction).RotatedBy(velocity.ToRotation());

				ParticleRenderers.OverPlayers.Add(new ShotgunShellParticle(shellPos, -normalized * Main.rand.NextFloat(3f, 5f) - Vector2.UnitY * Main.rand.NextFloat(2f), 1f, 60, ammo));

				for (int i = 0; i < 4; i++)
					Dust.NewDustPerfect(shellPos, DustID.Torch, -normalized * Main.rand.NextFloat(3f, 5f) - Vector2.UnitY * Main.rand.NextFloat(2f), 0, default, Main.rand.NextFloat(2f));
			}
		}

		return projectiles.ToArray();
	}
}

public class ShotgunStatsPlayer : ModPlayer
{
	public StatModifier shotCount = StatModifier.Default;
	public StatModifier shotSpread = StatModifier.Default;

	public override void ResetEffects()
	{
		shotCount = StatModifier.Default;
		shotSpread = StatModifier.Default;
	}
}