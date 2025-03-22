﻿namespace SpiritReforged.Common.PlayerCommon;

internal class CollisionPlayer : ModPlayer
{
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
	}
}
