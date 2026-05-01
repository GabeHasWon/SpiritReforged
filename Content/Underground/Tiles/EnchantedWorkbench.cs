using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.UI.Misc;
using SpiritReforged.Common.UI.System;
using SpiritReforged.Content.Forest.Glyphs;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underground.Tiles;

public class EnchantedWorkbench : ModTile
{
	public const int FullFrameWidth = 18 * 3;

	/// <summary> The tile coordinates of the workbench currently in use.<br/>
	/// Valid for the <b>local client</b> only. </summary>
	internal static Point16 TargetWorkbench = Point16.Zero;

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileOreFinderPriority[Type] = 600;
		Main.tileSpelunker[Type] = true;
		Main.tileNoFail[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x4);
		TileObjectData.newTile.Origin = new(2, 3);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		DustType = -1;

		AddMapEntry(new Color(50, 25, 55), CreateMapEntryName());
	}

	/// <summary> Deactivates the workbench tile at the provided coordinates and syncs it. </summary>
	public static void Deactivate(int i, int j)
	{
		TileExtensions.GetTopLeft(ref i, ref j);
		for (int x = i; x < i + 3; x++)
		{
			for (int y = j; y < j + 4; y++)
			{
				Tile tile = Framing.GetTileSafely(x, y);

				if (tile.HasTile && tile.TileType == ModContent.TileType<EnchantedWorkbench>())
					tile.TileFrameX += FullFrameWidth;
			}
		}

		NetMessage.SendTileSquare(-1, i, j, 3, 4);
	}

	public override void MouseOver(int i, int j)
	{
		if (UISystem.IsActive<EnchantmentUI>() || Framing.GetTileSafely(i, j).TileFrameX >= FullFrameWidth)
			return;

		Player player = Main.LocalPlayer;

		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = ModContent.ItemType<ChromaticWax>();
		player.noThrow = 2;
	}

	public override bool RightClick(int i, int j)
	{
		if (Framing.GetTileSafely(i, j).TileFrameX >= FullFrameWidth)
			return false;

		UISystem.SetActive<EnchantmentUI>();
		Main.playerInventory = true;

		TargetWorkbench = new(i, j);

		return true;
	}

	public override void NearbyEffects(int i, int j, bool closer)
	{
		if (!closer && TileObjectData.IsTopLeft(i, j) && Main.LocalPlayer.DistanceSQ(new Vector2(i + 1, j) * 16) > 100 * 100)
		{
			UISystem.SetInactive<EnchantmentUI>();
			TargetWorkbench = Point16.Zero;
		}
	}
}