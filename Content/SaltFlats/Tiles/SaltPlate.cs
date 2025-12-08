using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;

namespace SpiritReforged.Content.SaltFlats.Tiles;

public class SaltPlate : ModTile, IAutoloadTileItem
{
	public void AddItemRecipes(ModItem item) => item.CreateRecipe().AddIngredient(AutoContent.ItemType<SaltBlockDull>()).AddTile(TileID.WorkBenches).Register();

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;

		TileID.Sets.CorruptBiome[Type] = -2;
		TileID.Sets.CrimsonBiome[Type] = -2;

		AddMapEntry(new Color(180, 170, 170));
		DustType = DustID.Pearlsand;
	}
}