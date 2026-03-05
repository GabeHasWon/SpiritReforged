using Terraria.DataStructures;

namespace SpiritReforged.Content.Jungle.Bamboo.Items;

public class DashSwordPlayer : ModPlayer
{
	public bool HasDashCharge { get; private set; }

	public bool holdingSword;
	public bool dashing;

	private int _internalCooldown;

	/// <summary> Optionally set a dash cooldown. </summary>
	public void SetDashCooldown(int time = 30)
	{
		_internalCooldown = time;
		HasDashCharge = false;
	}

	public override void ResetEffects() => holdingSword = false;

	public override void PreUpdate()
	{
		if (dashing)
			Player.maxFallSpeed = 2000f;
	}

	public override void PostUpdateEquips()
	{
		if (!Player.ItemAnimationActive && (_internalCooldown = Math.Max(_internalCooldown - 1, 0)) == 0 && Player.velocity.Y == 0)
			HasDashCharge = true;
	}

	public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable) => dashing;
}

public class DashSwordLayer : PlayerDrawLayer
{
	public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.HeldItem);

	protected override void Draw(ref PlayerDrawSet drawInfo)
	{
		Item item = drawInfo.drawPlayer.HeldItem;
		if (!drawInfo.drawPlayer.GetModPlayer<DashSwordPlayer>().holdingSword || drawInfo.shadow != 0f || drawInfo.drawPlayer.frozen || drawInfo.drawPlayer.dead || drawInfo.drawPlayer.wet && item.noWet)
			return;

		if (item.ModItem is IDashSword dashSword)
			dashSword.DrawHeld(ref drawInfo);
	}
}

public interface IDashSword
{
	public void DrawHeld(ref PlayerDrawSet drawInfo);
}