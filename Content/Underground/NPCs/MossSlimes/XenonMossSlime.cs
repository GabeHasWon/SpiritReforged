namespace SpiritReforged.Content.Underground.NPCs.MossSlimes;

internal class XenonMossSlime : MossSlime
{
	protected override Vector3 LightColor => new(0f, 0.4f, 0.5f);
	protected override int MossType => ItemID.XenonMoss;
	protected override HashSet<int> TileTypes => [TileID.XenonMossBlock, TileID.XenonMossBrick];
}
