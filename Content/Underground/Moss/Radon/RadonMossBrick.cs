using SpiritReforged.Common.ItemCommon;
using TileHelper.Common;

namespace SpiritReforged.Content.Underground.Moss.Radon;

public class RadonMossBrick : ModTile, ILoadItem
{
	public void AddItemRecipes(ModItem item) => item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<RadonMoss>()).AddIngredient(ItemID.ClayBlock, 10).AddTile(TileID.Furnaces).Register();

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileLighted[Type] = true;
		Main.tileBlockLight[Type] = true;
		Main.tileBrick[Type] = true;
		Main.tileMergeDirt[Type] = true;
		TileHelperSets.TileGlowmask[Type] = Helpers.RequestGlowmask(this);

		AddMapEntry(new Color(252, 248, 3));
		HitSound = SoundID.Tink;
		DustType = DustID.YellowTorch;
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) => (r, g, b) = (0.318f * 1.1f, 0.23f * 1.1f, 0.04f * 1.1f);
}