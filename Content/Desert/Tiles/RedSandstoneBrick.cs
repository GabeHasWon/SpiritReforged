using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.TileMerging;

namespace SpiritReforged.Content.Desert.Tiles;

public class RedSandstoneBrick : ModTile, IAutoloadTileItem
{
	public virtual void AddItemRecipes(ModItem item)
	{
		int pillar = AutoContent.ItemType<RuinedSandstonePillar>();

		item.CreateRecipe().AddIngredient(ItemID.Sandstone).AddTile(TileID.Furnaces).Register();

		Recipe.Create(pillar).AddIngredient(item.Type, 2).AddTile(TileID.WorkBenches).Register();
		item.CreateRecipe(2).AddIngredient(pillar).AddTile(TileID.WorkBenches).Register();
	}

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;

		TileID.Sets.GeneralPlacementTiles[Type] = false;
		TileID.Sets.CanBeClearedDuringOreRunner[Type] = false;

		this.Merge(ModContent.TileType<RedSandstoneBrick>(), ModContent.TileType<RedSandstoneBrickCracked>(), ModContent.TileType<PaleHive>(), ModContent.TileType<InfectedHive>(), TileID.Sand);
		AddMapEntry(new Color(174, 74, 48));

		DustType = DustID.DynastyShingle_Red;
		this.AutoItem().ResearchUnlockCount = 100;
	}

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch) => TileMerger.DrawMerge(spriteBatch, i, j, ModContent.TileType<PaleHive>(), ModContent.TileType<InfectedHive>(), TileID.Sand);
	public override void ModifyFrameMerge(int i, int j, ref int up, ref int down, ref int left, ref int right, ref int upLeft, ref int upRight, ref int downLeft, ref int downRight) => RuinedSandstonePillar.SetupMerge(Type, ref up, ref down);
}