namespace SpiritReforged.Content.Forest.Glyphs.Radiant;

public class DivineStrike : ModBuff
{
	public override void SetStaticDefaults()
	{
		Main.buffNoSave[Type] = true;
		Main.buffNoTimeDisplay[Type] = true;
	}
}