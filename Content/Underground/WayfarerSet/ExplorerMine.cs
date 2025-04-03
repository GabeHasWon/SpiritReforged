namespace SpiritReforged.Content.Underground.WayfarerSet;

public class ExplorerMine : ModBuff
{
	public override void SetStaticDefaults() => Main.buffNoTimeDisplay[Type] = false;

	public override void Update(Player player, ref int buffIndex) => player.pickSpeed -= .2f;
}
