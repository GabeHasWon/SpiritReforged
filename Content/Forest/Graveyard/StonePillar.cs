using SpiritReforged.Common;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using Terraria.Audio;
using TileHelper.Common;

namespace SpiritReforged.Content.Forest.Graveyard;

public class StonePillar : ModTile, ILoadItem
{
	void ILoadItem.AddItemRecipes(ModItem item) => item.CreateRecipe(2).AddIngredient(ItemID.StoneBlock).AddTile(TileID.HeavyWorkBench).Register();

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileBlockLight[Type] = false;

		TileID.Sets.CanBeSloped[Type] = true;
		TileID.Sets.IsBeam[Type] = true;
		SpiritSets.FrameHeight[Type] = 18;

		TileMethods.Merge(Type, ModContent.TileType<StonePillarChipped>());
		AddMapEntry(new Color(100, 100, 100));
		DustType = DustID.Stone;

		for (int type = 0; type < TileLoader.TileCount; type++)
			Main.tileMerge[type][Type] |= Main.tileSolid[type]; //Have everything merge with this type
	}

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak) => TileFraming.Gemspark(i, j, resetFrame);

	public override bool Slope(int i, int j)
	{
		ChipTile(i, j, (ushort)ModContent.TileType<StonePillarChipped>());
		return false; //Never actually slope this tile
	}

	/// <summary> Applies custom framing, plays audiovisual effects, and syncs the tile if necessary. </summary>
	public static void ChipTile(int i, int j, ushort type)
	{
		Tile tile = Main.tile[i, j];
		if (tile.TileFrameY < 90)
		{
			tile.ResetToType(type);
			WorldGen.SquareTileFrame(i, j);

			for (int d = 0; d < 8; d++)
				Dust.NewDustDirect(new Vector2(i, j) * 16, 16, 16, DustID.Stone);

			SoundEngine.PlaySound(SoundID.Dig, new Vector2(i, j).ToWorldCoordinates());

			if (Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendTileSquare(-1, i, j);
		}
	}

	public override void ModifyFrameMerge(int i, int j, ref int up, ref int down, ref int left, ref int right, ref int upLeft, ref int upRight, ref int downLeft, ref int downRight)
	{
		if (left != -1 && Main.tileSolid[left] && !Main.tileNoAttach[left])
			left = Type;
		if (right != -1 && Main.tileSolid[right] && !Main.tileNoAttach[right])
			right = Type;
	} //Merge with valid tiles to the left and right
}

public class StonePillarChipped : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileBlockLight[Type] = false;

		TileID.Sets.CanBeSloped[Type] = true;
		TileID.Sets.IsBeam[Type] = true;
		SpiritSets.FrameHeight[Type] = 18;

		AddMapEntry(new Color(100, 100, 100));
		RegisterItemDrop(AutoContent.ItemType<StonePillar>());
		DustType = DustID.Stone;

		for (int type = 0; type < TileLoader.TileCount; type++)
			Main.tileMerge[type][Type] |= Main.tileSolid[type]; //Have everything merge with this type
	}

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak) => TileFraming.Gemspark(i, j, resetFrame);

	public override bool Slope(int i, int j)
	{
		StonePillar.ChipTile(i, j, (ushort)ModContent.TileType<StonePillar>());
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