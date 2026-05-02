using SpiritReforged.Common.UI.Misc;
using SpiritReforged.Common.UI.PotCatalogue;
using SpiritReforged.Common.UI.System;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Forest.Glyphs;
using System.Linq;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace SpiritReforged.Common.UI.Enchantment;

public class EnchanterUI : AutoUIState
{
	public static readonly Asset<Texture2D> LowerPanel = DrawHelpers.RequestLocal<EnchanterUI>("GlyphBubble", false);
	public static readonly Asset<Texture2D> WaxIcon = DrawHelpers.RequestLocal<EnchanterUI>("ChromaticWaxIcon", false);

	private static GlyphItem _hovered;

	private CatalogueList _list;
	private CatalogueList _infoList;
	private BasicItemSlot _slot;
	private UIImageButton _confirmButton;

	private bool _populated;

	public override void OnInitialize()
	{
		Width.Set(400, 0);
		Height.Set(240, 0);
		Left.Set(44, 0);
		Top.Set(0, 0.24f);

		_list = new();
		_list.Width.Set(204, 0);
		_list.Height.Set(164, 0);
		_list.Left.Set(20, 0);
		_list.Top.Set(68, 0);
		_list.AddScrollbar(new UIScrollbar());

		_infoList = new();
		_infoList.Width.Set(160, 0);
		_infoList.Height.Set(164, 0);
		_infoList.Left.Set(_list.Left.Pixels + _list.Width.Pixels + 2, 0);
		_infoList.Top.Set(68, 0);
		_infoList.AddScrollbar(new UIScrollbar());

		_slot = new(new Item(), ItemSlot.Context.PrefixItem);
		_slot.Left.Set(0, 0);
		_slot.Top.Set(0, 0);

		_confirmButton = new(TextureAssets.Reforge[0]);
		_confirmButton.Left.Set(_slot.Width.Pixels - 10, 0);
		_confirmButton.Top.Set(_slot.Height.Pixels - 10, 0);
		_confirmButton.OnLeftClick += OnClickConfirmButton;

		OverrideSamplerState = SamplerState.PointClamp;
		Append(_list);
		Append(_infoList);
		Append(_slot);
		Append(_confirmButton);
	}

	public override void Update(GameTime gameTime)
	{
		//RemoveAllChildren(); //DEBUG
		//Initialize();

		if (Main.LocalPlayer.controlInv || !Main.playerInventory)
			UISystem.SetInactive<EnchanterUI>();

		if (ContainsPoint(Main.MouseScreen))
			Main.LocalPlayer.mouseInterface = true;

		if (_slot.Item.IsAir)
		{
			if (_populated)
			{
				RemoveChild(_list);
				_list.ClearEntries();
				_infoList.ClearEntries();
			}

			_populated = false;
		}
		else
		{
			if (!_populated)
			{
				Append(_list);

				var glyphItems = ModContent.GetContent<GlyphItem>().ToList();
				foreach (GlyphItem item in glyphItems)
				{
					var button = new EnchantmentUI.GlyphButton(item.Type);
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

		//CatalogueUI.DrawPanel(spriteBatch, GetDimensions().ToRectangle(), Color.White.Additive() * 0.2f); //DEBUG visualisation
		//CatalogueUI.DrawPanel(spriteBatch, _upperPanel.GetDimensions().ToRectangle(), Color.White.Additive() * 0.2f);
		//CatalogueUI.DrawPanel(spriteBatch, _list.GetDimensions().ToRectangle(), Color.White.Additive() * 0.2f);
		//CatalogueUI.DrawPanel(spriteBatch, _infoList.GetDimensions().ToRectangle(), Color.White.Additive() * 0.2f);

		base.Draw(spriteBatch);
	}

	private void OnClickGlyphButton(UIMouseEvent evt, UIElement listeningElement)
	{
		if (ItemLoader.GetItem((listeningElement as EnchantmentUI.GlyphButton).itemType) is GlyphItem glyphItem)
		{
			_infoList.ClearEntries();

			_hovered = glyphItem;
			AddInfoElements();
		}
	}

	private void OnClickConfirmButton(UIMouseEvent evt, UIElement listeningElement)
	{
		if (_hovered != default)
			_hovered.ApplyGlyph(_slot.Item, new GlyphItem.ApplyContext(Main.LocalPlayer));
	}

	private void AddInfoElements()
	{
		var info = new CatalogueInfo();
		info.Width = _infoList.Width;
		info.Height.Set(30, 0);
		info.Action += NameInfo_Action;

		_infoList.AddEntry(info);

		info = new CatalogueInfo();
		info.Width = _infoList.Width;
		info.Height.Set(30, 0);
		info.Action += PriceInfo_Action;

		_infoList.AddEntry(info);

		info = new CatalogueInfo();
		info.Width = _infoList.Width;
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
		CatalogueUI.DrawPanel(spriteBatch, bounds, Color.Black * 0.2f, Color.Black * 0.1f);
		Texture2D texture = WaxIcon.Value;

		spriteBatch.Draw(texture, bounds.Left() + new Vector2(14, 0), null, Color.White, 0, texture.Size() / 2, 1, 0, 0);

		if (Enchanter.ValueByType.TryGetValue(_hovered.Type, out int value))
			Utils.DrawBorderString(spriteBatch, value.ToString(), bounds.Center(), Main.MouseTextColorReal, 0.9f, 0.5f, 0.5f);

		return true;
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

		return true;
	}
	#endregion
}