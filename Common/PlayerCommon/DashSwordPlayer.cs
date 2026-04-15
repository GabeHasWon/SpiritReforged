using Terraria.DataStructures;

namespace SpiritReforged.Common.PlayerCommon;

public class DashSwordPlayer : ModPlayer
{
	public bool HasDashCharge { get; private set; }
	public bool Dashing => _dashTime != 0;

	/// <summary> Modifies the rate in which dashes recharge. </summary>
	public StatModifier statDashCooldown = StatModifier.Default;

	private float _dashTime;
	private float _internalCooldown;

	/// <summary> Optionally set a dash cooldown. </summary>
	public void SetDash(int cooldown = 30)
	{
		_dashTime = 2;
		_internalCooldown = cooldown;
		HasDashCharge = false;
	}

	public override void ResetEffects()
	{
		statDashCooldown = StatModifier.Default;
		_dashTime = Math.Max(_dashTime - 1, 0);
	}

	public override void PreUpdate()
	{
		if (Dashing)
			Player.maxFallSpeed = 2000f;
	}

	public override void PostUpdateEquips()
	{
		if (!Player.ItemAnimationActive && (_internalCooldown = Math.Max(_internalCooldown - statDashCooldown.ApplyTo(1), 0)) == 0 && Player.velocity.Y == 0)
			HasDashCharge = true;
	}

	public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable) => Dashing;
}