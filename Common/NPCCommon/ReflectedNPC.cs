using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Common.NPCCommon;

/// <summary> Handles custom NPC reflection drawing. </summary>
internal class ReflectedNPC : GlobalNPC
{
	public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		if (!Reflections.DrawingReflection)
			return true;

		return npc.type is not (NPCID.Vampire or NPCID.VampireBat);
	}
}
