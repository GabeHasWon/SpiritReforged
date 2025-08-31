using SpiritReforged.Common.WorldGeneration;
using Terraria.GameContent.Events;
using Terraria.Graphics.Effects;

namespace SpiritReforged.Content.Savanna.DustStorm;

public class DustStormScene : ModSceneEffect
{
	public override SceneEffectPriority Priority => SceneEffectPriority.Environment;
	public override int Music => MusicLoader.GetMusicSlot(Mod, "Assets/Music/Duststorm" + (Main.swapMusic ? "Otherworld" : ""));

	public override bool IsSceneEffectActive(Player player) => player.GetModPlayer<DustStormPlayer>().ZoneDustStorm;

	public override void SpecialVisuals(Player player, bool isActive)
	{
		if (player.whoAmI == Main.myPlayer && isActive || Duststorm.Happening)
		{
			Duststorm.Happening = isActive;
			Duststorm.Update();
		}
	}
}

public static class Duststorm
{
	[WorldBound]
	internal static bool Happening;

	private static bool EffectsUp;

	public static void Update()
	{
		if (Happening)
		{
			Sandstorm.Severity = MathHelper.Lerp(Sandstorm.Severity, 0.35f, 0.05f);
			Main.LocalPlayer.ZoneSandstorm = false;

			if (!EffectsUp)
			{
				var center = Main.LocalPlayer.Center;

				SkyManager.Instance.Activate("Sandstorm", center);
				Filters.Scene.Activate("Sandstorm", center);
				Overlays.Scene.Activate("Sandstorm", center);
			}
		}
		else if (EffectsUp && !Main.LocalPlayer.ZoneSandstorm) //ZoneSandstorm check specifically prevents sandstorm visuals from fading out, then in when entering the desert
		{
			SkyManager.Instance.Deactivate("Sandstorm");
			Filters.Scene.Deactivate("Sandstorm");
			Overlays.Scene.Deactivate("Sandstorm");
		}

		EffectsUp = Filters.Scene["Sandstorm"].Active;
	}
}