﻿using Terraria.GameContent.Events;
using Terraria.Graphics.Effects;

namespace SpiritReforged.Content.Savanna.DustStorm;

public class DuststormScene : ModSceneEffect
{
	private bool wasDustStorm;

	public override SceneEffectPriority Priority => SceneEffectPriority.BiomeHigh;
	public override int Music => MusicLoader.GetMusicSlot(Mod, "Assets/Music/Duststorm");

	public override bool IsSceneEffectActive(Player player) => player.GetModPlayer<DustStormPlayer>().ZoneDustStorm || wasDustStorm;

	public override void SpecialVisuals(Player player, bool isActive)
	{
		const string sandstorm = "Sandstorm";
		var mPlayer = player.GetModPlayer<DustStormPlayer>();

		if (mPlayer.ZoneDustStorm)
		{
			float intensity = .35f;

			if (!Filters.Scene[sandstorm].IsActive()) //Initialize
			{
				//Severity has a value even if it isn't being used. Doing this prevents snapping to high-severity sandstorm vfx upon entering a dust storm zone
				if (!Filters.Scene[sandstorm].IsInUse())
					Sandstorm.Severity = intensity;

				var center = player.Center;

				SkyManager.Instance.Activate(sandstorm, center);
				Filters.Scene.Activate(sandstorm, center);
				Overlays.Scene.Activate(sandstorm, center); //Might have no effect?
			}

			if (!player.ZoneSandstorm)
				Sandstorm.Severity = MathHelper.Max(Sandstorm.Severity - .01f, intensity); //Transition into a calmer severity if necessary
		}

		if (!mPlayer.ZoneDustStorm && wasDustStorm && Filters.Scene[sandstorm].IsActive() && !player.ZoneSandstorm)
		{
			SkyManager.Instance.Deactivate(sandstorm);
			Filters.Scene.Deactivate(sandstorm);
			Overlays.Scene.Deactivate(sandstorm);
		}

		wasDustStorm = player.GetModPlayer<DustStormPlayer>().ZoneDustStorm;
	}
}
