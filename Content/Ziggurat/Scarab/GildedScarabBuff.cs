using Humanizer;

namespace SpiritReforged.Content.Ziggurat.Scarab;

internal class GildedScarabBuff : ModBuff
{
	public override void SetStaticDefaults() => Main.buffNoTimeDisplay[Type] = false;
	public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare) => tip = tip.FormatWith(Main.LocalPlayer.GetModPlayer<GildedScarabPlayer>().ScarabDefense);
	public override void Update(Player player, ref int buffIndex) => player.statDefense += player.GetModPlayer<GildedScarabPlayer>().ScarabDefense;
}