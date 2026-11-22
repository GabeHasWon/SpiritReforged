using SpiritReforged.Common.NPCCommon;

namespace SpiritReforged.Common.DebuffOverhaul.Buffs;

public class Hellfire : OnFire
{
    public override BuffSettings Settings => new(Category.Fire);
    public override void Load()
    {
        BuffHandler.Register(this, BuffID.OnFire3);

        StopGoresHook.Conditions += static (npc) => npc.HasBuff(BuffID.OnFire3);
        NPCEvents.HitEffectEvent += FireDeathEffects;
    }
}