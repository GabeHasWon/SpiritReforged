using SpiritReforged.Content.Underground.Moss.Oganesson;

namespace SpiritReforged.Content.Underground.NPCs.MossSlimes;

internal class OganessonMossSlime : MossSlime
{
	protected override Vector3 LightColor => new(0.5f, 0.5f, 0.5f);
	protected override int MossType => ModContent.ItemType<OganessonMossItem>();
	protected override HashSet<int> TileTypes => [ModContent.TileType<OganessonMoss>(), ModContent.TileType<OganessonMossBrick>()];
	protected override int DustType => DustID.WhiteTorch;
}
