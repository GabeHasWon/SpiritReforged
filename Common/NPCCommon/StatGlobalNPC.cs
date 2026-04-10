
namespace SpiritReforged.Common.NPCCommon;

public class StatGlobalNPC : GlobalNPC
{
	public override bool InstancePerEntity => true;

	public StatModifier statDefense = StatModifier.Default;
	public StatModifier statSpeed = StatModifier.Default;

	private float _slowCounter;

	public override void ResetEffects(NPC npc)
	{
		if (statSpeed == StatModifier.Default)
			_slowCounter = 0;

		statDefense = StatModifier.Default;
		statSpeed = StatModifier.Default;
	}

	public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers) => modifiers.Defense = modifiers.Defense.CombineWith(statDefense);

	public override bool PreAI(NPC npc)
	{
		if (statSpeed != StatModifier.Default)
		{
			if ((_slowCounter += Math.Clamp(1f - statSpeed.ApplyTo(1), 0, 1)) >= 1)
			{
				_slowCounter--;
				return true;
			}

			return false;
		}

		return true;
	}

	public override void PostAI(NPC npc)
	{
		if (statSpeed != StatModifier.Default)
			npc.position -= npc.velocity * (float)(1f - _slowCounter);
	}
}