using ReLogic.Graphics;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using System.Collections.ObjectModel;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;
using Terraria.UI;
using Terraria.UI.Chat;

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

		}
	}

	// TODO: Make an interface/helper system for this
	public override bool PreDrawTooltip(ReadOnlyCollection<TooltipLine> lines, ref int x, ref int y) //Assistant tooltip
	{
		const int padding = 17;

		if (Item.tooltipContext != ItemSlot.Context.InventoryItem || !CrossMod.MusicDisplay.Enabled)
			return true;

		var position = new Vector2(x - 14, y + 5);
		string text = this.GetLocalization("MusicDisplay").Value;
		(bool success, object[] info, _) = ((bool, object[], string message))CrossMod.MusicDisplay.Instance.Call("TryGetMusicInfo", (short)Main.curMusic);

		if (!success)
			return true;

		var name = (LocalizedText)info[0];
		var author = (LocalizedText)info[1];

		int textLength = GetBiggest(text, name.Value, author.Value);

		foreach (var line in lines)
			position.Y += FontAssets.MouseText.Value.MeasureString(line.Text).Y; //Position vertically

		if (Main.SettingsEnabled_OpaqueBoxBehindTooltips)
		{
			var bgSource = new Rectangle((int)position.X, (int)position.Y, textLength + padding, 34 * 3);
			Utils.DrawInvBG(Main.spriteBatch, bgSource, new Color(23, 25, 81, 255) * 0.925f);
		}

		DynamicSpriteFont font = FontAssets.ItemStack.Value;
		Color color = Main.MouseTextColorReal;

		ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, text, position + new Vector2(padding, 12), color, 0f, Vector2.Zero, Vector2.One);
		ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.DeathText.Value, name.Value, position + new Vector2(padding, 36), color, 0f, Vector2.Zero, new Vector2(0.5f));
		ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, author.Value, position + new Vector2(padding, 66), color, 0f, Vector2.Zero, Vector2.One);

		return true;
	}

	private static int GetBiggest(string text, string name, string author)
	{
		const int padding = 17;

		return (int)Math.Max(Size(text), Math.Max((FontAssets.DeathText.Value.MeasureString(name).X + padding * 2) * 0.5f, Size(author)));

		static float Size(string text) => FontAssets.MouseText.Value.MeasureString(text).X + padding;
	}

	//public override bool PreDrawTooltipLine(DrawableTooltipLine line, ref int yOffset)
	//{
	//	if (line.Name == "MusicDisplayName")
	//	{
	//		var font = FontAssets.DeathText.Value;
	//		Vector2 size = ChatManager.GetStringSize(font, line.Text, new Vector2(0.5f));
	//		Texture2D panel = Main.Assets.Request<Texture2D>("Images/UI/PanelBackground").Value;
	//		Texture2D border = Main.Assets.Request<Texture2D>("Images/UI/PanelBorder").Value;
	//		UIHelper.DrawPanel(Main.spriteBatch, panel, border, new Rectangle(line.X - 6, line.Y - 4, (int)size.X + 14, 38));
	//		ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, line.Text, new Vector2(line.X, line.Y + 2), Color.White, 0f, Vector2.Zero, new Vector2(0.5f));
	//		yOffset += 6;
	//		return false;
	//	}

	//	return true;
	//}

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

		AddMapEntry(new Color(142, 92, 79), Language.GetText("Mods.SpiritReforged.Items.OtherworldlyRadioItem.DisplayName"));
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