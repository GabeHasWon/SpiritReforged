namespace SpiritReforged.Content.Underground.Items.ExplorerTreads;

public class ExplorerSpeed : ModBuff
{
	public override void Update(Player player, ref int buffIndex)
	{
		player.maxRunSpeed *= 1.75f;
		player.accRunSpeed *= 1.75f;
		player.runAcceleration *= 1.75f;
	}
}
