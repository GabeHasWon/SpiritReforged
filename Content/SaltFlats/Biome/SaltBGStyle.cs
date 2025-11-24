using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Content.SaltFlats.Biome;

public class SaltBGStyle : CustomSurfaceBackgroundStyle
{
	private static readonly Asset<Texture2D> FarMountains = ModContent.Request<Texture2D>("SpiritReforged/Assets/Textures/Backgrounds/SaltBackgroundFar_Mountains");
	private static readonly Asset<Texture2D> FarClouds = ModContent.Request<Texture2D>("SpiritReforged/Assets/Textures/Backgrounds/SaltBackgroundFar_Clouds");
	private static readonly Asset<Texture2D> SkyReflectionMask = ModContent.Request<Texture2D>("SpiritReforged/Assets/Textures/Backgrounds/SaltBackgroundFar_Mask");

	private static float MiddleOffset;

	public override int MiddleTexture => -1;
	public override int CloseTexture => -1;

	public override bool Draw(SpriteBatch spriteBatch, LayerType layer)
	{
		if (layer == LayerType.Far)
		{
			int slot = FarTexture;
			if (slot >= 0 && slot < TextureAssets.Background.Length)
			{
				Texture2D texture = LoadBackground(slot);
				Texture2D cloudTexture = FarClouds.Value;
				Texture2D mountainTexture = FarMountains.Value;
				Texture2D nightSkyMaskTexture = SkyReflectionMask.Value;

				Color color = BackgroundStyleHelper.SurfaceBackgroundModified;
				Rectangle bounds = GetBounds(slot);
				int loops = BackgroundStyleHelper.BackgroundLoops;

				float screenCenterY = Main.screenPosition.Y + Main.screenHeight / 2f;
				float dif = ((SaltFlatsSystem.SurfaceHeight == 0) ? (float)(Main.worldSurface * 0.67f) : SaltFlatsSystem.SurfaceHeight) * 16 - screenCenterY;
				BackgroundStyleHelper.BackgroundTopY = (int)(dif - dif * 0.8f);

				if (Main.gameMenu)
				{
					BackgroundStyleHelper.BackgroundTopY = 130;
				}

				DrawScroll((position, scale) => spriteBatch.Draw(cloudTexture, position + new Vector2(MiddleOffset, 0), bounds, color, 0, default, scale, SpriteEffects.None, 0), -1, loops + 1);
				DrawScroll((position, scale) => spriteBatch.Draw(texture, position, bounds, color, 0, Vector2.Zero, BackgroundStyleHelper.BackgroundScale, SpriteEffects.None, 0));
				DrawScroll((position, scale) => spriteBatch.Draw(cloudTexture, position + new Vector2(MiddleOffset, -670), bounds, color * 0.5f, 0, default, BackgroundStyleHelper.BackgroundScale, SpriteEffects.FlipVertically, 0), -1, loops + 1);
				DrawScroll((position, scale) => spriteBatch.Draw(mountainTexture, position, bounds, color, 0, Vector2.Zero, BackgroundStyleHelper.BackgroundScale, SpriteEffects.None, 0));

				if (SaltFlatsSystem.nightSkyOpacity > 0f)
				{
					Effect bgShader = AssetLoader.LoadedShaders["SaltFlatsSky"].Value;
					Matrix projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);
					bgShader.Parameters["texColorUVLerper"].SetValue(1f);
					bgShader.Parameters["WorldViewProjection"].SetValue(Main.BackgroundViewMatrix.TransformationMatrix * projection);
					bgShader.Parameters["viewMatrix"].SetValue(projection);
					SaltFlatsSystem.SetSkyColor(bgShader);
					RestartSpritebatch(bgShader);
					DrawScroll((position, scale) => spriteBatch.Draw(nightSkyMaskTexture, position, bounds, color * SaltFlatsSystem.nightSkyOpacity, 0, Vector2.Zero, BackgroundStyleHelper.BackgroundScale, SpriteEffects.None, 0));
					RestartSpritebatch(null);
				}

				if (!Main.gamePaused) //Move out of a drawing method
				{
					MiddleOffset += 0.4f * Main.windSpeedCurrent;
					MiddleOffset %= 2048; // Needs to be looped, otherwise the textures run out
				}
			}
		}

		return false;
	}
}