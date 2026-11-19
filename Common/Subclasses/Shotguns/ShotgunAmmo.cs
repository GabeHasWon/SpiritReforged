using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Forest.Ammo;
using SpiritReforged.Content.Particles;
using Terraria.DataStructures;

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

public interface IShotgunAmmo
{
	public readonly record struct ShotgunStats(int ShotCount, float ShotSpread)
	{
		public readonly int GetShotCount(Player player) => (int)player.GetModPlayer<ShotgunPlayer>().shotCount.ApplyTo(ShotCount);

		public readonly float GetShotSpread(Player player) => player.GetModPlayer<ShotgunPlayer>().shotSpread.ApplyTo(ShotSpread);
	}

	public ShotgunStats Stats { get; }

	/// <summary> Called for <b>EVERY</b> shot according to <see cref="Stats"/> when <paramref name="item"/> is fired.<para/>
	/// Can be used to create shoot effects unique to this ammo item. <see cref="Stats"/> should be used to retrieve shot counts and spread if needed. </summary>
	/// <param name="item"> The item that created the projectile. </param>
	/// <param name="player"> The player who created the projectile. </param>
	/// <param name="source"> The entity source. </param>
	/// <param name="position"> The spawn position. </param>
	/// <param name="velocity"> The velocity of the projectile, automatically adjusted by spread. </param>
	/// <param name="type"> The type of projectile. </param>
	/// <param name="damage"> The projectile damage. </param>
	/// <param name="knockback"> The projectile knockback. </param>
	/// <param name="index"> The index of projectile created (by zero) relative to <see cref="ShotgunStats.GetShotCount"/>. </param>
	public void OnShoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback, int index);
}

public class ShotgunGlobalItem : GlobalItem
{
	public static readonly IShotgunAmmo.ShotgunStats?[] Shotgun = SpiritSets.ItemFactory.CreateNamedSet(nameof(Shotgun)).Description("Whether this item will be considered a shotgun and use shotgun stats").RegisterCustomSet<IShotgunAmmo.ShotgunStats?>(null);

	public override void SetStaticDefaults() //Set vanilla shotgun defaults
	{
		Shotgun[ItemID.Boomstick] = new();
		Shotgun[ItemID.QuadBarrelShotgun] = new(3, 0.5f);
		Shotgun[ItemID.OnyxBlaster] = new(0, -0.2f); //Projectile speed mult = +0.25f
		Shotgun[ItemID.Shotgun] = new();
		Shotgun[ItemID.TacticalShotgun] = new(0, -0.35f);
	}

	public override void SetDefaults(Item entity)
	{
		if (entity.type < ItemID.Count && Shotgun[entity.type] != null) //Force all vanilla shotguns to use the appropriate ammo type
			entity.useAmmo = ModContent.ItemType<Shot>();
	}

	public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (ContentSamples.ItemsByType[source.AmmoItemIdUsed].ModItem is IShotgunAmmo ammo)
		{
			IShotgunAmmo.ShotgunStats ammoStats = ammo.Stats;
			IShotgunAmmo.ShotgunStats itemStats = Shotgun[item.type].Value;

			for (int i = 0; i < ammoStats.GetShotCount(player) + itemStats.GetShotCount(player); i++)
				ammo.OnShoot(item, player, source, position, velocity.RotatedByRandom(ammoStats.GetShotSpread(player) + itemStats.GetShotSpread(player)), type, damage, knockback, i);

			if (item.type < ItemID.Count) //Vanilla-only effects
			{
				Vector2 normalized = velocity.SafeNormalize(Vector2.UnitX);
				Vector2 shellPos = position + new Vector2(item.width / 2, -8 * player.direction).RotatedBy(velocity.ToRotation());

				ParticleRenderers.OverPlayers.Add(new ShotgunShellParticle(shellPos, -normalized * Main.rand.NextFloat(3f, 5f) - Vector2.UnitY * Main.rand.NextFloat(2f), 1f, 60, ammo));

				for (int i = 0; i < 4; i++)
					Dust.NewDustPerfect(shellPos, DustID.Torch, -normalized * Main.rand.NextFloat(3f, 5f) - Vector2.UnitY * Main.rand.NextFloat(2f), 0, default, Main.rand.NextFloat(2f));
			}

			return false;
		}

		return true;
	}
}

public class ShotgunPlayer : ModPlayer
{
	public StatModifier shotCount = StatModifier.Default;
	public StatModifier shotSpread = StatModifier.Default;

	public override void ResetEffects()
	{
		shotCount = StatModifier.Default;
		shotSpread = StatModifier.Default;
	}
}