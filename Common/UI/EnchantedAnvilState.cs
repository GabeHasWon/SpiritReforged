using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.UI.Misc;
using SpiritReforged.Common.UI.System;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Forest.Glyphs;
using System.Linq;
using Terraria.UI;

namespace SpiritReforged.Common.UI;

public class EnchantedAnvilState : AutoUIState
{
	public class GlyphButton : UIElement
	{
		private readonly int _itemType;

		public GlyphButton(int itemType)
		{
			_itemType = itemType;

			Width.Set(28, 0);
			Height.Set(28, 0);
		}

		public override void Update(GameTime gameTime)
		{
			if (IsMouseHovering)
			{
				if (Main.mouseLeft && Main.mouseLeftRelease)
				{

				}
			}
		}

		public override void Draw(SpriteBatch spriteBatch)
		{
			Texture2D texture = TextureAssets.Item[_itemType].Value;
			Vector2 center = GetDimensions().Center();

			if (IsMouseHovering)
			{
				DrawHelpers.DrawOutline(spriteBatch, texture, center, Color.White, (offset) =>
				{
					Texture2D outlineTexture = TextureColorCache.ColorSolid(texture, Color.White);
					Color outlineColor = ItemLoader.GetItem(_itemType) is GlyphItem glyphItem ? glyphItem.settings.Color : Color.White;

					spriteBatch.Draw(outlineTexture, center + offset, null, outlineColor, 0, texture.Size() / 2, 1, 0, 0);
				});
			}

			spriteBatch.Draw(texture, center, null, Color.White, 0, texture.Size() / 2, 1, 0, 0);
		}
	}

	private BasicItemSlot _slot;
	private float _animationProgress;
	private readonly List<GlyphButton> _glyphButtons = [];

	public override void OnInitialize()
	{
		Width = Height = StyleDimension.Fill;

		_slot = new(new Item());
		_slot.Left = new StyleDimension(-(_slot.Width.Pixels / 2), 0.5f);
		_slot.Top = new StyleDimension(-(_slot.Height.Pixels / 2), 0.5f);
		OverrideSamplerState = SamplerState.PointClamp;
		Append(_slot);
	}

	public override void Update(GameTime gameTime)
	{
		if (Main.LocalPlayer.controlInv || !Main.playerInventory)
			UISystem.SetInactive<EnchantedAnvilState>();

		if (_slot.Item.IsAir)
		{
			if (_glyphButtons.Count != 0) //Remove all buttons
				RemoveButtons();

			_animationProgress = 0;
		}
		else
		{
			if (_glyphButtons.Count == 0) //Add all buttons
				AddButtons();

			_animationProgress = MathHelper.Min(_animationProgress + 0.025f, 1);
		}

		int count = _glyphButtons.Count;
		for (int i = 0; i < count; i++)
		{
			GlyphButton button = _glyphButtons[i];
			Vector2 origin = button.GetDimensions().ToRectangle().Size() / 2;

			float rotation = MathHelper.PiOver2 * ((float)(i / (count - 1f)) - 0.5f);
			Vector2 targetPosition = _slot.GetDimensions().Center() + new Vector2(0, -100).RotatedBy(rotation);

			var lerpPosition = Vector2.Lerp(button.GetDimensions().Center(), targetPosition, EaseFunction.EaseCircularInOut.Ease(_animationProgress)) - origin; //Lerp to position
			button.Left.Set(lerpPosition.X, 0);
			button.Top.Set(lerpPosition.Y, 0);
		}

		base.Update(gameTime);
	}

	private void AddButtons()
	{
		CreateGlyphs(3, out int[] itemTypes);

		for (int i = 0; i < itemTypes.Length; i++)
		{
			GlyphButton button = new(itemTypes[i]);
			button.Left.Set(0, 0.5f);
			button.Top.Set(0, 0.5f);

			_glyphButtons.Add(button);
			Append(button);
		}
	}

	private void RemoveButtons()
	{
		for (int i = _glyphButtons.Count - 1; i >= 0; i--)
		{
			GlyphButton button = _glyphButtons[i];
			_glyphButtons.Remove(button);
			RemoveChild(button);
		}
	}

	private static void CreateGlyphs(int count, out int[] itemTypes)
	{
		List<GlyphItem> glyphItems = ModContent.GetContent<GlyphItem>().ToList();
		List<int> result = [];

		for (int c = 0; c < count; c++)
		{
			var choice = glyphItems[Main.rand.Next(glyphItems.Count)];

			result.Add(choice.Type);
			glyphItems.Remove(choice);

			if (glyphItems.Count == 0)
				break;
		}

		itemTypes = result.ToArray();
	}
}