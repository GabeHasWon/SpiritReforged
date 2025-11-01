using SpiritReforged.Common.Visuals;
using Terraria.DataStructures;

namespace SpiritReforged.Content.SaltFlats.Biome;

public class SaltBGStyle : CustomSurfaceBackgroundStyle
{
	private static readonly Asset<Texture2D> FarMountains = ModContent.Request<Texture2D>("SpiritReforged/Assets/Textures/Backgrounds/SaltBackgroundFar_Mountains");
	private static readonly Asset<Texture2D> FarClouds = ModContent.Request<Texture2D>("SpiritReforged/Assets/Textures/Backgrounds/SaltBackgroundFar_Clouds");

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

				Color color = BackgroundStyleHelper.SurfaceBackgroundModified;
				Rectangle bounds = GetBounds(slot);
				int loops = BackgroundStyleHelper.BackgroundLoops;

				float screenCenterY = Main.screenPosition.Y + Main.screenHeight / 2f;
				float dif = ((SaltFlatsSystem.SurfaceHeight == 0) ? (float)(Main.worldSurface * 0.67f) : SaltFlatsSystem.SurfaceHeight) * 16 - screenCenterY;
				BackgroundStyleHelper.BackgroundTopY = (int)(dif - dif * 0.8f);

				DrawScroll((position, scale) => spriteBatch.Draw(cloudTexture, position + new Vector2(MiddleOffset, 0), bounds, color, 0, default, scale, SpriteEffects.None, 0), -1, loops + 1);
				DrawScroll((position, scale) => spriteBatch.Draw(texture, position, bounds, color, 0, Vector2.Zero, BackgroundStyleHelper.BackgroundScale, SpriteEffects.None, 0));
				DrawScroll((position, scale) => spriteBatch.Draw(cloudTexture, position + new Vector2(MiddleOffset, -670), bounds, color * 0.5f, 0, default, BackgroundStyleHelper.BackgroundScale, SpriteEffects.FlipVertically, 0), -1, loops + 1);
				DrawScroll((position, scale) => spriteBatch.Draw(mountainTexture, position, bounds, color, 0, Vector2.Zero, BackgroundStyleHelper.BackgroundScale, SpriteEffects.None, 0));

				if (!Main.gamePaused) //Move out of a drawing method
				{
					MiddleOffset += 0.4f * Main.windSpeedCurrent;
					MiddleOffset %= 2048; // Needs to be looped, otherwise the textures run out
				}
			}
		}

		return false;
	}

	/// <summary> Draws Cloud. </summary>
	/// <param name="cloud">The cloud to draw.</param>
	/// <param name="color">The color to draw the cloud in.</param>
	/// <param name="yOffset">The vertical offset of the cloud.</param>
	/// <param name="index">The index of the cloud in <see cref="Main.cloud"/> if applicable.</param>
	public static void DrawForegroudCloud(Cloud cloud, Color color, float yOffset, SpriteEffects effects = default, int index = -1)
	{
		Texture2D texture = TextureAssets.Cloud[cloud.type].Value;
		Vector2 position = new(cloud.position.X + texture.Width * 0.5f, yOffset + texture.Height * 0.5f);
		Rectangle sourceRectangle = new(0, 0, texture.Width, texture.Height);
		float rotation = cloud.rotation;
		Vector2 origin = texture.Size() / 2;
		float scale = cloud.scale;
		DrawData drawData = new(texture, position, sourceRectangle, color, rotation, origin, scale, effects);

		ModCloud modCloud = cloud.ModCloud;
		if (modCloud == null || index == -1 || modCloud.Draw(Main.spriteBatch, cloud, index, ref drawData))
		{
			drawData.Draw(Main.spriteBatch);
		}
	}
}