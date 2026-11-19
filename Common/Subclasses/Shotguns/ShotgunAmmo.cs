using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Forest.Ammo;
using SpiritReforged.Content.Particles;
using Terraria.DataStructures;

namespace SpiritReforged.Common.Subclasses.Shotguns;

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
public abstract class ShotgunAmmoItem(ShootBehavior behavior, int shotCount, float spreadAmount, float speed) : ModItem
{
	public readonly ShootBehavior Behaviour = behavior;

	public int shotCount = shotCount;
	public float spreadAmount = spreadAmount;
	public float speed = speed;

	public override void SetDefaults()
	{
		Item.DamageType = ModContent.GetInstance<ShotgunClass>();
		Item.ammo = ModContent.ItemType<Shot>();
		Item.maxStack = Item.CommonMaxStack;
		Item.consumable = true;
	}
}

public class ShotgunGlobalItem : GlobalItem
{
	public static readonly ShotgunPlayer.ShotgunStats?[] Shotgun = SpiritSets.ItemFactory.CreateNamedSet(nameof(Shotgun)).Description("Whether this item will be considered a shotgun and use shotgun stats").RegisterCustomSet<ShotgunPlayer.ShotgunStats?>(null);

	public override void SetStaticDefaults() //Set vanilla shotgun defaults
	{
		Shotgun[ItemID.Boomstick] = new();
		Shotgun[ItemID.QuadBarrelShotgun] = new(shotMultiplier: 0.5f, spreadMultiplier: 0.5f);
		Shotgun[ItemID.OnyxBlaster] = new(speedMultiplier: 0.25f, spreadMultiplier: -0.2f);
		Shotgun[ItemID.Shotgun] = new();
		Shotgun[ItemID.TacticalShotgun] = new(spreadMultiplier: -0.35f);
	}

	public override void SetDefaults(Item entity)
	{
		if (entity.type < ItemID.Count && Shotgun[entity.type] != null) //Force all vanilla shotguns to use the appropriate ammo type
			entity.useAmmo = ModContent.ItemType<Shot>();
	}

	public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (ContentSamples.ItemsByType[source.AmmoItemIdUsed].ModItem is ShotgunAmmoItem ammo)
		{
			var direction = Vector2.Normalize(velocity);
			ShotgunPlayer shotgunPlayer = player.GetModPlayer<ShotgunPlayer>();
			ShotgunPlayer.ShotgunStats stats = Shotgun[item.type].Value;

			ammo.Behaviour.Invoke(item, player, source, position, direction,
				(int)shotgunPlayer.shotgunStats.ShotCount.ApplyTo(1),
				shotgunPlayer.shotgunStats.ShotSpread.ApplyTo(1),
				shotgunPlayer.shotgunStats.ShotSpeed.ApplyTo(1), damage, knockback);

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
	public readonly record struct ShotgunStats(StatModifier ShotCount, StatModifier ShotSpread, StatModifier ShotSpeed);

	public static readonly ShotgunStats Default = new(StatModifier.Default, StatModifier.Default, StatModifier.Default);
	public ShotgunStats shotgunStats = new();

	public override void ResetEffects() => shotgunStats = Default;
}