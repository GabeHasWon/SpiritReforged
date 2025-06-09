namespace SpiritReforged.Content.Underground.NPCs.MossSlimes;

internal class NeonMossSlime : MossSlime
{
	protected override Vector3 LightColor => new(0.45f, 0.05f, 0.45f);
	protected override int MossType => ItemID.PurpleMoss;
}
