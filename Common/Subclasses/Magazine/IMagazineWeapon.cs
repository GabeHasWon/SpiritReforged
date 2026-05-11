namespace SpiritReforged.Common.Subclasses.Magazine;

#nullable enable

public delegate bool PreCheckAmmo(Item ammo, Item weapon, Player player, MagazineData data);

/// <summary>
/// Stores info related to a magazine weapon's ammo. This is used for greater flexibility with ammo (such as multiple simultaneous or optional ammo types).
/// </summary>
public readonly record struct AmmoData(int AmmoRequiredPerShot, int[] AmmoIds, bool SimpleAmmoOr = true, PreCheckAmmo? ShootCallback = null)
{
	/// <summary>
	/// Ammo definition for one ammo, one ammo per shot.
	/// </summary>
	public static AmmoData SingleAmmo(int ammoId) => new(1, [ammoId]);
}

/// <summary>
/// Stores info required for a magazine.
/// </summary>
/// <param name="ReloadTime">Reload time max in seconds. Technically truncated into ticks, though it should hardly matter.</param>
/// <param name="Data">Elaborates on ammo data if desired. Defaults to nothing, which means the item either uses no ammo or should use the vanilla ammo system.</param>
public readonly record struct MagazineData(int Size, float ReloadTime, AmmoData? Data = null);

public record struct CurrentMagazine(int AmmoUsed, int ReloadTimer);

internal interface IMagazineWeapon
{
	public class MagazineFunctionalityGlobalItem : GlobalItem
	{
		public override bool InstancePerEntity => true;

		public CurrentMagazine Current = new();

		public override bool AppliesToEntity(Item entity, bool lateInstantiation) => entity.ModItem is IMagazineWeapon;

		public override bool CanUseItem(Item item, Player player) 
		{
			if (item.ModItem is IMagazineWeapon weapon)
				return weapon.Current.ReloadTimer <= 0;

			return true;
		}

		public override bool? UseItem(Item item, Player player)
		{
			if (item.ModItem is IMagazineWeapon weapon)
			{
				weapon.Current.AmmoUsed++;

				if (weapon.Current.AmmoUsed == weapon.Data.Size)
					weapon.Current.ReloadTimer = (int)(weapon.Data.ReloadTime * 60);

				return null;
			}

			return null;
		}

		public override void HoldItem(Item item, Player player)
		{
			if (item.ModItem is not IMagazineWeapon weapon)
				return;

			if (weapon.Current.ReloadTimer == 1)
			{
				weapon.Current.AmmoUsed = 0;
			}

			weapon.Current.ReloadTimer = Math.Max(0, weapon.Current.ReloadTimer - 1);
		}
	}

	public Item Item { get; }
	public MagazineData Data { get; }

	/// <summary>
	/// Defines the current ammo selection.
	/// </summary>
	public ref CurrentMagazine Current => ref Item.GetGlobalItem<MagazineFunctionalityGlobalItem>().Current; 

	/// <summary>
	/// Whether this item is defined as a "magazine weapon" - used for global magazine weapon buffs that may not be applied to all items that apply this interface.
	/// </summary>
	public bool MarkedAsMagazine => true;
}
