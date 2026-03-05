namespace SpiritReforged.Common.NPCCommon;

public class SlowdownGlobalNPC : GlobalNPC
{
	public bool BeingSlowed => slowStrength > 0;
	public override bool InstancePerEntity => true;

	/// <summary> Slows <paramref name="npc"/> by the given amount. Accepts a range of 0-1. </summary>
	public static void ApplySlow(NPC npc, float amount) => npc.GetGlobalNPC<SlowdownGlobalNPC>().slowStrength = MathHelper.Clamp(amount, 0, 1);

	private float slowStrength;
	private float slowCounter;

	public override void ResetEffects(NPC npc) => slowStrength = 0;
	public override bool PreAI(NPC npc)
	{
		if (BeingSlowed)
		{
			if ((slowCounter += 1 - slowStrength) >= 1)
			{
				slowCounter--;
				return true;
			}

			return false;
		}

		return true;
	}

	public override void PostAI(NPC npc)
	{
		if (BeingSlowed)
			npc.position -= npc.velocity * (float)(1f - slowCounter);
	}
}
