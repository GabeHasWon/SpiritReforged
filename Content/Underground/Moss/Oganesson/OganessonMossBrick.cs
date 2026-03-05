using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals.Glowmasks;

namespace SpiritReforged.Content.Underground.Moss.Oganesson;

[AutoloadGlowmask("255,255,255")]
public class OganessonMossBrick : ModTile, IAutoloadTileItem
{
	public void AddItemRecipes(ModItem item) => item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<OganessonMoss>()).AddIngredient(ItemID.ClayBlock, 10).AddTile(TileID.Furnaces).Register();
	
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileLighted[Type] = true;
		Main.tileBlockLight[Type] = true;
		Main.tileBrick[Type] = true;
		Main.tileMergeDirt[Type] = true;

		AddMapEntry(new Color(252, 252, 252));
		HitSound = SoundID.Tink;
		DustType = DustID.WhiteTorch;

		//Set item StaticDefaults
		var item = this.AutoItem();
		item.ResearchUnlockCount = 100;

	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) => (r, g, b) = (0.35f, 0.35f, 0.35f);
}