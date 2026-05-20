using Microsoft.Xna.Framework.Input;
using ReLogic.Graphics;
using SpiritReforged.Common.ConfigurationCommon;
using SpiritReforged.Common.WorldGeneration.Ecotones;
using Terraria.GameInput;
using Terraria.UI.Chat;

namespace SpiritReforged.Common.ModCompat.EcotoneMapper;

internal class EcotoneMapperDisplay : ModSystem
{
	const float OffscreenXMin = 10f;
	const float OffscreenYMin = 10f;

	public override void PreWorldGen() => EcotoneMapperHooks.ActuallyManuallyMapping = false;

	// Code heavily adapted from WorldGenPreviewer.
	// https://github.com/JavidPack/WorldGenPreviewer/blob/1.4/UIWorldLoadSpecial.cs
	internal static void DrawSelectionAreas()
	{
		if (!EcotoneMapperHooks.ActuallyManuallyMapping && !ModContent.GetInstance<ReforgedClientConfig>().DebugEcotones || Main.keyState.IsKeyDown(Keys.OemTilde))
			return;

		DynamicSpriteFont font = FontAssets.MouseText.Value;
		CenterText(font, "Mods.SpiritReforged.Generation.Mapping.Exit", Vector2.Zero);
		CenterText(font, "Mods.SpiritReforged.Generation.Mapping.Tilde", Vector2.UnitY * 24);

		string exitText = Language.GetTextValue("Mods.SpiritReforged.Generation.Mapping.Help");
		ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, exitText, new Vector2(20, 20), Color.White, 0f, Vector2.Zero, new(0.8f));

		PlayerInput.SetZoom_Unscaled();
		Main.spriteBatch.End();
		Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);

		float num20 = Main.mapFullscreenPos.X;// does it zoom into cursor or center.
		float num21 = Main.mapFullscreenPos.Y;
		num20 *= Main.mapFullscreenScale;
		num21 *= Main.mapFullscreenScale;
		float panX = -num20 + Main.screenWidth / 2f;
		float num2 = -num21 + Main.screenHeight / 2f;
		panX += OffscreenXMin * Main.mapFullscreenScale;
		num2 += OffscreenYMin * Main.mapFullscreenScale;

		foreach (var entry in EcotoneSurfaceMapping.Entries)
		{
			if (!EcotoneMapperHooks.ActuallyManuallyMapping)
				DrawDebug(font, panX, num2, entry);
			else
				DrawSelection(font, panX, num2, entry);
		}

		Main.spriteBatch.End();
		PlayerInput.SetZoom_UI();
		Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
	}

	private static void DrawSelection(DynamicSpriteFont font, float panX, float num2, EcotoneSurfaceMapping.EcotoneEntry entry)
	{
		Rectangle drawRectangle = ModifyRectangle(OffscreenXMin, OffscreenYMin, panX, num2, entry.Bounds);
		Utils.DrawInvBG(Main.spriteBatch, drawRectangle, new Color(23, 25, 81, 255) * 0.925f * 0.85f);

		var position = drawRectangle.Location.ToVector2() + new Vector2(12);
		ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, entry.Definition.Name, position, Color.White, 0f, Vector2.Zero, Vector2.One);

		foreach (EcotoneBase ecotone in EcotoneBase.Ecotones)
		{

		}
	}

	private static void DrawDebug(DynamicSpriteFont font, float panX, float num2, EcotoneSurfaceMapping.EcotoneEntry entry)
	{
		const int DebugHeight = 56;

		Rectangle drawRectangle = ModifyRectangle(OffscreenXMin, OffscreenYMin, panX, num2, entry.StrictBounds);
		Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, drawRectangle, Color.Blue * 0.6f);

		drawRectangle = ModifyRectangle(OffscreenXMin, OffscreenYMin, panX, num2, entry.Bounds);
		Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, drawRectangle, Color.Red * 0.4f);

		drawRectangle.Y -= DebugHeight;
		drawRectangle.Height = DebugHeight;
		Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, drawRectangle, Color.White * 0.6f);

		// Corruption, if any + header
		string corruptionType = entry.CorruptionType == BiomeConversionID.Purity ? "" : $" ({GetConversionName(entry.CorruptionType)})";
		string text = entry.Left.Name + " < " + $"{entry.Definition.Name}{corruptionType} < " + entry.Right.Name;
		Vector2 size = ChatManager.GetStringSize(font, text, Vector2.One);
		Vector2 stringPosition = new(drawRectangle.Center.X - size.X / 2f, drawRectangle.Y + 8);
		ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, text, stringPosition, Color.White, 0f, Vector2.Zero, Vector2.One);

		// Bounds information
		text = $"{entry.Width} x {entry.Height} [c/AAAAFF:({entry.StrictBounds.Height})]";
		ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, text, stringPosition + new Vector2(0, 24), Color.White, 0f, Vector2.Zero, new(0.8f));
	}

	private static string GetConversionName(int corruptionType)
	{
		if (corruptionType >= BiomeConversionID.Count)
			return BiomeConversionLoader.GetBiomeConversion(corruptionType).Name;

		return corruptionType switch
		{
			BiomeConversionID.Purity => "Purity",
			BiomeConversionID.Corruption => "Corruption",
			BiomeConversionID.Hallow => "Hallow",
			BiomeConversionID.GlowingMushroom => "GlowingMushroom",
			BiomeConversionID.Crimson => "Crimson",
			BiomeConversionID.Sand => "Sand",
			BiomeConversionID.Snow => "Snow",
			BiomeConversionID.Dirt => "Dirt",
			BiomeConversionID.PurificationPowder => "PurificationPowder",
			BiomeConversionID.Chlorophyte => "Chlorophyte",
			_ => throw new InvalidCastException("No conversion ID of type " + corruptionType + " exists.")
		};
	}

	private static string CenterText(DynamicSpriteFont font, string text, Vector2 offset)
	{
		string exitText = Language.GetTextValue(text);
		Vector2 exitSize = ChatManager.GetStringSize(font, exitText, Vector2.One);
		var position = new Vector2(Main.screenWidth / 2f - exitSize.X / 2, 256) + offset;
		ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, exitText, position, Color.White, 0f, Vector2.Zero, Vector2.One);
		return exitText;
	}

	private static Rectangle ModifyRectangle(float offscreenXMin, float offscreenYMin, float panX, float num2, Rectangle bounds)
	{
		int x = (int)((bounds.X - offscreenXMin) * Main.mapFullscreenScale + panX);
		int y = (int)((bounds.Y - offscreenYMin) * Main.mapFullscreenScale + num2);
		int width = (int)(bounds.Width * Main.mapFullscreenScale);
		int height = (int)(bounds.Height * Main.mapFullscreenScale);
		return new Rectangle(x, y, width, height);
	}
}
