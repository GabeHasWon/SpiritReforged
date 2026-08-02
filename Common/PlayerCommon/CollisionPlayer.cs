namespace SpiritReforged.Common.PlayerCommon;

internal class CollisionPlayer : ModPlayer
{
	public override ModPlayer Clone(Player newEntity)
	{
		CollisionPlayer modPlayer = newEntity.GetModPlayer<CollisionPlayer>();
		modPlayer._ignorePlatformDuration = _ignorePlatformDuration;

		return modPlayer;
	}

	public bool IgnorePlatforms
	{
		get => _ignorePlatformDuration > 0;
		set => _ignorePlatformDuration = value ? 4 : 0;
	}

	private int _ignorePlatformDuration;

	/// <summary> Handles rotating the player based on per-tick conditions. See <see cref="PlayerExtensions.Rotate"/>. </summary>
	public float rotation;
	private bool _wasRotated;

	/// <summary> Set to true if the player should fall through a platform validated by <see cref="FallThrough"/>. </summary>
	public bool fallThrough;
	private bool _noReset;

	public override void Load() => On_Player.DryCollision += OverrideCollision;

	private static void OverrideCollision(On_Player.orig_DryCollision orig, Player self, bool fallThrough, bool ignorePlats)
	{
		if (self.TryGetModPlayer(out CollisionPlayer cPlayer) && cPlayer.IgnorePlatforms)
			fallThrough = ignorePlats = true;

		orig(self, fallThrough, ignorePlats);
	}

	/// <summary> Should be checked continuously while the player is intersecting with custom platform. See <see cref="fallThrough"/>. </summary>
	/// <returns> Whether the player is falling through. </returns>
	public bool FallThrough()
	{
		_noReset = true;
		return fallThrough || Player.grapCount > 0;
	}

	public override void UpdateEquips()
	{
		if (_ignorePlatformDuration > 0)
		{
			Player.controlDown = true;
			_ignorePlatformDuration--;
		}
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
