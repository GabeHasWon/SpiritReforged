using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using TileHelper.Common;

namespace SpiritReforged.Content.SaltFlats.Tiles;

public class SaltPanel : ModTile, ICreateItem
{
	public void ItemRecipes(ModItem modItem)
	{
		modItem.CreateRecipe().AddRecipeGroup("Salt").AddTile(TileID.WorkBenches).Register();
		modItem.CreateRecipe().AddIngredient(AutoContent.ItemType<SaltPlate>()).AddTile(TileID.WorkBenches).Register();
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