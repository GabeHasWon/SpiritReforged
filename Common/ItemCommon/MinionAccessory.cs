namespace SpiritReforged.Common.ItemCommon;

/// <summary> 
/// A class that handles boilerplate code that every minion accessory (accessories that spawn a guy) has. 
/// Needs each Minion Accessory to provide damage and a projectile type
/// Stores data using a record that can be referenced later, and associates data with the itemtype
/// </summary>
public record MinionAccessoryData(int ProjType, int Damage);

public abstract class MinionAccessory : EquippableItem
{
	public static readonly Dictionary<int, MinionAccessoryData> MinionDataByItemId = [];
	public abstract MinionAccessoryData Data { get; }

	public sealed override void SetStaticDefaults()
	{
		MinionDataByItemId.Add(Type, Data);
		StaticDefaults();
	}

	/// <inheritdoc cref="ModType.SetStaticDefaults"/>
	public virtual void StaticDefaults() { }

	public sealed override void SetDefaults()
	{
		Item.DamageType = DamageClass.Summon;
		Item.accessory = true;
		Item.damage = Data.Damage;
		//Item.shoot = Data.ProjType;

		Defaults();
	}

	/// <inheritdoc cref="ModItem.SetDefaults"/>
	public virtual void Defaults() { }

	public override void UpdateEquippable(Player player)
	{
		if (player.whoAmI == Main.myPlayer && MinionDataByItemId.TryGetValue(Type, out var data))
		{
			int projType = data.ProjType;
			int projDamage = player.GetWeaponDamage(Item);

			if (player.ownedProjectileCounts[projType] < 1)
				Projectile.NewProjectile(Terraria.Entity.GetSource_NaturalSpawn(), player.Center, Vector2.Zero, projType, projDamage, 0f, player.whoAmI);
		}
	}

	public override bool WeaponPrefix() => false;

	public override bool MeleePrefix() => false;

	public override bool MagicPrefix() => false;

	public override bool RangedPrefix() => false;

}
