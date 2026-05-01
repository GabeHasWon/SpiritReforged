using SpiritReforged.Common.UI.PotCatalogue;
using SpiritReforged.Common.UI.System;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Forest.Glyphs;
using System.Linq;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace SpiritReforged.Common.UI.Misc;

public class EnchanterUI : AutoUIState
{
	public static readonly Asset<Texture2D> LowerPanel = DrawHelpers.RequestLocal<EnchanterUI>("GlyphBubble", false);

	private CatalogueList _list;
	private UIElement _upperPanel;
	private UIElement _lowerPanel;
	private UIElement _infoPanel;
	private BasicItemSlot _slot;
	private UIImageButton _selectButton;
	private UIImageButton _completeButton;

	private bool _populated;

	public override void OnInitialize()
	{
		Width.Set(320, 0);
		Height.Set(200, 0);
		Left.Set(44, 0);
		HAlign = 0.0f;
		VAlign = 0.3f;

		_upperPanel = new();
		_upperPanel.Width = StyleDimension.Fill;
		_upperPanel.Height.Set(80, 0);

		_lowerPanel = new();
		_lowerPanel.Width.Set(0, 0.6f);
		_lowerPanel.Height = new StyleDimension(Height.Pixels - _upperPanel.Height.Pixels, 0);
		_lowerPanel.Left.Set(26, 0);
		_lowerPanel.Top.Set(_upperPanel.Height.Pixels, 0);

		_infoPanel = new();
		_infoPanel.Width.Set(0, 0.25f);
		_infoPanel.Height = new StyleDimension(Height.Pixels - _upperPanel.Height.Pixels, 0);
		_infoPanel.Top.Set(_upperPanel.Height.Pixels, 0);
		_infoPanel.HAlign = 0.97f;

		_list = new();
		_list.Width = _list.Height = new StyleDimension(-20, 1f);
		_list.Left = _list.Top = new StyleDimension(10, 0);
		_list.OverflowHidden = false;

		_slot = new(new Item(), ItemSlot.Context.PrefixItem);
		_slot.Left.Set(0, 0);
		_slot.Top.Set(0, 0);

		_selectButton = new(TextureAssets.Reforge[0]);
		_selectButton.Left.Set(_slot.Width.Pixels - 10, 0);
		_selectButton.Top.Set(_slot.Height.Pixels - 10, 0);

		_completeButton = new(TextureAssets.Reforge[0]);
		_completeButton.Left.Set(_slot.Width.Pixels + 10, 0);
		_completeButton.Top.Set(0, 0);

		OverrideSamplerState = SamplerState.PointClamp;
		Append(_upperPanel);
		Append(_lowerPanel);
		Append(_infoPanel);

		_upperPanel.Append(_slot);
		_upperPanel.Append(_selectButton);
		_upperPanel.Append(_completeButton);
		_lowerPanel.Append(_list);
	}

	public override void Update(GameTime gameTime)
	{
		if (Main.LocalPlayer.controlInv || !Main.playerInventory)
			UISystem.SetInactive<EnchanterUI>();

		if (_slot.Item.IsAir)
		{
			if (_populated)
			{
				RemoveChild(_lowerPanel);
				_list.ClearEntries();
			}

			_populated = false;
		}
		else
		{
			if (!_populated)
			{
				Append(_lowerPanel);

				var glyphItems = ModContent.GetContent<GlyphItem>().ToList();
				foreach (GlyphItem item in glyphItems)
					_list.AddEntry(new EnchantmentUI.GlyphButton(item.Type));
			}

			_populated = true;
		}

		Recalculate();

		base.Update(gameTime);
	}

	public override void Draw(SpriteBatch spriteBatch)
	{
		//Draw the background panel
		if (_lowerPanel.Parent == this)
		{
			var area = _lowerPanel.GetDimensions().ToRectangle();
			Texture2D texture = LowerPanel.Value;

			Main.spriteBatch.Draw(texture, area.Center() + new Vector2(46, -4), null, Color.White * 0.8f, 0, texture.Size() / 2, 1, 0, 0);
		}

		base.Draw(spriteBatch);
	}

	private void OnClickButton(UIMouseEvent evt, UIElement listeningElement)
	{
		if (ItemLoader.GetItem((listeningElement as EnchantmentUI.GlyphButton).itemType) is GlyphItem glyphItem)
			glyphItem.ApplyGlyph(_slot.Item, new GlyphItem.ApplyContext(Main.LocalPlayer));
	}
}