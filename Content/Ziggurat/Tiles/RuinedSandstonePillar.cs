using SpiritReforged.Common;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;

namespace SpiritReforged.Content.Ziggurat.Tiles;

public class RuinedSandstonePillar : ModTile, IAutoloadTileItem
{
	void IAutoloadTileItem.StaticItemDefaults(ModItem item) => item.Item.ResearchUnlockCount = 100;

	public void AddItemRecipes(ModItem item) => item.CreateRecipe(2).AddIngredient(ItemID.SmoothSandstone).AddTile(TileID.Sawmill).Register();

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileBlockLight[Type] = false;

		TileID.Sets.IsBeam[Type] = true;
		SpiritSets.FrameHeight[Type] = 18;

		AddMapEntry(new Color(174, 110, 48));

		DustType = DustID.Dirt;

		for (int type = 0; type < TileLoader.TileCount; type++)
			Main.tileMerge[type][Type] |= Main.tileSolid[type]; //Have everything merge with this type
	}

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak) => TileFraming.Gemspark(i, j, resetFrame);
	public override void PostTileFrame(int i, int j, int up, int down, int left, int right, int upLeft, int upRight, int downLeft, int downRight)
	{
		const short rowHeight = 90;

		if (j % 2 == 0)
			Main.tile[i, j].TileFrameY += rowHeight;
	}

	public override void ModifyFrameMerge(int i, int j, ref int up, ref int down, ref int left, ref int right, ref int upLeft, ref int upRight, ref int downLeft, ref int downRight)
	{
		if (left != -1 && Main.tileSolid[left] && !Main.tileNoAttach[left])
			left = Type;
		if (right != -1 && Main.tileSolid[right] && !Main.tileNoAttach[right])
			right = Type;
	} //Merge with valid tiles to the left and right

	public static void SetupMerge(int myType, ref int up, ref int down)
	{
		int type = ModContent.TileType<RuinedSandstonePillar>();

		if (up == type)
			up = myType;

		if (down == type)
			down = myType;
	}
}