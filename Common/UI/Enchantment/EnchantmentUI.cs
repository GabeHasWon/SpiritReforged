using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.UI.Misc;
using SpiritReforged.Common.UI.PotCatalogue;
using SpiritReforged.Common.UI.System;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Forest.Glyphs;
using SpiritReforged.Content.Underground.Tiles;
using System.Linq;
using Terraria.DataStructures;
using Terraria.UI;
using Terraria.Utilities;

namespace SpiritReforged.Common.UI.Enchantment;

public class EnchantmentUI : AutoUIState
{
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
		_slot.Width.Set(48, 0);
		_slot.Height.Set(48, 0);
		_slot.Left.Set(-(_slot.Width.Pixels / 2), 0.5f);
		_slot.Top.Set(-(_slot.Height.Pixels / 2), 0.5f);

		OverrideSamplerState = SamplerState.PointClamp;
		Append(_slot);
	}

	public override void Update(GameTime gameTime)
	{
		if (Main.LocalPlayer.controlInv || !Main.playerInventory)
			UISystem.SetInactive<EnchantmentUI>();

		if (ContainsPoint(Main.MouseScreen))
			Main.LocalPlayer.mouseInterface = true;

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

	public override void OnDeactivate()
	{
		if (!_slot.Item.IsAir)
		{
			IEntitySource source = new EntitySource_TileInteraction(Main.LocalPlayer, EnchantedWorkbench.TargetWorkbench.X, EnchantedWorkbench.TargetWorkbench.Y);

			Main.LocalPlayer.QuickSpawnItem(source, _slot.Item.Clone());
			_slot.Item.TurnToAir();
		}

		EnchantedWorkbench.TargetWorkbench = Point16.Zero;
	}

	public override void Draw(SpriteBatch spriteBatch)
	{
		Texture2D texture = Background.Value;
		Rectangle source = texture.Frame(2, 1, 0, 0, -2);
		Vector2 center = GetDimensions().Center();

		spriteBatch.Draw(texture, center, source, Color.White, 0, source.Size() / 2, 1, 0, 0);

		if (EnchantedWorkbench.TargetWorkbench != Point16.Zero)
		{
			for (int i = 0; i < 3; i++)
			{
				source = texture.Frame(2, 1, 1, 0, -2);
				spriteBatch.Draw(texture, center + Main.rand.NextVector2Circular(2, 2), source, Color.White.Additive(200), 0, source.Size() / 2, 1, 0, 0);
			}
		}

		string text = "Enchant";
		Vector2 dimensions = FontAssets.MouseText.Value.MeasureString(text);
		dimensions.Y *= 0.75f;

		CatalogueUI.DrawPanel(spriteBatch, new Rectangle((int)(center.X - dimensions.X / 2), (int)(center.Y - 40 - dimensions.Y / 2), (int)dimensions.X, (int)dimensions.Y), Color.Black * 0.5f);
		Utils.DrawBorderString(spriteBatch, "Enchant", center - new Vector2(0, 50), Main.MouseTextColorReal, 0.9f, 0.5f);

		base.Draw(spriteBatch);
	}

	private void AddButtons()
	{
		CreateGlyphs(3, out int[] itemTypes);

		for (int i = 0; i < itemTypes.Length; i++)
		{
			GlyphButton button = new(itemTypes[i]);
			float spacer = i - itemTypes.Length / 2;

			button.Left.Set(spacer * (button.Width.Pixels + 10) - button.Width.Pixels / 2, 0.5f);
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
			_slot.Item.SetGlyph(new(glyphItem.Type), new GlyphItem.ApplyContext(Main.LocalPlayer));
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
		var glyphItems = ModContent.GetContent<GlyphItem>().ToList();
		List<int> result = [];

		for (int c = 0; c < count; c++)
		{
			int random = new FastRandom(EnchantedWorkbench.TargetWorkbench.X + EnchantedWorkbench.TargetWorkbench.Y + c).Next(glyphItems.Count);
			var choice = glyphItems[random];

			result.Add(choice.Type);
			glyphItems.Remove(choice);

			if (glyphItems.Count == 0)
				break;
		}

		itemTypes = result.ToArray();
	}
}