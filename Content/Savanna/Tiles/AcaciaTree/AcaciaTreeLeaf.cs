namespace SpiritReforged.Content.Savanna.Tiles.AcaciaTree;

internal class AcaciaTreeLeaf : ModGore
{
	public override void SetStaticDefaults()
	{
		ChildSafety.SafeGore[Type] = true;
		GoreID.Sets.SpecialAI[Type] = 3;
		GoreID.Sets.PaintedFallingLeaf[Type] = true;
	}
}
