namespace SpiritReforged.Common.NPCCommon;

internal class ReflectedNPC : GlobalNPC
{
	internal static bool ReflectingNPCs = false;

	public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		if (!ReflectingNPCs)
			return true;

		if (npc.type is NPCID.Vampire or NPCID.VampireBat)
			return false;

		return true;
	}
}
