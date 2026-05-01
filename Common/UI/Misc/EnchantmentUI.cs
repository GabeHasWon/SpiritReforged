using SpiritReforged.Common.Particle;
using SpiritReforged.Common.UI.System;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Forest.Glyphs;
using SpiritReforged.Content.Underground.Tiles;
using System.Linq;
using Terraria.DataStructures;
using Terraria.Graphics.Renderers;
using Terraria.UI;

namespace SpiritReforged.Common.UI.Misc;

public class EnchantmentUI : AutoUIState
{
	public class GlyphButton : UIElement
	{
		public readonly int itemType;
		private float _hoverTime;

		public GlyphButton(int itemType)
		{
			this.itemType = itemType;

			Width.Set(32, 0);
			Height.Set(32, 0);
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			if (IsMouseHovering)
			{
				_hoverTime = Math.Min(_hoverTime + 0.1f, 1);
			}
			else
			{
				_hoverTime = Math.Max(_hoverTime - 0.1f, 0);
			}
		}

		public override void Draw(SpriteBatch spriteBatch)
		{
			Texture2D texture = TextureAssets.Item[itemType].Value;
			Vector2 center = GetDimensions().Center() - new Vector2(0, _hoverTime * 4);

			if (IsMouseHovering)
			{
				Texture2D outlineTexture = TextureColorCache.ColorSolid(texture, Color.White);
				Color outlineColor = ItemLoader.GetItem(itemType) is GlyphItem glyphItem ? glyphItem.settings.Color : Color.White;

				DrawHelpers.DrawOutline(spriteBatch, texture, center, Color.White, (offset) =>
					spriteBatch.Draw(outlineTexture, center + offset, null, outlineColor, 0, texture.Size() / 2, 1, 0, 0));

				if ((int)Main.timeForVisualEffects % 80 == 0 || Main.rand.NextBool(120))
				{
					Vector2 velocity = Main.rand.NextVector2Circular(0.5f, 0.5f);

					TerrariaParticles.OverInventory.Add(new PrettySparkleParticle()
					{
						LocalPosition = Main.rand.NextVector2FromRectangle(GetDimensions().ToRectangle()),
						Scale = new Vector2(Main.rand.NextFloat(0.25f, 0.6f)),
						ColorTint = outlineColor,
						Velocity = velocity,
						AccelerationPerFrame = -(velocity * 0.01f),
						TimeToLive = 120
					});
				}
			}

			spriteBatch.Draw(texture, center, null, Color.White, 0, texture.Size() / 2, 1, 0, 0);
		}
	}

	private static readonly Asset<Texture2D> Background = DrawHelpers.RequestLocal<EnchantmentUI>("EnchantmentUI_Background", false);

	private readonly List<GlyphButton> _glyphButtons = [];
	private BasicItemSlot _slot;
	private float _animationProgress;

	public override void OnInitialize()
	{
		Width = Height = new StyleDimension(200, 0);
		HAlign = 0.5f;
		VAlign = 0.6f;

		_slot = new(new Item(), ItemSlot.Context.CreativeSacrifice);
		_slot.Left = new StyleDimension(-(_slot.Width.Pixels / 2), 0.5f);
		_slot.Top = new StyleDimension(-(_slot.Height.Pixels / 2), 0.5f);

		OverrideSamplerState = SamplerState.PointClamp;
		Append(_slot);
	}

	public override void Update(GameTime gameTime)
	{
		if (Main.LocalPlayer.controlInv || !Main.playerInventory)
			UISystem.SetInactive<EnchantmentUI>();

		if (_slot.Item.IsAir || EnchantedWorkbench.TargetWorkbench == Point16.Zero)
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

		base.Update(gameTime);
	}

	public override void Draw(SpriteBatch spriteBatch)
	{
		Texture2D texture = Background.Value;
		Vector2 center = GetDimensions().Center();

		spriteBatch.Draw(texture, center + new Vector2(0, 40), null, Color.White, 0, texture.Size() / 2, 1, 0, 0);
		Utils.DrawBorderString(spriteBatch, "Enchant", center + new Vector2(0, 86), Main.MouseTextColorReal, 0.9f, 0.5f);

		base.Draw(spriteBatch);
	}

	private void AddButtons()
	{
		CreateGlyphs(3, out int[] itemTypes);

		for (int i = 0; i < itemTypes.Length; i++)
		{
			GlyphButton button = new(itemTypes[i]);
			button.Left.Set((button.Width.Pixels + 8) * (i - itemTypes.Length * 0.5f), 0.5f);
			button.Top.Set(40, 0.5f);
			button.OnLeftClick += OnClickButton;

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

	private void OnClickButton(UIMouseEvent evt, UIElement listeningElement)
	{
		if (ItemLoader.GetItem((listeningElement as GlyphButton).itemType) is GlyphItem glyphItem)
		{
			glyphItem.ApplyGlyph(_slot.Item, new GlyphItem.ApplyContext(Main.LocalPlayer));
			Point16 target = EnchantedWorkbench.TargetWorkbench;

			if (target != Point16.Zero)
			{
				EnchantedWorkbench.Deactivate(target.X, target.Y);
				EnchantedWorkbench.TargetWorkbench = Point16.Zero;
			}
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