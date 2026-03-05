using SpiritReforged.Common.WallCommon;

namespace SpiritReforged.Content.Ziggurat.Walls;

public class PolishedSandstoneWall : ModWall, IAutoloadUnsafeWall, IAutoloadWallItem
{
	public static int UnsafeType { get; private set; } = SpiritReforgedMod.Instance.Find<ModWall>("PolishedSandstoneWallUnsafe").Type;

	public override void SetStaticDefaults()
	{
		Main.wallHouse[Type] = true;
		DustType = DustID.DynastyShingle_Red;

		var entryColor = new Color(154, 90, 28);
		AddMapEntry(entryColor);
		WallLoader.GetWall(UnsafeType).AddMapEntry(entryColor);
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
}