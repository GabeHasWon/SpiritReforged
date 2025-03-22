using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat.Classic;
using SpiritReforged.Common.TileCommon;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Ocean.Tiles;

public class Driftwood : ModTile, IAutoloadTileItem
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;
		Main.tileBrick[Type] = true;
		Main.tileMergeDirt[Type] = true;

		AddMapEntry(new Color(138, 79, 45));

		//Set item StaticDefaults
		var item = this.AutoItem();
		Recipes.AddToGroup(RecipeGroupID.Wood, item.type);
		item.ResearchUnlockCount = 100;

		CrateDatabase.AddCrateRule(ItemID.OceanCrate, ItemDropRule.Common(item.type, 5, 10, 30));
		CrateDatabase.AddCrateRule(ItemID.OceanCrateHard, ItemDropRule.Common(item.type, 5, 10, 30));

		SpiritClassic.AddReplacement("DriftwoodTileItem", this.AutoItem().type);
	}
}