using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.UI.Misc;
using SpiritReforged.Common.UI.PotCatalogue;
using SpiritReforged.Common.UI.System;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Glyphs;
using System.Linq;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace SpiritReforged.Common.UI.Enchantment;

public class EnchanterUI : AutoUIState
{
	private class EnchanterButton(int style, LocalizedText hoverText) : UIElement
	{
		public const int COLUMNS = 3;
		public const int ROWS = 2;

		public static readonly Asset<Texture2D> IconTexture = DrawHelpers.RequestLocal<EnchanterUI>(nameof(EnchanterButton), false);

		public readonly int style = style;
		public readonly LocalizedText hoverText = hoverText;

		public bool ShowHoverEffects { get; set; }
		public Color DrawColor { get; set; }

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			bool hovering = ShowHoverEffects;
			Texture2D texture = IconTexture.Value;
			Rectangle source = texture.Frame(COLUMNS, ROWS, style, hovering ? 1 : 0, -2, -2);

			if (hovering)
			{
				Main.hoverItemName = hoverText.Value;
				Main.mouseText = true;

				Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;
				spriteBatch.Draw(bloom, GetDimensions().Center(), null, Color.Cyan.Additive(), 0, bloom.Size() / 2, new Vector2(1f / bloom.Width * source.Width * 1.5f, 1f / bloom.Height * source.Height), 0, 0);
			}

			spriteBatch.Draw(texture, GetDimensions().Center(), source, DrawColor, 0, source.Size() / 2, 1, 0, 0);
		}
	}

	public static readonly SoundStyle RemoveEnchantment = new("SpiritReforged/Assets/SFX/Item/Shatter2");
	public static readonly Asset<Texture2D> LowerPanel = DrawHelpers.RequestLocal<EnchanterUI>("GlyphBubble", false);

	private static GlyphItem _hovered;

	private CatalogueList _list;
	private CatalogueList _infoList;
	private BasicItemSlot _slot;
	private EnchanterButton _confirmButton, _clearButton, _randomButton;

	private bool _populated;

	public override void OnInitialize()
	{
		Width.Set(400, 0);
		Height.Set(240, 0);
		Left.Set(44, 0);
		Top.Set(270, 0);

		_list = new(new CatalogueList.FullPadding(6));
		_list.Width.Set(204, 0);
		_list.Height.Set(164, 0);
		_list.Left.Set(34, 0);
		_list.Top.Set(54, 0);
		_list.AddScrollbar(new UIScrollbar());

		_infoList = new();
		_infoList.Width.Set(160, 0);
		_infoList.Height.Set(164, 0);
		_infoList.Left.Set(_list.Left.Pixels + _list.Width.Pixels + 2, 0);
		_infoList.Top = _list.Top;
		_infoList.AddScrollbar(new UIScrollbar());

		_slot = new(new Item(), ItemSlot.Context.PrefixItem);
		_slot.Left.Set(0, 0);
		_slot.Top.Set(0, 0);

		_confirmButton = new(0, Language.GetText("Mods.SpiritReforged.Misc.Enchantment.Enchant"));
		_confirmButton.Width = _confirmButton.Height = new(30, 0);
		_confirmButton.Left.Set(_slot.Width.Pixels + 5, 0);
		_confirmButton.Top.Set(16, 0);
		_confirmButton.OnLeftClick += OnClickConfirmButton;
		_confirmButton.OnUpdate += OnHoverConfirmButton;

		_clearButton = new(1, Language.GetText("Mods.SpiritReforged.Misc.Enchantment.Clear"));
		_clearButton.Width = _clearButton.Height = new(30, 0);
		_clearButton.Left.Set(_list.Width.Pixels - 50, 0);
		_clearButton.Top.Set(_list.Height.Pixels + 20, 0);
		_clearButton.OnLeftClick += OnClickClearButton;
		_clearButton.OnUpdate += OnHoverClearButton;

		_randomButton = new(2, Language.GetText("Mods.SpiritReforged.Misc.Enchantment.Random"));
		_randomButton.Width = _randomButton.Height = new(30, 0);
		_randomButton.Left.Set(_list.Width.Pixels - 20, 0);
		_randomButton.Top.Set(_list.Height.Pixels + 20, 0);
		_randomButton.OnLeftClick += OnClickRandomButton;
		_randomButton.OnUpdate += OnHoverRandomButton;

		OverrideSamplerState = SamplerState.PointClamp;
		Append(_slot);
	}

	public override void Update(GameTime gameTime)
	{
		if (Main.LocalPlayer.TalkNPC == null)
		{
			UISystem.SetInactive<EnchanterUI>();
			_hovered = default;
		}

		if (ContainsPoint(Main.MouseScreen))
			Main.LocalPlayer.mouseInterface = true;

		Main.npcChatText = string.Empty;

		if (_slot.Item.IsAir)
		{
			if (_slot.IsMouseHovering && !CanEnchant(Main.mouseItem))
				Main.mouseLeft = Main.mouseLeftRelease = false; //Prevent interaction with the slot if it is empty and an invalid item is held

			if (_populated)
				ClearList();

			_hovered = default;
			_populated = false;
		}
		else
		{
			if (!_populated)
				PopulateList();

			_populated = true;
		}

		base.Update(gameTime);
	}

	public override void OnDeactivate()
	{
		if (!_slot.Item.IsAir)
		{
			IEntitySource source = (NPC.FindFirstNPC(ModContent.NPCType<Enchanter>()) is int whoAmI) ? Main.npc[whoAmI].GetSource_GiftOrReward() : null;
			
			Main.LocalPlayer.QuickSpawnItem(source, _slot.Item.Clone());
			_slot.Item.TurnToAir();
		}
	}

	public override void Draw(SpriteBatch spriteBatch)
	{
		//Draw the background panel
		if (_list.Parent == this)
		{
			var area = _list.GetDimensions().ToRectangle();
			Texture2D texture = LowerPanel.Value;

			Main.spriteBatch.Draw(texture, area.Center() + new Vector2(81, -4), null, Color.White * 0.8f, 0, texture.Size() / 2, 1, 0, 0);
		}

		Vector2 position = _slot.GetDimensions().ToRectangle().TopRight() + new Vector2(6, 0);
		Color color = ChromaticWax.SpecialColor;

		string text = _slot.Item.IsAir 
			? Language.GetTextValue("Mods.SpiritReforged.Misc.Enchantment.PlaceToEnchant") 
			: $"{Language.GetTextValue("LegacyInterface.46")}: " + Language.GetTextValue("Mods.SpiritReforged.Misc.Enchantment.Cost", (_hovered?.Type is int type) ? Enchanter.SpecialShop[type].ToString() : 3, string.Format("{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B));

		Utils.DrawBorderString(spriteBatch, text, position, Main.MouseTextColorReal, 1, 0, 0);

		base.Draw(spriteBatch);
	}

	/// <summary> Checks whether <paramref name="item"/> can be affected by any glyph within <see cref="Enchanter.SpecialShop"/>. </summary>
	public static bool CanEnchant(Item item)
	{
		foreach (int type in Enchanter.SpecialShop.Keys)
		{
			if (ItemLoader.GetItem(type) is GlyphItem glyphItem && glyphItem.CanApplyGlyph(item))
				return true;
		}

		return false;
	}

	private void PopulateList()
	{
		Append(_list);
		Append(_infoList);
		Append(_confirmButton);
		Append(_clearButton);
		Append(_randomButton);

		foreach (int type in Enchanter.SpecialShop.Keys)
		{
			bool canApply = ItemLoader.GetItem(type) is GlyphItem glyphItem && glyphItem.CanApplyGlyph(_slot.Item);
			GlyphButton button = new(type, inactive: !canApply);

			if (canApply)
				button.OnLeftClick += OnClickGlyphButton;

			_list.AddEntry(button);
		}
	}

	private void ClearList()
	{
		RemoveChild(_list);
		RemoveChild(_infoList);
		RemoveChild(_confirmButton);
		RemoveChild(_clearButton);
		RemoveChild(_randomButton);

		_list.ClearEntries();
		_infoList.ClearEntries();

		_hovered = default;
	}

	#region evt
	private void OnClickGlyphButton(UIMouseEvent evt, UIElement listeningElement)
	{
		if (ItemLoader.GetItem((listeningElement as GlyphButton).itemType) is GlyphItem glyphItem)
		{
			_hovered = glyphItem;
			AddInfoElements();
		}
	}

	private void OnHoverConfirmButton(UIElement element)
	{
		var ui = element as EnchanterButton;

		ui.ShowHoverEffects = element.IsMouseHovering && _hovered != default && IsRichEnough();
		ui.DrawColor = (_hovered != default && IsRichEnough()) ? Color.White : Color.Gray * 0.5f;

		static bool IsRichEnough()
		{
			if (_hovered?.Type is not int type)
				return false;

			int cost = Enchanter.SpecialShop[type];
			return Main.LocalPlayer.FindItems(ModContent.ItemType<ChromaticWax>(), PlayerExtensions.FindAll, out PlayerExtensions.FoundItems foundItems) && foundItems.Count >= cost;
		}
	}

	private void OnHoverClearButton(UIElement element)
	{
		var ui = element as EnchanterButton;

		ui.ShowHoverEffects = element.IsMouseHovering && _slot.Item.HasGlyph();
		ui.DrawColor = _slot.Item.HasGlyph() ? Color.White : Color.Gray * 0.5f;
	}

	private void OnHoverRandomButton(UIElement element)
	{
		var ui = element as EnchanterButton;
		bool hovering = element.IsMouseHovering;

		ui.ShowHoverEffects = hovering;
		ui.DrawColor = Color.White;
	}

	private void OnClickConfirmButton(UIMouseEvent evt, UIElement listeningElement)
	{
		if (_hovered == default)
			return;

		int cost = Enchanter.SpecialShop[_hovered.Type];
		int type = ModContent.ItemType<ChromaticWax>();

		if (Main.LocalPlayer.FindItems(type, PlayerExtensions.FindAll, out PlayerExtensions.FoundItems foundItems) && foundItems.Count >= cost && _slot.Item.SetGlyph(new(_hovered.Type), new GlyphItem.ApplyContext(Main.LocalPlayer)))
		{
			for (int c = 0; c < cost; c++) //Consume the necessary number of currency
				foundItems.Consume();

			if (_slot.Item.TryGetGlobalItem(out GlyphItem.GlyphGlobalItem glyphGlobalItem))
				glyphGlobalItem.StartAnimation();

			ClearList(); //Reset the list
			PopulateList();
			SpawnCenteredText(_slot.Item, Main.LocalPlayer.Top);
		}
	}

	private void OnClickClearButton(UIMouseEvent evt, UIElement listeningElement)
	{
		Item item = _slot.Item;

		if (!item.HasGlyph())
			return;

		item.SetGlyph(default);
		item.prefix = 0;
		item.ClearNameOverride();
		item.Refresh(false);

		ClearList(); //Reset the list
		PopulateList();
		SpawnCenteredText(_slot.Item, Main.LocalPlayer.Top);

		SoundEngine.PlaySound(RemoveEnchantment with { Volume = 0.5f, Pitch = -0.5f });
		SoundEngine.PlaySound(SoundID.AbigailUpgrade with { Pitch = 0.2f, PitchVariance = 0.2f });
	}

	private void OnClickRandomButton(UIMouseEvent evt, UIElement listeningElement)
	{
		while (true)
		{
			int[] types = Enchanter.SpecialShop.Keys.ToArray();
			int selectedType = types[Main.rand.Next(types.Length)];
			int cost = Enchanter.SpecialShop[selectedType];

			if (!Main.LocalPlayer.FindItems(ModContent.ItemType<ChromaticWax>(), PlayerExtensions.FindAll, out PlayerExtensions.FoundItems foundItems) || foundItems.Count < cost)
				return;

			if (_slot.Item.SetGlyph(new(selectedType), new GlyphItem.ApplyContext(Main.LocalPlayer)))
			{
				for (int c = 0; c < cost; c++) //Consume the necessary number of currency
					foundItems.Consume();

				if (_slot.Item.TryGetGlobalItem(out GlyphItem.GlyphGlobalItem glyphGlobalItem))
					glyphGlobalItem.StartAnimation();

				ClearList(); //Reset the list
				PopulateList();
				SpawnCenteredText(_slot.Item, Main.LocalPlayer.Top);

				break;
			}
		}
	}
	#endregion

	private void AddInfoElements()
	{
		_infoList.ClearEntries();
		float width = _infoList.AvailableWidth + 2;

		var info = new CatalogueInfo();
		info.Width.Set(width, 0);
		info.Height.Set(40, 0);
		info.Action += NameInfo_Action;

		_infoList.AddEntry(info);

		info = new CatalogueInfo();
		info.Width.Set(width, 0);
		info.Height.Set(32 + UIHelper.GetTextHeight(_hovered.Tooltip.Value, (int)info.Width.Pixels), 0);
		info.Action += DescInfo_Action;

		_infoList.AddEntry(info);
	}

	private static void SpawnCenteredText(Item item, Vector2 center)
	{
		int index = PopupText.NewText(PopupTextContext.ItemReforge, item, item.stack, noStack: true);
		if (index != -1)
		{
			PopupText popup = Main.popupText[index];

			Vector2 stringSize = FontAssets.MouseText.Value.MeasureString(popup.name);
			popup.position = Main.LocalPlayer.Center - stringSize / 2;
		}
	}

	#region draw actions
	private bool NameInfo_Action(SpriteBatch spriteBatch, Rectangle bounds)
	{
		if (_hovered == default)
			return false;

		string name = _hovered.DisplayName.Value;
		var namePos = bounds.Center();

		Utils.DrawBorderString(spriteBatch, name, namePos, Main.MouseTextColorReal, 0.9f, 0.5f, 0.5f);

		return true;
	}

	private bool DescInfo_Action(SpriteBatch spriteBatch, Rectangle bounds)
	{
		if (_hovered == default)
			return false;

		string[] wrappingText = UIHelper.WrapText(_hovered.Tooltip.Value, bounds.Width);
		for (int i = 0; i < wrappingText.Length; i++)
		{
			string text = wrappingText[i];

			if (text is null)
				continue;

			float height = FontAssets.MouseText.Value.MeasureString(text).Y / 2;
			Utils.DrawBorderString(spriteBatch, text, bounds.Top() + new Vector2(0, 10 + height * i), Main.MouseTextColorReal, 0.8f, 0.5f, 0);
		}

		return false;
	}
	#endregion
}