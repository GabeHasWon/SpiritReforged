namespace SpiritReforged.Common.PlayerCommon;

/// <summary> Handles fountain water style flags using <see cref="SetFountain"/>. Does not exist on the server. </summary>
[Autoload(Side = ModSide.Client)]
internal class FountainPlayer : ModPlayer
{
	private int _fountainSlot = -1;

	private void TileReset() => _fountainSlot = -1;

	public override void PreUpdate()
	{
		if (_fountainSlot != -1)
			Main.SceneMetrics.ActiveFountainColor = _fountainSlot;
	}

	/// <summary> Sets the player's current fountain to the given water style. Avoid calling this method on the server. </summary>
	internal void SetFountain<T>() where T : ModWaterStyle => _fountainSlot = ModContent.GetInstance<T>().Slot;

	/// <summary> Handles <see cref="FountainPlayer"/> reset. Does not exist on the server. </summary>
	[Autoload(Side = ModSide.Client)]
	public class FountainResetSystem : ModSystem
	{
		public override void ResetNearbyTileEffects() => Main.LocalPlayer.GetModPlayer<FountainPlayer>().TileReset();
	}
}
