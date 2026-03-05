namespace SpiritReforged.Content.Underground.WayfarerSet;

public class ExplorerPot : ModBuff
{
	public override void SetStaticDefaults() => Main.buffNoTimeDisplay[Type] = false;
	public override void Update(Player player, ref int buffIndex)
	{
		player.moveSpeed += .2f;
		player.runAcceleration += .06f;
	}
}
