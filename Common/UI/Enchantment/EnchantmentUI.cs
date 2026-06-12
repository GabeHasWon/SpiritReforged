using SpiritReforged.Common.Easing;
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
using Terraria.GameContent.UI.Elements;
using Terraria.Graphics.Renderers;
using Terraria.UI;
using Terraria.Utilities;

namespace SpiritReforged.Common.UI.Enchantment;

public class EnchantmentUI : AutoUIState
{
	private static readonly Asset<Texture2D> Background = DrawHelpers.RequestLocal<EnchantmentUI>("EnchantmentUI_Background", false);
	private static readonly Asset<Texture2D> CreativeParticle = Main.Assets.Request<Texture2D>("Images/UI/Creative/Research_Spark");

	private static readonly Vector2[] FlameOrigin = [
			new(36, 10), 
			new(22, 24), 
			new(38, 34), 
			new(144, 12), 
			new(126, 22), 
			new(138, 26)
		];

	private readonly List<GlyphButton> _glyphButtons = [];

	private UIParticleLayer _particleLayer;
	private BasicItemSlot _slot;

	public override void OnInitialize()
	{
		Width = Height = new StyleDimension(200, 0);
		HAlign = 0.5f;
		VAlign = 0.4f;

		_slot = new(new Item(), ItemSlot.Context.CreativeSacrifice, 1);
		_slot.Width.Set(48, 0);
		_slot.Height.Set(48, 0);
		_slot.Left.Set(-(_slot.Width.Pixels / 2), 0.5f);
		_slot.Top.Set(-(_slot.Height.Pixels / 2), 0.5f);

		_particleLayer = new()
		{
			Width = new StyleDimension(0f, 1f),
			Height = new StyleDimension(0f, 1f)
		};

		OverrideSamplerState = SamplerState.PointClamp;
		Append(_slot);
		Append(_particleLayer);
	}

	public override void Update(GameTime gameTime)
	{
		if (Main.LocalPlayer.controlInv || !Main.playerInventory || _slot.Item.IsAir && !EnchantedWorkbench.HasCoords)
			UISystem.SetInactive<EnchantmentUI>(); //Automatically disable the UI if not in the inventory or an enchanted item is removed

		if (ContainsPoint(Main.MouseScreen))
			Main.LocalPlayer.mouseInterface = true;

		if (_slot.Item.IsAir || !EnchantedWorkbench.HasCoords)
		{
			if (_glyphButtons.Count != 0) //Remove all buttons
				RemoveButtons();
		}
		else
		{
			if (_glyphButtons.Count == 0) //Add all buttons
				AddButtons();
		}

		base.Update(gameTime);

		if (EnchantedWorkbench.HasCoords && Main.rand.NextBool(2))
		{
			Vector2 initialVelocity = Vector2.UnitY * -0.5f;
			_particleLayer.AddParticle(new CreativeSacrificeParticle(CreativeParticle, null, initialVelocity, FlameOrigin[Main.rand.Next(FlameOrigin.Length)] + new Vector2(13, -4))
			{
				AccelerationPerFrame = Vector2.UnitY * 0.01f,
				ScaleOffsetPerFrame = -1f / 90f
			});
		}

		_particleLayer.Update(gameTime);
	}

	public override void OnDeactivate()
	{
		if (!_slot.Item.IsAir)
		{
			IEntitySource source = new EntitySource_TileInteraction(Main.LocalPlayer, EnchantedWorkbench.ActiveCoordinates.X, EnchantedWorkbench.ActiveCoordinates.Y);

			Main.LocalPlayer.QuickSpawnItem(source, _slot.Item.Clone());
			_slot.Item.TurnToAir();
		}

		RemoveButtons();
		EnchantedWorkbench.RemoveCoords();
	}

	public override void Draw(SpriteBatch spriteBatch)
	{
		Texture2D texture = Background.Value;
		Rectangle source = texture.Frame(2, 1, 0, 0, -2);
		Vector2 center = GetDimensions().Center();

		spriteBatch.Draw(texture, center, source, Color.White, 0, source.Size() / 2, 1, 0, 0);

		if (EnchantedWorkbench.HasCoords)
		{
			for (int i = 0; i < 3; i++)
			{
				source = texture.Frame(2, 1, 1, 0, -2);
				float squash = EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects / (10f + i) % 1);

				spriteBatch.Draw(texture, center + (Vector2.UnitX * (1f + squash * 0.2f)).RotatedBy(Main.timeForVisualEffects / (2f * (i + 1))), source, Color.White.Additive(200), 0, source.Size() / 2, new Vector2(1f + squash * 0.02f, 1f - squash * 0.02f), 0, 0);
			}

			string text = Language.GetTextValue("Mods.SpiritReforged.Misc.Enchantment.Enchant");
			Vector2 dimensions = FontAssets.MouseText.Value.MeasureString(text);
			dimensions.Y *= 0.75f;

			//Draw textbox
			CatalogueUI.DrawPanel(spriteBatch, new Rectangle((int)(center.X - dimensions.X / 2), (int)(center.Y - 40 - dimensions.Y / 2), (int)dimensions.X, (int)dimensions.Y), Color.Black * 0.5f);
			Utils.DrawBorderString(spriteBatch, text, center - new Vector2(0, 50), Main.MouseTextColorReal, 0.9f, 0.5f);
		}

		base.Draw(spriteBatch);

		_particleLayer.Draw(spriteBatch);
	}

	private void AddButtons()
	{
		CreateGlyphs(_slot.Item, 3, out int[] itemTypes);

		for (int i = 0; i < itemTypes.Length; i++)
		{
			GlyphButton button = new(itemTypes[i], true);
			float spacer = i - itemTypes.Length / 2;

			button.Left.Set(spacer * (button.Width.Pixels + 10) - button.Width.Pixels / 2, 0.5f);
			button.Top.Set(50, 0.5f);
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
			if (_slot.Item.SetGlyph(new(glyphItem.Type), new GlyphItem.ApplyContext(Main.LocalPlayer)) && _slot.Item.TryGetGlobalItem(out GlyphItem.GlyphGlobalItem glyphGlobalItem))
				glyphGlobalItem.StartAnimation();

			if (EnchantedWorkbench.HasCoords)
			{
				Point16 target = EnchantedWorkbench.ActiveCoordinates;

				EnchantedWorkbench.Deactivate(target.X, target.Y);
				EnchantedWorkbench.RemoveCoords();
			}
		}
	}

	private static void CreateGlyphs(Item forItem, int count, out int[] itemTypes)
	{
		IEnumerable<GlyphItem> glyphItems = ModContent.GetContent<GlyphItem>();
		int itemsCount = glyphItems.Count();

		GlyphItem[] pool = glyphItems.OrderBy(SelectRandomOrder).ToArray(); //The total range of possible picks
		List<int> result = [];

		for (int c = 0; c < pool.Length; c++)
		{
			GlyphItem choice = pool[c];
			if (choice.CanApplyGlyph(forItem))
			{
				result.Add(choice.Type);

				if (result.Count >= count)
					break;
			}
		}

		itemTypes = result.ToArray();

		static float SelectRandomOrder(GlyphItem glyphItem) => new FastRandom(glyphItem.Type * EnchantedWorkbench.ActiveCoordinates.X * EnchantedWorkbench.ActiveCoordinates.Y).NextFloat();
	}
}