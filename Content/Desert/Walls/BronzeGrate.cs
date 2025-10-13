using SpiritReforged.Common.WallCommon;

namespace SpiritReforged.Content.Desert.Walls;

public class BronzeGrate : ModWall, IAutoloadUnsafeWall, IAutoloadWallItem
{
	public static int UnsafeType { get; private set; } = SpiritReforgedMod.Instance.Find<ModWall>("BronzeGrateUnsafe").Type;

	public override void SetStaticDefaults()
	{
		Main.wallHouse[Type] = true;
		Main.wallLight[Type] = true;
		DustType = DustID.Copper;

		var entryColor = new Color(170, 80, 50);
		AddMapEntry(entryColor);

		Main.wallLight[UnsafeType] = true;
		WallLoader.GetWall(UnsafeType).AddMapEntry(entryColor);
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
}