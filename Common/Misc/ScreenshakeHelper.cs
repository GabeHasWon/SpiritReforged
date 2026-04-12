using SpiritReforged.Common.ConfigurationCommon;
using Terraria.Graphics.CameraModifiers;

namespace SpiritReforged.Common.Misc;

#nullable enable

/// <summary>
/// Simplifies screen shake through <see cref="PunchCameraModifier"/>, alongside respecting <see cref="ReforgedClientConfig.ScreenshakeStrength"/>.
/// </summary>
internal static class ScreenshakeHelper
{
	/// <summary>
	/// Shakes the screen through <see cref="PunchCameraModifier"/>.
	/// </summary>
	public static void Shake(Vector2 position, Vector2 direction, float strength, float vibration, int frames, float distanceFalloff = -1, string? uniqueId = null)
	{
		float shake = ModContent.GetInstance<ReforgedClientConfig>().ScreenshakeStrength;

		if (shake == 0)
			return;

		Main.instance.CameraModifiers.Add(new PunchCameraModifier(position, direction * shake, strength * shake, vibration * shake, (int)(frames * shake), distanceFalloff, uniqueId));
	}
}
