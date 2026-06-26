using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat.Classic;
using SpiritReforged.Common.TileCommon;
using Terraria.GameContent.ItemDropRules;
using TileHelper.Common;

namespace SpiritReforged.Content.Ocean.Tiles;

public class Driftwood : ModTile, ILoadItem
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

		ItemLootDatabase.AddItemRule(ItemID.OceanCrate, ItemDropRule.Common(item.type, 4, 15, 35));
		ItemLootDatabase.AddItemRule(ItemID.OceanCrateHard, ItemDropRule.Common(item.type, 4, 15, 35));

		SpiritClassic.AddItemReplacement("DriftwoodTileItem", item.type);
		ItemID.Sets.ShimmerTransformToItem[item.type] = ItemID.Wood;
	}
}