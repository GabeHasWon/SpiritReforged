namespace SpiritReforged.Content.Underground.Tiles.Mirror;

public class Unlucky : ModBuff
{
	public override void SetStaticDefaults() => Main.debuff[Type] = true;

	public override void Update(Player player, ref int buffIndex) => player.luck -= 0.5f;
}