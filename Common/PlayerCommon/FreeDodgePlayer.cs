namespace SpiritReforged.Common.PlayerCommon;

public sealed class FreeDodgePlayer : ModPlayer
{
	public interface IFreeDodge
	{
		/// <summary> Called whenever the player is about to take damage. </summary>
		/// <returns> Whether damage should be dodged. </returns>
		public bool FreeDodge(Player.HurtInfo info);
	}

	public int oldHeldProjectile;
	public StatModifier freeDodgeTime = StatModifier.Default;

	public override void Load() => On_Player.Update += CacheHeldProjectile;

	private static void CacheHeldProjectile(On_Player.orig_Update orig, Player self, int i)
	{
		if (self.TryGetModPlayer(out FreeDodgePlayer freeDodgePlayer))
			freeDodgePlayer.oldHeldProjectile = self.heldProj;

		orig(self, i);
	}

	public override void ResetEffects() => freeDodgeTime = StatModifier.Default;

	public override bool FreeDodge(Player.HurtInfo info)
	{
		if (oldHeldProjectile != -1 && Main.projectile[oldHeldProjectile].ModProjectile is IFreeDodge iFreeDodge)
			return iFreeDodge.FreeDodge(info);

		return false;
	}
}