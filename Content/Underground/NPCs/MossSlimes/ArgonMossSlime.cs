namespace SpiritReforged.Content.Underground.NPCs.MossSlimes;

internal class ArgonMossSlime : MossSlime
{
	protected override Vector3 LightColor => new(0.3f, 0.1f, 0.3f);
	protected override int MossType => ItemID.ArgonMoss;
	protected override HashSet<int> TileTypes => [TileID.ArgonMossBlock, TileID.ArgonMossBrick];
}
