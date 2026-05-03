using SpiritReforged.Common.Misc;
using SpiritReforged.Common.UI.Misc;
using SpiritReforged.Common.UI.PotCatalogue;
using SpiritReforged.Common.UI.System;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Forest.Glyphs;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace SpiritReforged.Common.UI.Enchantment;

public class EnchanterUI : AutoUIState
{
	private class ConfirmButton : UIElement
	{
		public static readonly Asset<Texture2D> IconTexture = DrawHelpers.RequestLocal<EnchanterUI>("EnchantButton", false);

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			bool hovering = IsMouseHovering && _hovered != default;
			Texture2D texture = IconTexture.Value;
			Rectangle source = texture.Frame(1, 2, 0, hovering ? 1 : 0, 0, -2);

			if (hovering)
			{
				Main.hoverItemName = Language.GetTextValue("Mods.SpiritReforged.Misc.Enchantment.Enchant");
				Main.mouseText = true;

				DrawHelpers.DrawOutline(default, default, default, default, (offset) =>
					spriteBatch.Draw(texture, GetDimensions().Center() + offset.RotatedBy(Main.timeForVisualEffects / 20f), source, Color.White.Additive() * 0.3f, 0, source.Size() / 2, 1, 0, 0));
			}

			spriteBatch.Draw(texture, GetDimensions().Center(), source, hovering ? Color.White : Color.Gray * 0.5f, 0, source.Size() / 2, 1, 0, 0);
		}
	}

	public static readonly Asset<Texture2D> LowerPanel = DrawHelpers.RequestLocal<EnchanterUI>("GlyphBubble", false);
	public static readonly Asset<Texture2D> WaxIcon = DrawHelpers.RequestLocal<EnchanterUI>("ChromaticWaxIcon", false);

	private static GlyphItem _hovered;

	private CatalogueList _list;
	private CatalogueList _infoList;
	private BasicItemSlot _slot;
	private ConfirmButton _confirmButton;

	private bool _populated;

	public override void OnInitialize()
	{
		Width.Set(400, 0);
		Height.Set(240, 0);
		Left.Set(44, 0);
		Top.Set(0, 0.25f);

		_list = new();
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

		_confirmButton = new();
		_confirmButton.Width = _confirmButton.Height = new(30, 0);
		_confirmButton.Left.Set(_slot.Width.Pixels + 4, 0);
		_confirmButton.OnLeftClick += OnClickConfirmButton;

		OverrideSamplerState = SamplerState.PointClamp;
		//Append(_list);
		//Append(_infoList);
		Append(_slot);
		//Append(_confirmButton);
	}

	public override void Update(GameTime gameTime)
	{
		//RemoveAllChildren();
		//Initialize();

		if (Main.LocalPlayer.controlInv || !Main.playerInventory)
		{
			UISystem.SetInactive<EnchanterUI>();
			_hovered = default;
		}

		if (ContainsPoint(Main.MouseScreen))
			Main.LocalPlayer.mouseInterface = true;

		Main.LocalPlayer.SetTalkNPC(-1);

		if (_slot.Item.IsAir)
		{
			if (_populated)
			{
				RemoveChild(_list);
				RemoveChild(_infoList);
				RemoveChild(_confirmButton);

				_list.ClearEntries();
				_infoList.ClearEntries();
			}

			_hovered = default;
			_populated = false;
		}
		else
		{
			if (!_populated)
			{
				Append(_list);
				Append(_infoList);
				Append(_confirmButton);

				foreach (int type in Enchanter.SpecialShop.Keys)
				{
					var button = new EnchantmentUI.GlyphButton(type);
					button.OnLeftClick += OnClickGlyphButton;

					_list.AddEntry(button);
				}
			}

			_populated = true;
		}

		base.Update(gameTime);
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

		if (_slot.Item.IsAir)
		{
			Vector2 position = _slot.GetDimensions().ToRectangle().TopRight() + new Vector2(6, 0);
			Utils.DrawBorderString(spriteBatch, Language.GetTextValue("Mods.SpiritReforged.Misc.Enchantment.PlaceToEnchant"), position, Main.MouseTextColorReal, 1, 0, 0);
		}

		base.Draw(spriteBatch);
	}

	private void OnClickGlyphButton(UIMouseEvent evt, UIElement listeningElement)
	{
		if (ItemLoader.GetItem((listeningElement as EnchantmentUI.GlyphButton).itemType) is GlyphItem glyphItem)
		{
			_hovered = glyphItem;
			AddInfoElements();
		}
	}

	private void OnClickConfirmButton(UIMouseEvent evt, UIElement listeningElement)
	{
		if (_hovered != default)
		{
			int cost = Enchanter.SpecialShop[_hovered.Type];
			int type = ModContent.ItemType<ChromaticWax>();

			if (Main.LocalPlayer.CountItem(type, cost) == cost)
			{
				for (int c = 0; c < cost; c++)
					Main.LocalPlayer.ConsumeItem(type);

				_hovered.ApplyGlyph(_slot.Item, new GlyphItem.ApplyContext(Main.LocalPlayer));
				GlyphGlobalItem.StartAnimation(_slot.Item);
			}
		}
	}

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
		info.Height.Set(30, 0);
		info.Action += PriceInfo_Action;

		_infoList.AddEntry(info);

		info = new CatalogueInfo();
		info.Width.Set(width, 0);
		info.Height.Set(32 + UIHelper.GetTextHeight(_hovered.Tooltip.Value, (int)info.Width.Pixels), 0);
		info.Action += DescInfo_Action;

		_infoList.AddEntry(info);
	}

	#region draw actions
	private bool PriceInfo_Action(SpriteBatch spriteBatch, Rectangle bounds)
	{
		if (_hovered == default)
			return false;

		bounds.Y -= 4;
		Rectangle innerBounds = new(bounds.X, bounds.Y, 50, bounds.Height);
		CatalogueUI.DrawPanel(spriteBatch, innerBounds, Color.Black * 0.3f, Color.Black * 0.2f);
		Texture2D texture = WaxIcon.Value;

		spriteBatch.Draw(texture, innerBounds.Left() + new Vector2(14, 0), null, Color.White, 0, texture.Size() / 2, 1, 0, 0);
		Utils.DrawBorderString(spriteBatch, Enchanter.SpecialShop[_hovered.Type].ToString(), innerBounds.Right() + new Vector2(-12, 4), Main.MouseTextColorReal, 0.9f, 0.5f, 0.5f);

		if (innerBounds.Contains(Main.MouseScreen.ToPoint()))
			Main.hoverItemName = Language.GetTextValue("LegacyInterface.46");

		return false;
	}

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