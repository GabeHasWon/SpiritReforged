using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;

namespace SpiritReforged.Content.Underground.Pottery;

public class PotteryWheel : ModTile, IAutoloadTileItem
{
	private const int FullFrameHeight = 18 * 3;

	public void AddItemRecipes(ModItem item) => item.CreateRecipe().AddIngredient(ItemID.StoneBlock, 10)
		.AddRecipeGroup(RecipeGroupID.Wood, 15).AddIngredient(ItemID.BottledWater).Register();

	public override void SetStaticDefaults()
	{
		Main.tileLighted[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileWaterDeath[Type] = true;
		Main.tileLavaDeath[Type] = true;
		TileID.Sets.InteractibleByNPCs[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(254, 121, 2), this.AutoModItem().DisplayName);
		DustType = DustID.WoodFurniture;
		AnimationFrameHeight = FullFrameHeight;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
	public override void AnimateTile(ref int frame, ref int frameCounter)
	{
		if (++frameCounter >= 4)
		{
			frameCounter = 0;
			frame = ++frame % 4;
		}
	}
}