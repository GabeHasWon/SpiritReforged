namespace SpiritReforged.Common.PlayerCommon;

internal class CoinLootPlayer : ModPlayer
{
	public float EnemyCoinMultiplier { get; private set; } = 1;

	/// <summary> Accepts full percentage values. </summary>
	public void AddMult(int value) => EnemyCoinMultiplier += (float)(value / 100f);
	public override void ResetEffects() => EnemyCoinMultiplier = 1;
}

internal class CoinLootNPC : GlobalNPC
{
	public override bool PreKill(NPC npc)
	{
		var p = Main.player[npc.lastInteraction];
		float mult = p.GetModPlayer<CoinLootPlayer>().EnemyCoinMultiplier;

		npc.value *= mult;

		return true;
	}
}