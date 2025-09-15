using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;

namespace SpiritReforged.Content.Forest.Misc.OtherworldlyRadio;

public class OtherworldlyRadioItem : ModItem
{
	public override void SetStaticDefaults()
	{
		NPCShopHelper.AddEntry(new NPCShopHelper.ConditionalEntry(static shop => shop.NpcType == NPCID.PartyGirl, new NPCShop.Entry(Type)));
		Main.RegisterItemAnimation(Type, new DrawAnimationVertical(2, 2) { NotActuallyAnimating = true });
	}

	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<OtherworldlyRadioPlaced>());
		Item.width = 30;
		Item.height = 32;
		Item.value = Item.sellPrice(gold: 3);
		Item.rare = ItemRarityID.Blue;
		Item.maxStack = 1;
	}

	public override bool ConsumeItem(Player player) => false;
	public override bool CanRightClick() => true;
	public override void RightClick(Player player) => Main.swapMusic = !Main.swapMusic;

	public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
	{
		frame.Y = (frame.Height + 2) * (Main.swapMusic ? 0 : 1);
		spriteBatch.Draw(TextureAssets.Item[Type].Value, position, frame, Item.GetAlpha(drawColor), 0, origin, scale, default, 0);

		return false;
	}
}

public class OtherworldlyRadioPlaced : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileObsidianKill[Type] = true;

		TileID.Sets.HasOutlines[Type] = true;
		TileID.Sets.DisableSmartCursor[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.Origin = new Point16(1, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidWithTop | AnchorType.SolidTile | AnchorType.Table, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.CoordinateHeights = [16, 18];
		TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
		TileObjectData.addAlternate(1);
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(142, 92, 79), CreateMapEntryName());
		RegisterItemDrop(ModContent.ItemType<OtherworldlyRadioItem>());

		AdjTiles = [TileID.MusicBoxes];
		DustType = -1;
	}

	public override bool RightClick(int i, int j)
	{
		SoundEngine.PlaySound(SoundID.Mech, new Vector2(i * 16, j * 16));
		HitWire(i, j);
		return true;
	}

	public override void EmitParticles(int i, int j, Tile tile, short tileFrameX, short tileFrameY, Color tileLight, bool visible)
	{
		if (visible && Main.swapMusic && tile.TileFrameX % 36 == 0 && tile.TileFrameY % 36 == 0 && MusicBoxTile.SpawnNote)
			MusicBoxTile.SpawnMusicNote(i, j);
	}

	public override void MouseOver(int i, int j)
	{
		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = ModContent.ItemType<OtherworldlyRadioItem>();
	}

	public override void HitWire(int i, int j) => Main.swapMusic = !Main.swapMusic;
	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;
}