namespace SpiritReforged.Common.PlayerCommon;

internal class CoinLootPlayer : ModPlayer
{
	public float enemyCoinMultiplier = 1;
	public override void ResetEffects() => enemyCoinMultiplier = 1;
}

internal class CoinLootNPC : GlobalNPC
{
	public override bool PreKill(NPC npc)
	{
		var p = Main.player[npc.lastInteraction];
		float mult = p.GetModPlayer<CoinLootPlayer>().enemyCoinMultiplier;

		npc.value *= mult;

		return true;
	}
}