using SpiritReforged.Common;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using Terraria.Audio;
using Terraria.IO;

namespace SpiritReforged.Content.Forest.Graveyard;

public class StonePillar : ModTile, IAutoloadTileItem
{
	/// <summary> Changes <see cref="StonePillar"/> tiles to <see cref="Main.tileFrameImportant"/> during world save for accurate player-applied framing. </summary>
	public sealed class PillarSaveLoader : ILoadable
	{
		public void Load(Mod mod) => On_WorldFile.InternalSaveWorld += PreSaveWorld;

		private static void PreSaveWorld(On_WorldFile.orig_InternalSaveWorld orig, bool useCloudSaving, bool resetTime)
		{
			Main.tileFrameImportant[ModContent.TileType<StonePillar>()] = true;
			orig(useCloudSaving, resetTime);
		}

		public void Unload() { }
	}

	private static bool IsChippedFrame = false; //Preserves alternate framing past TileFrame

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

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
	{
		if (Main.tile[i, j].TileFrameY >= 90)
			IsChippedFrame = true;

		Main.tileFrameImportant[Type] = false; //Ensure this type doesn't use frame important saving
		return TileFraming.Gemspark(i, j, resetFrame);
	}

	public override void PostTileFrame(int i, int j, int up, int down, int left, int right, int upLeft, int upRight, int downLeft, int downRight)
	{
		if (IsChippedFrame)
		{
			Tile tile = Main.tile[i, j];

			if (tile.TileFrameY < 90)
				tile.TileFrameY += 90;

			IsChippedFrame = false;
		}
	}

	public override bool Slope(int i, int j)
	{
		ChipTile(i, j);
		return false; //Never actually slope this tile
	}

	/// <summary> Applies custom framing, plas audiovisual effects, and syncs the tile if necessary. </summary>
	public static void ChipTile(int i, int j)
	{
		Tile tile = Main.tile[i, j];
		if (tile.TileFrameY < 90)
		{
			tile.TileFrameY += 90;

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