using SpiritReforged.Common.ItemCommon.FloatingItem;
using SpiritReforged.Common.Misc;

namespace SpiritReforged.Content.Ocean.Items.Driftwood;

public class DriftwoodTileItem : FloatingItem
{
	public override float SpawnWeight => 1f;
	public override float Weight => base.Weight * 0.9f;
	public override float Bouyancy => base.Bouyancy * 1.05f;

	public override void SetStaticDefaults() => Recipes.AddToGroup(RecipeGroupID.Wood, Type);

	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<DriftwoodTile>());
		Item.width = Item.height = 16;
		Item.rare = ItemRarityID.White;
		Item.maxStack = Item.CommonMaxStack;
	}
}

public class DriftwoodTile : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileMergeDirt[Type] = true;
		Main.tileBlockLight[Type] = true;
		Main.tileMerge[Type][TileID.WoodBlock] = true;
		Main.tileMerge[TileID.WoodBlock][Type] = true;
		Main.tileMerge[Type][TileID.Sand] = true;
		Main.tileMerge[TileID.Sand][Type] = true;

		AddMapEntry(new Color(138, 79, 45));
	}
}