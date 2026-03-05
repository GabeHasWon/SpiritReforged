namespace SpiritReforged.Content.Underground.NPCs.MossSlimes;

internal class KryptonMossSlime : MossSlime
{
	protected override Vector3 LightColor => new(0.08f, 0.4f, 0);
	protected override int MossType => ItemID.KryptonMoss;
	protected override HashSet<int> TileTypes => [TileID.KryptonMossBlock, TileID.KryptonMossBrick];
	protected override int DustType => DustID.KryptonMoss;
}
