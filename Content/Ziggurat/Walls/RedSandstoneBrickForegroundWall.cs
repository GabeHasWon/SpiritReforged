using SpiritReforged.Common;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.WallCommon;

namespace SpiritReforged.Content.Ziggurat.Walls;

public class RedSandstoneBrickForegroundWall : ModWall, IAutoloadWallItem
{
	public void AddItemRecipes(ModItem item) => item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<RedSandstoneBrickWall>(), 4).AddTile(TileID.HeavyWorkBench).AddCondition(Condition.InGraveyard).Register();

	public override void SetStaticDefaults()
	{
		SpiritSets.WallBlocksLight[Type] = true;
		Main.wallHouse[Type] = false;
		DustType = DustID.DynastyShingle_Red;

		AddMapEntry(new(150, 70, 40));
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;

	public override bool WallFrame(int i, int j, bool randomizeFrame, ref int style, ref int frameNumber)
	{
		ForegroundWallLoader.SpecialWallFraming(i, j, frameNumber);
		return false;
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		ForegroundWallLoader.AddPoint(i, j);
		return true;
	}
}