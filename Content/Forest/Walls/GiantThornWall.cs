using SpiritReforged.Common.WallCommon;

namespace SpiritReforged.Content.Forest.Walls;

public class GiantThornWall : ModWall, IAutoloadWallItem
{
	public override void SetStaticDefaults()
	{
		Main.wallLight[Type] = true;

		DustType = DustID.JunglePlants;
		HitSound = SoundID.Grass;
		AddMapEntry(new Color(64, 78, 38));
	}
}