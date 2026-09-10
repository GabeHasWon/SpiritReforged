namespace SpiritReforged.Common.ProjectileCommon;

/// <summary>
/// Allows global, per-player projectile speed modifications using the <see cref="GetProjectileModifierSpeed"/> event.<br/>
/// In order to buff (select) projectiles, subscribe a method to <see cref="GetProjectileModifierSpeed"/>. The event is reset every frame.
/// </summary>
internal class ProjectileSpeedModifierPlayer : ModPlayer
{
	public delegate float GetProjectileModifierSpeedDelegate(Projectile projectile);

	public event GetProjectileModifierSpeedDelegate GetProjectileModifierSpeed;

	// Clear invokation list so we can use this like a standard stat
	public override void ResetEffects() => GetProjectileModifierSpeed = null;

	public float Invoke(Projectile projectile) => GetProjectileModifierSpeed?.Invoke(projectile) ?? 0;
}
