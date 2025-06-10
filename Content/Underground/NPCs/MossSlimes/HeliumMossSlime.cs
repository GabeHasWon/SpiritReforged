namespace SpiritReforged.Content.Underground.NPCs.MossSlimes;

internal class HeliumMossSlime : MossSlime
{
	protected override Vector3 LightColor => Main.DiscoColor.ToVector3() * 0.3f;
	protected override int MossType => ItemID.RainbowMoss;
}
