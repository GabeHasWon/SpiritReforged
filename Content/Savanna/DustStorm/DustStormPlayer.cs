﻿using SpiritReforged.Content.Savanna.Biome;

namespace SpiritReforged.Content.Savanna.DustStorm;

public class DustStormPlayer : ModPlayer
{
	/// <summary> Whether the player is present in a dust storm. </summary>
	public bool ZoneDustStorm => (Math.Abs(Main.windSpeedCurrent) > .4f || Player.ZoneSandstorm) && (Player.InModBiome<SavannaBiome>() || EvilSavanna());

	private bool EvilSavanna()
	{
		if (SavannaTileCounts.InSavanna)
			return Player.ZoneCorrupt || Player.ZoneCrimson || Player.ZoneHallow;

		return false;
	}
}
