namespace SpiritReforged.Content.Underground.NPCs.MossSlimes;

internal class ArgonMossSlime : MossSlime
{
	protected override Vector3 LightColor => new(0.4f, 0.15f, 0.4f);
	protected override int MossType => ItemID.ArgonMoss;
	protected override HashSet<int> TileTypes => [TileID.ArgonMossBlock, TileID.ArgonMossBrick];
	protected override int DustType => DustID.ArgonMoss;
}
