using SpiritReforged.Content.Ocean.Items;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;
using Terraria.GameInput;

namespace SpiritReforged.Content.Ocean.Tiles;

public class OceanPirateChest : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileSpelunker[Type] = true;
		Main.tileContainer[Type] = true;
		Main.tileShine2[Type] = true;
		Main.tileShine[Type] = 1200;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileOreFinderPriority[Type] = 500;

		TileID.Sets.HasOutlines[Type] = true;
		TileID.Sets.CanBeClearedDuringGeneration[Type] = false;
		TileID.Sets.CanBeClearedDuringOreRunner[Type] = false;
		TileID.Sets.HasOutlines[Type] = true;
		TileID.Sets.BasicChest[Type] = true;
		TileID.Sets.DisableSmartCursor[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = new Point16(0, 1);
		TileObjectData.newTile.CoordinateHeights = [16, 18];
		TileObjectData.newTile.HookCheckIfCanPlace = new PlacementHook(Chest.FindEmptyChest, -1, 0, true);
		TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(Chest.AfterPlacement_Hook, -1, 0, false);
		TileObjectData.newTile.AnchorInvalidTiles = [127];
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.LavaDeath = false;
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
		TileObjectData.addTile(Type);

		LocalizedText name = CreateMapEntryName();
		AddMapEntry(new Color(161, 115, 54), name, MapChestName);
		AddMapEntry(new Color(87, 64, 31), name, MapChestName);

		DustType = DustID.Dirt;
		AdjTiles = [TileID.Containers];
	}

	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;
	public override ushort GetMapOption(int i, int j) => (ushort)(IsLockedChest(i, j) ? 1 : 0);
	public override bool IsLockedChest(int i, int j) => Main.tile[i, j] != null && Main.tile[i, j].TileFrameX > 18;

	public static string MapChestName(string name, int i, int j)
	{
		Tile tile = Main.tile[i, j];
		if (tile == null)
			return name;

		int left = i, top = j;

		if (tile.TileFrameX % 54 != 0)
			left--;

		if (tile.TileFrameY != 0)
			top--;

		int chest = Chest.FindChest(left, top);
		if (chest != -1 && Main.chest[chest].name != "")
			name += ": " + Main.chest[chest].name;
		return name;
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY) => Chest.DestroyChest(i, j);

	public override bool RightClick(int i, int j)
	{
		Player player = Main.LocalPlayer;
		Tile tile = Main.tile[i, j];
		Main.mouseRightRelease = false;

		int left = i, top = j;
		if (tile.TileFrameX % 36 != 0)
			left--;
		if (tile.TileFrameY != 0)
			top--;

		if (player.sign >= 0)
		{
			SoundEngine.PlaySound(SoundID.MenuClose);
			player.sign = -1;
			Main.editSign = false;
			Main.npcChatText = "";
		}

		if (Main.editChest)
		{
			SoundEngine.PlaySound(SoundID.MenuTick);
			Main.editChest = false;
			Main.npcChatText = "";
		}

		if (player.editedChestName)
		{
			NetMessage.SendData(MessageID.SyncPlayerChest, -1, -1, NetworkText.FromLiteral(Main.chest[player.chest].name), player.chest, 1f);
			player.editedChestName = false;
		}

		bool isLocked = IsLockedChest(left, top);
		if (Main.netMode == NetmodeID.MultiplayerClient && !isLocked)
		{
			if (left == player.chestX && top == player.chestY && player.chest >= 0)
			{
				player.chest = -1;
				Recipe.FindRecipes();
				SoundEngine.PlaySound(SoundID.MenuClose);
			}
			else
			{
				NetMessage.SendData(MessageID.RequestChestOpen, -1, -1, null, left, top);
				Main.stackSplit = 600;
			}
		}
		else
		{
			if (isLocked)
			{
				int chestKey = ModContent.ItemType<PirateKey>();
				for (int k = 0; k < player.inventory.Length; k++)
				{
					if (player.inventory[k].type == chestKey && player.inventory[k].stack > 0)
					{
						for (int l = 0; l < 2; ++l) //Look into why Chest.Unlock(left, top) doesn't work???
							for (int v = 0; v < 2; ++v)
								Framing.GetTileSafely(left + l, top + v).TileFrameX -= 36;

						SoundEngine.PlaySound(SoundID.Unlock, new Vector2(left * 16, top * 16));

						if (--player.inventory[k].stack <= 0)
							player.inventory[k].TurnToAir();

						if (Main.netMode == NetmodeID.MultiplayerClient)
							NetMessage.SendTileSquare(-1, left, top, 2);
						break;
					}
				}
			}
			else
			{
				int chest = Chest.FindChest(left, top);
				if (chest >= 0)
				{
					Main.stackSplit = 600;

					if (chest == player.chest)
					{
						player.chest = -1;
						SoundEngine.PlaySound(SoundID.MenuClose);
					}
					else
					{
						player.chest = chest;
						Main.playerInventory = true;

						if (PlayerInput.GrappleAndInteractAreShared)
							PlayerInput.Triggers.JustPressed.Grapple = false;

						Main.recBigList = false;
						player.chestX = left;
						player.chestY = top;
						SoundEngine.PlaySound(player.chest < 0 ? SoundID.MenuOpen : SoundID.MenuTick);
					}

					Recipe.FindRecipes();
				}
			}
		}

		return true;
	}

	public override void MouseOver(int i, int j)
	{
		Player player = Main.LocalPlayer;
		Tile tile = Main.tile[i, j];
		int left = i, top = j;

		if (tile.TileFrameX % 36 != 0)
			left--;
		if (tile.TileFrameY != 0)
			top--;

		int chest = Chest.FindChest(left, top);
		player.cursorItemIconID = -1;

		if (chest < 0)
			player.cursorItemIconText = Language.GetTextValue("LegacyChestType.0");
		else
		{
			string defaultName = TileLoader.DefaultContainerName(tile.TileType, tile.TileFrameX, tile.TileFrameY);
			player.cursorItemIconText = Main.chest[chest].name.Length > 0 ? Main.chest[chest].name : defaultName;
			if (player.cursorItemIconText == defaultName)
			{
				player.cursorItemIconID = IsLockedChest(left, top) ? ModContent.ItemType<PirateKey>() : ModContent.ItemType<PirateChest>();
				player.cursorItemIconText = string.Empty;
			}
		}

		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
	}

	public override void MouseOverFar(int i, int j)
	{
		MouseOver(i, j);
		Player player = Main.LocalPlayer;
		if (player.cursorItemIconText == string.Empty)
		{
			player.cursorItemIconEnabled = false;
			player.cursorItemIconID = 0;
		}
	}
}