namespace SpiritReforged.Common.DebuffOverhaul.Buffs;

public class Frostbite : Frostburn
{
    public override Settings LocalSettings => new(0.4f, 2000);
    public override void Load() => Handler.Register(this, BuffID.Frostburn2);
}