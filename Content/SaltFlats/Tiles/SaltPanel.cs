using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;

namespace SpiritReforged.Content.SaltFlats.Tiles;

public class SaltPanel : ModTile, IAutoloadTileItem
{
	public void AddItemRecipes(ModItem item)
	{
		item.CreateRecipe().AddRecipeGroup("Salt").AddTile(TileID.WorkBenches).Register();
		item.CreateRecipe().AddIngredient(AutoContent.ItemType<SaltPlate>()).AddTile(TileID.WorkBenches).Register();
	}

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;

		TileID.Sets.CorruptBiome[Type] = -2;
		TileID.Sets.CrimsonBiome[Type] = -2;

		AddMapEntry(new Color(40, 40, 40));
		DustType = DustID.Asphalt;
	}

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak) => TileFraming.Gemspark(i, j, resetFrame);
}