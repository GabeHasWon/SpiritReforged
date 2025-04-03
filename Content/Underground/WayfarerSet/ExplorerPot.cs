namespace SpiritReforged.Content.Underground.WayfarerSet;

public class ExplorerPot : ModBuff
{
	public override void SetStaticDefaults() => Main.buffNoTimeDisplay[Type] = false;

	public override void Update(Player player, ref int buffIndex)
	{
		WayfarerPlayer wayfarerPlayer = player.GetModPlayer<WayfarerPlayer>();

		player.moveSpeed += wayfarerPlayer.movementStacks * .2f;
		player.runAcceleration += wayfarerPlayer.movementStacks * .06f;
	}
}
