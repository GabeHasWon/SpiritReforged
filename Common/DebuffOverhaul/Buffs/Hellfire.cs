using SpiritReforged.Common.NPCCommon;

namespace SpiritReforged.Common.DebuffOverhaul.Buffs;

public class Hellfire : OnFire
{
    public override Settings LocalSettings => new(0.25f, 800);
    public override void Load()
    {
        Handler.Register(this, BuffID.OnFire3);

        StopGoresHook.Conditions += static (npc) => npc.HasBuff(BuffID.OnFire3);
        NPCEvents.HitEffectEvent += FireDeathEffects;
    }
}