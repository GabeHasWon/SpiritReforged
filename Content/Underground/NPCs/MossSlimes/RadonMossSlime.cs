using SpiritReforged.Content.Underground.Moss.Radon;

namespace SpiritReforged.Content.Underground.NPCs.MossSlimes;

internal class RadonMossSlime : MossSlime
{
	protected override Vector3 LightColor => new(0.45f, 0.45f, 0.05f);
	protected override int MossType => ModContent.ItemType<RadonMossItem>();
}
