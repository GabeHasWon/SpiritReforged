namespace SpiritReforged.Content.Underground.NPCs.MossSlimes;

internal class NeonMossSlime : MossSlime
{
	protected override Vector3 LightColor => new(0.5f, 0.07f, 0.5f);
	protected override int MossType => ItemID.VioletMoss;
	protected override HashSet<int> TileTypes => [TileID.VioletMoss, TileID.VioletMossBrick];
	protected override int DustType => DustID.VioletMoss;
}
