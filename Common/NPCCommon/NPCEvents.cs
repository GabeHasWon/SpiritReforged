namespace SpiritReforged.Common.NPCCommon;

public class NPCEvents : GlobalNPC
{
	public delegate void NPCDelegate(NPC npc);
	public static event NPCDelegate OnNPCLoot;

	public override void Load() => On_NPC.NPCLoot += NPCLoot;

	private static void NPCLoot(On_NPC.orig_NPCLoot orig, NPC self)
	{
		orig(self);
		OnNPCLoot?.Invoke(self);
	}
}