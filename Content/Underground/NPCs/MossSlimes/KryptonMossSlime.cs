namespace SpiritReforged.Content.Underground.NPCs.MossSlimes;

internal class KryptonMossSlime : MossSlime
{
	protected override Vector3 LightColor => new(0.1f, 0.5f, 0);
	protected override int MossType => ItemID.KryptonMoss;
	protected override HashSet<int> TileTypes => [TileID.KryptonMossBlock, TileID.KryptonMossBrick];
}
