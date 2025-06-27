namespace SpiritReforged.Common.PlayerCommon;

/// <summary> Handles double tap abilities in any direction. </summary>
public class DoubleTapPlayer : ModPlayer
{
	public delegate void DoubleTapDelegate(Player player, int keyDir);
	public static event DoubleTapDelegate OnDoubleTap;

	public const int UpTapThreshold = 14;

	public int lastTapUpTimer = 0;
	public bool controlUpLast = false;

	public bool UpPress => !Player.controlUp && controlUpLast;

	public override void Load() => On_Player.KeyDoubleTap += DoubleTap;
	private static void DoubleTap(On_Player.orig_KeyDoubleTap orig, Player self, int keyDir)
	{
		orig(self, keyDir);

		if (keyDir == 0)
			self.GetModPlayer<DoubleTapPlayer>().DoubleTapDown();
	}

	public override void ResetEffects() => lastTapUpTimer--;

	public override void SetControls()
	{
		if (UpPress)
		{
			lastTapUpTimer = lastTapUpTimer < 0 ? UpTapThreshold : lastTapUpTimer + UpTapThreshold;

			if (lastTapUpTimer > UpTapThreshold)
			{
				OnDoubleTap?.Invoke(Player, !Main.ReversedUpDownArmorSetBonuses ? 1 : 0);
				lastTapUpTimer = 0;
			}
		}

		controlUpLast = Player.controlUp;
	}

	internal void DoubleTapDown() => OnDoubleTap?.Invoke(Player, Main.ReversedUpDownArmorSetBonuses ? 1 : 0);
}