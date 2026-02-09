using SpiritReforged.Common;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Graveyard;

public class StonePillar : ModTile, IAutoloadTileItem
{
	public void AddItemRecipes(ModItem item) => item.CreateRecipe(2).AddIngredient(ItemID.StoneBlock).AddTile(TileID.HeavyWorkBench).Register();

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileBlockLight[Type] = false;

		TileID.Sets.CanBeSloped[Type] = true;
		TileID.Sets.IsBeam[Type] = true;
		SpiritSets.FrameHeight[Type] = 18;

		AddMapEntry(new Color(100, 100, 100));
		DustType = DustID.Stone;
		this.AutoItem().ResearchUnlockCount = 100;

		for (int type = 0; type < TileLoader.TileCount; type++)
			Main.tileMerge[type][Type] |= Main.tileSolid[type]; //Have everything merge with this type
	}

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak) => TileFraming.Gemspark(i, j, resetFrame);

	public override bool Slope(int i, int j)
	{
		Tile tile = Main.tile[i, j];
		if (tile.TileFrameX is 18 or 36 or 54 && tile.TileFrameY is 0) //Generic tops
		{
			Point16 result = new(18, 90);

			tile.TileFrameX = result.X;
			tile.TileFrameY = result.Y;
		}
		else if (tile.TileFrameX is 0 or 36 or 72 && tile.TileFrameY is 54) //Top left corners
		{
			Point16 result = new(0, 90);

			tile.TileFrameX = result.X;
			tile.TileFrameY = result.Y;
		}
		else if (tile.TileFrameX is 18 or 54 or 90 && tile.TileFrameY is 54) //Top right corners
		{
			Point16 result = new(36, 90);

			tile.TileFrameX = result.X;
			tile.TileFrameY = result.Y;
		}
		else if (tile.TileFrameX is 18 or 36 or 54 && tile.TileFrameY is 36) //Generic bottoms
		{
			Point16 result = new(72, 90);

			tile.TileFrameX = result.X;
			tile.TileFrameY = result.Y;
		}
		else if (tile.TileFrameX is 0 or 36 or 72 && tile.TileFrameY is 72) //Bottom left corners
		{
			Point16 result = new(54, 90);

			tile.TileFrameX = result.X;
			tile.TileFrameY = result.Y;
		}
		else if (tile.TileFrameX is 18 or 54 or 90 && tile.TileFrameY is 72) //Bottom right corners
		{
			Point16 result = new(90, 90);

			tile.TileFrameX = result.X;
			tile.TileFrameY = result.Y;
		}

		return false; //Never actually slope this tile
	}

	public override void ModifyFrameMerge(int i, int j, ref int up, ref int down, ref int left, ref int right, ref int upLeft, ref int upRight, ref int downLeft, ref int downRight)
	{
		if (left != -1 && Main.tileSolid[left] && !Main.tileNoAttach[left])
			left = Type;
		if (right != -1 && Main.tileSolid[right] && !Main.tileNoAttach[right])
			right = Type;
	} //Merge with valid tiles to the left and right
}