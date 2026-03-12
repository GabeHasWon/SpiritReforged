namespace SpiritReforged.Common.BuffCommon;

/// <summary>
/// Tracks a player's bet flags. Use <see cref="HasPet(int)"/> to check if a pet is currently active (or should be).
/// </summary>
public class PetPlayer : ModPlayer
{
	internal HashSet<int> pets = [];

	public override void ResetEffects() => pets.Clear();

	public bool HasPet(int projectileId) => pets.Contains(projectileId);

	/// <summary>Automatically sets pet flag for any given projectile using <see cref="pets"/>.</summary>
	public void PetFlag(Projectile projectile)
	{
		var modPlayer = Main.player[projectile.owner].GetModPlayer<PetPlayer>();

		if (!modPlayer.pets.Contains(projectile.type))
			modPlayer.pets.Add(projectile.type);

		if (Player.dead)
			modPlayer.pets.Remove(projectile.type);

		if (modPlayer.pets.Contains(projectile.type))
			projectile.timeLeft = 2;
	}
}

/// <summary>
/// Base class for a "pet buff", which simply checks a flag, spawns the pet if it's not already spawned, and counts as a pet buff.
/// </summary>
public abstract class PetBuff<T> : ModBuff where T : ModProjectile
{
	/// <summary>
	/// Defines the default localization values for the buff. This simplifies creating the buff.
	/// </summary>
	protected abstract (string name, string description) BuffInfo { get; }

	/// <summary>
	/// Whether this buff is a light pet buff or not. Defaults to false.
	/// </summary>
	protected virtual bool IsLightPet => false;

	public override LocalizedText DisplayName => this.GetLocalization(nameof(DisplayName), () => BuffInfo.name);
	public override LocalizedText Description => this.GetLocalization(nameof(Description), () => BuffInfo.description);

	public sealed override void SetStaticDefaults()
	{
		Main.buffNoTimeDisplay[Type] = true;

		if (IsLightPet)
			Main.lightPet[Type] = true;
		else
			Main.vanityPet[Type] = true;
	}

	public sealed override void Update(Player player, ref int buffIndex)
	{
		player.buffTime[buffIndex] = 18000;
		SetPetFlag(player.GetModPlayer<PetPlayer>());

		bool petProjectileNotSpawned = player.ownedProjectileCounts[ModContent.ProjectileType<T>()] <= 0;
		if (petProjectileNotSpawned && player.whoAmI == Main.myPlayer)
			Projectile.NewProjectile(player.GetSource_Buff(buffIndex), player.Center, Vector2.Zero, ModContent.ProjectileType<T>(), 0, 0f, player.whoAmI);
	}

	public virtual void SetPetFlag(PetPlayer petPlayer)
	{
		if (!petPlayer.HasPet(ModContent.ProjectileType<T>()))
			petPlayer.pets.Add(ModContent.ProjectileType<T>());
	}
}
