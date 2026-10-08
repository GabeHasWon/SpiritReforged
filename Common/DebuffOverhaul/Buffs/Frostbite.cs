namespace SpiritReforged.Common.DebuffOverhaul.Buffs;

public class Frostbite : Frostburn
{
    public override BuffSettings Settings => new(Category.Fire);
    public override void Load() => BuffHandler.Register(this, BuffID.Frostburn2);
}