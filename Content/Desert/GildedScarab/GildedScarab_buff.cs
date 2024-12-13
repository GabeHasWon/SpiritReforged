﻿namespace SpiritReforged.Content.Desert.GildedScarab;

internal class GildedScarab_buff : ModBuff
{
	public override void SetStaticDefaults()
	{
		Main.pvpBuff[Type] = true;
		Main.buffNoTimeDisplay[Type] = false;
	}

	public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
		=> tip = Language.GetText("Mods.SpiritReforged.Content.Desert.GildedScarab.GildedScarab_buff.DisplayName").WithFormatArgs(Main.LocalPlayer.GetModPlayer<GildedScarabPlayer>().scarabDefense).Value;

	public override void Update(Player player, ref int buffIndex) => player.statDefense += player.GetModPlayer<GildedScarabPlayer>().scarabDefense;
}