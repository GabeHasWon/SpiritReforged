namespace SpiritReforged.Common.DebuffOverhaul.Buffs;

public class Frostbite : Frostburn
{
    public override BuffSettings Settings => new(0.4f * VanillaScaling, (int)(2000 * VanillaMaximum));
    public override void Load() => BuffHandler.Register(this, BuffID.Frostburn2);
}