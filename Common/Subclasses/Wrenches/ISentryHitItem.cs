using Terraria.Audio;

namespace SpiritReforged.Common.Subclasses.Wrenches;

/// <summary>
/// Defines an item as a "wrench"; that is, a melee weapon that can hit sentries.<br/>
/// Hooks: <see cref="CanHitSentry(Projectile)"/>, <see cref="OnHitSentry(Projectile)"/>, <see cref="PreHitEffects(ref SoundStyle, ref int)"/>
/// </summary>
internal interface ISentryHitItem
{
	/// <summary>
	/// Whether the player can hit a sentry. Returns true by default.
	/// </summary>
	public bool CanHitSentry(Player player, Projectile sentry) => true;

	/// <summary>
	/// Runs when the player hits a sentry.
	/// </summary>
	public void OnHitSentry(Player player, Projectile sentry);

	/// <summary>
	/// Runs before the default hit effects occur.
	/// </summary>
	/// <param name="style"></param>
	/// <param name="dustType"></param>
	/// <param name="dustCount"></param>
	/// <returns></returns>
	public bool PreHitEffects(ref SoundStyle style, ref int dustType, ref int dustCount) => true;
}
