using SpiritReforged.Common;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using TileHelper.Common;

namespace SpiritReforged.Content.Savanna.Tiles;

public class Drywood : ModTile, ILoadItem
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;
		Main.tileBrick[Type] = true;
		Main.tileMergeDirt[Type] = true;

		DustType = DustID.t_PearlWood;
		AddMapEntry(new Color(145, 128, 109));

		//Set item static defaults
		var item = this.AutoItem();

		SpiritSets.Timber[item.type] = true;
		ItemID.Sets.ShimmerTransformToItem[item.type] = ItemID.Wood;
		Recipes.AddToGroup(RecipeGroupID.Wood, item.type);
		item.ResearchUnlockCount = 100;
	}
}