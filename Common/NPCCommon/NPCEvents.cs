using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Common.NPCCommon;

public class NPCEvents : GlobalNPC
{
	public delegate void NPCDelegate(NPC npc);
	public delegate void SpawnRateDelegate(Player player, ref int spawnRate, ref int maxSpawns);
	public delegate void SetBestiaryDelegate(NPC npc, BestiaryDatabase database, BestiaryEntry bestiaryEntry);

	public static event NPCDelegate OnNPCLoot;
	public static event SpawnRateDelegate OnEditSpawnRate;
	public static event SetBestiaryDelegate OnSetBestiary;

	public override void Load() => On_NPC.NPCLoot += NPCLoot;
	private static void NPCLoot(On_NPC.orig_NPCLoot orig, NPC self)
	{
		orig(self);
		OnNPCLoot?.Invoke(self);
	}

	public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns) => OnEditSpawnRate?.Invoke(player, ref spawnRate, ref maxSpawns);
	public override void SetBestiary(NPC npc, BestiaryDatabase database, BestiaryEntry bestiaryEntry) => OnSetBestiary?.Invoke(npc, database, bestiaryEntry);
}