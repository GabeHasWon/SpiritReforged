using Microsoft.Xna.Framework.Input;
using ReLogic.Graphics;
using SpiritReforged.Common.ConfigurationCommon;
using SpiritReforged.Common.WorldGeneration.Ecotones;
using Terraria.GameInput;
using Terraria.UI.Chat;

namespace SpiritReforged.Common.ModCompat.EcotoneMapper;

#nullable enable

internal class EcotoneMapperDisplay : ModSystem
{
	const float OffscreenXMin = 10f;
	const float OffscreenYMin = 10f;

	// Code heavily adapted from WorldGenPreviewer's map overlay code.
	// https://github.com/JavidPack/WorldGenPreviewer/blob/1.4/UIWorldLoadSpecial.cs
	internal static void DrawSelectionAreas()
	{
		if (!EcotoneMapperHooks.ActuallyManuallyMapping && !ModContent.GetInstance<ReforgedClientConfig>().DebugEcotones || Main.keyState.IsKeyDown(Keys.OemTilde))
			return;

		DynamicSpriteFont font = FontAssets.MouseText.Value;

		if (ModContent.GetInstance<ReforgedClientConfig>().DebugEcotones)
		{
			CenterText(font, "Mods.SpiritReforged.Generation.Mapping.Exit", Vector2.Zero);
			CenterText(font, "Mods.SpiritReforged.Generation.Mapping.Tilde", Vector2.UnitY * 24);

			string exitText = Language.GetTextValue("Mods.SpiritReforged.Generation.Mapping.Help");
			ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, exitText, new Vector2(20, 20), Color.White, 0f, Vector2.Zero, new(0.8f));
		}
		else if (EcotoneMapperHooks.MappingEcotone is { } mappingEcotone)
		{
			float width = ChatManager.GetStringSize(font, Language.GetTextValue("Mods.SpiritReforged.Generation.Mapping.Select", mappingEcotone.DisplayName.Value), Vector2.One).X;

			var position = new Vector2(Main.screenWidth / 2f - width / 2f - 12, 256);
			Utils.DrawInvBG(Main.spriteBatch, new Rectangle((int)position.X, (int)position.Y - 6, (int)width + 24, 124), new Color(23, 25, 81) * 0.925f * 0.85f);

			CenterText(font, "Mods.SpiritReforged.Generation.Mapping.Continue", Vector2.Zero, "", ClickContinue, 1.2f);
			CenterText(font, "Mods.SpiritReforged.Generation.Mapping.OrEscape", Vector2.UnitY * 26, "", null, 0.8f);
			CenterText(font, "Mods.SpiritReforged.Generation.Mapping.Select", Vector2.UnitY * 60, mappingEcotone.DisplayName.Value);
			CenterText(font, "Mods.SpiritReforged.Generation.Mapping.Tilde", Vector2.UnitY * 84, "", null, 0.8f);

			// Skip pause if the player hits escape, as mentioned above
			if (Main.keyState.IsKeyDown(Keys.Escape))
				EcotoneMapperHooks.ReadyToContinue = true;
		}

		PlayerInput.SetZoom_Unscaled();
		Main.spriteBatch.End();
		Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);

		float num20 = Main.mapFullscreenPos.X;
		float num21 = Main.mapFullscreenPos.Y;
		num20 *= Main.mapFullscreenScale;
		num21 *= Main.mapFullscreenScale;
		float panX = -num20 + Main.screenWidth / 2f;
		float panY = -num21 + Main.screenHeight / 2f;
		panX += OffscreenXMin * Main.mapFullscreenScale;
		panY += OffscreenYMin * Main.mapFullscreenScale;
		int entryId = 0;

		if (EcotoneSurfaceMapping.Entries.Count == 0)
		{
			EndBatch();
			return;
		}

		// Create a shallow copy so that threading doesn't add to the list while it's being rendered
		List<EcotoneSurfaceMapping.EcotoneEntry> entryCopy = new(EcotoneSurfaceMapping.Entries);

		foreach (var entry in entryCopy)
		{
			if (!EcotoneMapperHooks.ActuallyManuallyMapping)
				DrawDebug(font, panX, panY, entry);
			else
				DrawSelection(font, panX, panY, entry, ref entryId);

			entryId++;
		}

		EndBatch();
		return;

		static void EndBatch()
		{
			Main.spriteBatch.End();
			PlayerInput.SetZoom_UI();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
		}
	}

	private static void ClickContinue() => EcotoneMapperHooks.ReadyToContinue = true;

	private static void DrawSelection(DynamicSpriteFont font, float panX, float panY, EcotoneSurfaceMapping.EcotoneEntry entry, ref int entryId)
	{
		Rectangle drawRectangle = ModifyRectangle(OffscreenXMin, OffscreenYMin, panX, panY, entry.Bounds, true);

		bool hover = drawRectangle.Contains(Main.MouseScreen.ToPoint());
		bool hasEntry = EcotoneMapperHooks.ForcedEcotones.ContainsKey(entryId);
		float colorMul = hasEntry ? 0.5f : 1;
		Color backCol = hasEntry ? new Color(23, 81, 25) : new Color(23, 25, 81);
		bool invalid = EcotoneMapperHooks.MappingEcotone?.EcotoneEdgeBlocklist.Contains(entry.Definition.Name) is true;

		if (invalid)
		{
			hover = false;
			backCol = new Color(81, 23, 25);
		}

		if (hover)
		{
			colorMul *= 0.5f;

			if (Main.mouseLeft && Main.mouseLeftRelease)
			{
				Main.mouseLeftRelease = false;

				if (hasEntry)
					EcotoneMapperHooks.ForcedEcotones.Remove(entryId);
				else
					EcotoneMapperHooks.ForcedEcotones.Add(entryId, new EcotoneMapperHooks.EcotoneEntryPair(EcotoneMapperHooks.MappingEcotone, entry));
			}
		}

		Utils.DrawInvBG(Main.spriteBatch, drawRectangle, new Color((int)(backCol.R * colorMul), (int)(backCol.G * colorMul), (int)(backCol.B * colorMul), 255) * 0.925f * 0.85f);

		float scale = MathF.Min(MathF.Min(drawRectangle.Width - 8, drawRectangle.Height) / 70f, 1);
		var position = drawRectangle.Location.ToVector2() + new Vector2(20) * scale;

		// Draws either the entry name or invalid
		if (!hasEntry)
		{
			string text = entry.Definition.DisplayName.Value;
			float textScale = scale;

			if (invalid)
			{
				text = Language.GetTextValue("Mods.SpiritReforged.Generation.Mapping.Invalid", entry.Definition.DisplayName.Value);
				textScale = MathF.Min(drawRectangle.Width - 8, drawRectangle.Height) / ChatManager.GetStringSize(font, text, new(scale)).X;
			}

			ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, text, position, Color.White, 0f, Vector2.Zero, Vector2.One * textScale * scale);
		}
		else // Draws the entry name, crossed out, and the new name
		{
			string text = entry.Definition.DisplayName.Value;
			ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, text, position, Color.Gray, 0f, Vector2.Zero, Vector2.One * scale);

			float width = ChatManager.GetStringSize(font, text, Vector2.One).X * scale;
			Vector2 strikeOutPos = position + Vector2.UnitY * 8 * scale;
			Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, strikeOutPos - new Vector2(2), new Rectangle(0, 0, (int)width + 4, (int)(6 * scale)), Color.Black);
			Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, strikeOutPos, new Rectangle(0, 0, (int)width, (int)(2 * scale)), Color.Gray);

			position.X += width + 16;
			string actualName = EcotoneMapperHooks.MappingEcotone!.DisplayName.Value;
			ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, actualName, position, Color.White, 0f, Vector2.Zero, Vector2.One * scale);
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
		string text = entry.Left.Name + " < " + $"{entry.Definition.Name} < " + entry.Right.Name;
		Vector2 size = ChatManager.GetStringSize(font, text, Vector2.One);
		Vector2 stringPosition = new(drawRectangle.Center.X - size.X / 2f, drawRectangle.Y + 8);
		ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, text, stringPosition, Color.White, 0f, Vector2.Zero, Vector2.One);

		// Bounds information
		text = $"{entry.Width} x {entry.Height} [c/AAAAFF:({entry.StrictBounds.Height})]";
		ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, text, stringPosition + new Vector2(0, 24), Color.White, 0f, Vector2.Zero, new(0.8f));
	}

	private static string CenterText(DynamicSpriteFont font, string text, Vector2 offset, string textFormat = "", Action? onClick = null, float scale = 1f)
	{
		string exitText = Language.GetTextValue(text);

		if (textFormat != "")
			exitText = Language.GetTextValue(text, textFormat);

		Vector2 exitSize = ChatManager.GetStringSize(font, exitText, Vector2.One);
		var position = new Vector2(Main.screenWidth / 2f - exitSize.X * scale / 2, 256) + offset;
		Color color = Color.White;

		if (onClick is { } action && new Rectangle((int)position.X, (int)position.Y, (int)(exitSize.X * scale), (int)(exitSize.Y * scale)).Contains(Main.MouseScreen.ToPoint()))
		{
			color = Color.Gray;

			if (Main.mouseLeft && Main.mouseLeftRelease)
			{
				Main.mouseLeftRelease = false;

				action();
			}
		}

		ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, exitText, position, color, 0f, Vector2.Zero, new(scale));
		return exitText;
	}

	private static Rectangle ModifyRectangle(float offscreenXMin, float offscreenYMin, float panX, float num2, Rectangle bounds, bool addBuffer = false)
	{
		int baseY = bounds.Y;
		int baseWidth = bounds.Width;
		int baseHeight = bounds.Height;

		if (addBuffer)
		{
			int minHeight = 10;

			baseWidth++;
			baseY -= Math.Max(minHeight - baseHeight, 0);
			baseHeight = Math.Max(minHeight, baseHeight); 
		}

		int x = (int)((bounds.X - offscreenXMin) * Main.mapFullscreenScale + panX);
		int y = (int)((baseY - offscreenYMin) * Main.mapFullscreenScale + num2);
		int width = (int)(baseWidth * Main.mapFullscreenScale);
		int height = (int)(baseHeight * Main.mapFullscreenScale);
		return new Rectangle(x, y, width, height);
	}
}
