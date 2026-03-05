namespace SpiritReforged.Common.PlayerCommon;

internal class CollisionPlayer : ModPlayer
{
	/// <summary> Handles rotating the player based on per-tick conditions. See <see cref="PlayerExtensions.Rotate"/>. </summary>
	public float rotation;
	private bool _wasRotated;

	/// <summary> Set to true if the player should fall through a platform validated by <see cref="FallThrough"/>. </summary>
	public bool fallThrough;
	private bool _noReset;

	/// <summary> Should be checked continuously while the player is intersecting with custom platform. See <see cref="fallThrough"/>. </summary>
	/// <returns> Whether the player is falling through. </returns>
	public bool FallThrough()
	{
		_noReset = true;
		return fallThrough || Player.grapCount > 0;
	}

	public override void ResetEffects()
	{
		if (!_noReset)
			fallThrough = false;

		_noReset = false;

		if (rotation == 0 && _wasRotated)
		{
			Player.fullRotation = 0;
			Player.fullRotationOrigin = default;
		}

		_wasRotated = rotation != 0;
		rotation = 0;
	}
}
