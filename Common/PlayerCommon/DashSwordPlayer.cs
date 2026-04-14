using Terraria.DataStructures;

namespace SpiritReforged.Common.PlayerCommon;

public class DashSwordPlayer : ModPlayer
{
	public bool HasDashCharge { get; private set; }

	/// <summary> Modifies the rate in which dashes recharge. </summary>
	public StatModifier statDashCooldown = StatModifier.Default;
	public bool dashing;

	private float _internalCooldown;

	/// <summary> Optionally set a dash cooldown. </summary>
	public void SetDashCooldown(int time = 30)
	{
		_internalCooldown = time;
		HasDashCharge = false;
	}

	public override void ResetEffects() => statDashCooldown = StatModifier.Default;

	public override void PreUpdate()
	{
		if (dashing)
			Player.maxFallSpeed = 2000f;
	}

	public override void PostUpdateEquips()
	{
		if (!Player.ItemAnimationActive && (_internalCooldown = Math.Max(_internalCooldown - statDashCooldown.ApplyTo(1), 0)) == 0 && Player.velocity.Y == 0)
			HasDashCharge = true;
	}

	public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable) => dashing;
}