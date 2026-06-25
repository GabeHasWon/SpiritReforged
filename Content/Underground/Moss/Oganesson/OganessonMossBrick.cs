using SpiritReforged.Common.ItemCommon;
using TileHelper.Common;

namespace SpiritReforged.Content.Underground.Moss.Oganesson;

public class OganessonMossBrick : ModTile, ILoadItem
{
	public void AddItemRecipes(ModItem item) => item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<OganessonMoss>()).AddIngredient(ItemID.ClayBlock, 10).AddTile(TileID.Furnaces).Register();
	
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileLighted[Type] = true;
		Main.tileBlockLight[Type] = true;
		Main.tileBrick[Type] = true;
		Main.tileMergeDirt[Type] = true;
		Sets.TileGlowmask[Type] = Helpers.RequestGlowmask(this);

		AddMapEntry(new Color(252, 252, 252));
		HitSound = SoundID.Tink;
		DustType = DustID.WhiteTorch;
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) => (r, g, b) = (0.35f, 0.35f, 0.35f);
}