using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;
using Terraria.UI.Chat;

namespace SpiritReforged.Content.Forest.Misc.OtherworldlyRadio;

public class OtherworldlyRadioItem : ModItem
{
	public override void SetStaticDefaults()
	{
		NPCShopHelper.AddEntry(new NPCShopHelper.ConditionalEntry(static shop => shop.NpcType == NPCID.PartyGirl, new NPCShop.Entry(Type)));
		Main.RegisterItemAnimation(Type, new DrawAnimationVertical(2, 2) { NotActuallyAnimating = true });
		this.GetLocalization("TunedToNormal");
		this.GetLocalization("TunedToOtherworld");
		this.GetLocalization("MusicDisplay");
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

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		if (tooltips.FindIndex(x => x.Name == "ItemName") is int index and not -1)
		{
			tooltips[index].Text += " ";
			
			if (!Main.swapMusic)
				tooltips[index].Text += this.GetLocalization("TunedToNormal").Value;
			else
				tooltips[index].Text += this.GetLocalization("TunedToOtherworld").Value;
		}

		if (CrossMod.MusicDisplay.Enabled)
		{
			object[] info = (object[])CrossMod.MusicDisplay.Instance.Call("GetMusicInfo", (short)Main.curMusic);
			var name = (LocalizedText)info[0];
			var author = (LocalizedText)info[1];
			string text = this.GetLocalization("MusicDisplay").Value;

			tooltips.Add(new TooltipLine(Mod, "MusicDisplayInfo", text) { OverrideColor = Color.LightGray });
			tooltips.Add(new TooltipLine(Mod, "MusicDisplayName", name.Value));
			tooltips.Add(new TooltipLine(Mod, "MusicDisplayAuthor", author.Value));
		}
	}

	public override bool PreDrawTooltipLine(DrawableTooltipLine line, ref int yOffset)
	{
		if (line.Name == "MusicDisplayName")
		{
			var font = FontAssets.DeathText.Value;
			ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, line.Text, new Vector2(line.X, line.Y), Color.White, 0f, Vector2.Zero, new Vector2(0.5f));
			yOffset += 4;
			return false;
		}

		return true;
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