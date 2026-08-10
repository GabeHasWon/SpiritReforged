namespace SpiritReforged.Common.ProjectileCommon;

/// <summary> Allows projectiles to arbitrarily speed up. Slow down not currently implemented.<br/>
/// Set the projectile's <see cref="SpeedModifier"/> to >0 to increase it's speed (if set every frame), or use <see cref="ProjectileSpeedModifierPlayer.GetProjectileModifierSpeed"/>
/// for more dynamic, arbitrary player-wide changes (such as an accessory). </summary>
internal class SpeedModifierProjectile : GlobalProjectile
{
	/// <summary> Safety measure to stop recursion. I don't know if it's needed. - Gabe </summary>
	private static bool Recursion = false;

	public override bool InstancePerEntity => true;

	public StatModifier SpeedModifier { get; set; } = StatModifier.Default;
	
	//Internal tracker for speed. Don't modify this directly.
	private float _timer = 0;

	public override bool PreAI(Projectile projectile)
	{
		SpeedUpBehaviour(projectile, this);
		return true;
	}

	private static void SpeedUpBehaviour(Projectile projectile, SpeedModifierProjectile self)
	{
		if (!projectile.TryGetOwner(out Player plr))
			return;

		self._timer += plr.GetModPlayer<ProjectileSpeedModifierPlayer>().Invoke(projectile);
		self._timer += self.SpeedModifier.ApplyTo(1) - 1f;

		self.SpeedModifier = StatModifier.Default; //Reset field

		while (self._timer > 1)
		{
			RepeatAI(projectile, 1);
			self._timer--;
		}
	}

	/// <summary> Method used to run AI again, with recursion checks & respecting <see cref="ModProjectile.AIType"/>. </summary>
	public static void RepeatAI(Projectile projectile, int repeats)
	{
		if (Recursion)
			return;

		Recursion = true;

		int type = projectile.type;
		bool actType = projectile.ModProjectile != null && projectile.ModProjectile.AIType > ProjectileID.None;

		for (int i = 0; i < repeats; ++i)
		{
			if (actType)
				projectile.type = projectile.ModProjectile.AIType;

			projectile.VanillaAI();

			if (actType)
				projectile.type = type;
		}

		ProjectileLoader.AI(projectile);

		Recursion = false;
	}
}
