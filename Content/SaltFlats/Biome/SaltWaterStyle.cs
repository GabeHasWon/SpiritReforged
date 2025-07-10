using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.RenderTargets;
using Terraria.Graphics;

namespace SpiritReforged.Content.SaltFlats.Biome;

public class SaltWaterStyle : ModWaterStyle
{
	public static readonly Asset<Texture2D> RainTexture = DrawHelpers.RequestLocal(typeof(SaltWaterStyle), "SaltRain", false);

	public static int StyleSlot { get; private set; }
	public override void SetStaticDefaults() => StyleSlot = Slot;

	#region visuals
	public static ModTarget2D WaterTarget { get; } = new(IsActive, DrawAndHandleWaterTarget);
	public static ModTarget2D OverlayTarget { get; } = new(IsActive, DrawOverlayTarget);

	
	private static Vector2 Origin;

	public static bool IsActive() => Main.waterStyle == StyleSlot;

	public override void Load()
	{
		WaterAlpha.OnWaterColor += ColorWater;
		DrawOverHandler.PostDrawTilesSolid += DrawShine;
	}

	private static void DrawAndHandleWaterTarget(SpriteBatch spriteBatch)
	{
		//spriteBatch.Draw(Main.instance.backWaterTarget, Main.sceneWallPos - Main.screenPosition, Color.White);
		spriteBatch.Draw(Main.waterTarget, Main.sceneWaterPos - Main.screenPosition, Color.White);
	}

	private static void DrawOverlayTarget(SpriteBatch spriteBatch)
	{
		const float scale = 2;

		var noise = AssetLoader.LoadedTextures["waterNoise"].Value;
		var screenPos = Main.screenPosition;

		float scroll = (float)Main.timeForVisualEffects / 4000f % 1;
		float opacity = 0.9f + (float)Math.Sin(Main.timeForVisualEffects / 100f) * 0.2f;

		for (int x = 0; x < Main.screenWidth / (noise.Width * scale) + 1; x++)
		{
			for (int y = 0; y < Main.screenHeight / (noise.Height * scale) + 1; y++)
			{
				var position = Origin + new Vector2(noise.Width * scale * (x - scroll), noise.Height * scale * (y - scroll));
				spriteBatch.Draw(noise, position - screenPos, null, (Color.White * opacity).Additive(), 0, Vector2.Zero, scale, default, 0);

				var position2 = Origin + new Vector2(noise.Width * scale * (x + scroll), noise.Height * scale * (y + scroll));
				spriteBatch.Draw(noise, position2 - screenPos, null, (Color.White * opacity).Additive(), 0, Vector2.Zero, scale, default, 0);
			}
		}
	}

	private static void DrawShine()
	{
		if (!IsActive() || OverlayTarget.Target is null || WaterTarget.Target is null)
			return;

		var s = AssetLoader.LoadedShaders["SimpleMultiply"];
		s.Parameters["tileTexture"].SetValue(WaterTarget);
		s.Parameters["lightness"].SetValue(3);

		Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, Main.Rasterizer, s, Main.GameViewMatrix.TransformationMatrix);

		Main.spriteBatch.Draw(OverlayTarget, Vector2.Zero, null, Color.White, 0, Vector2.Zero, 1, default, 0);
		Main.spriteBatch.End();
	}

	private static bool ColorWater(int x, int y, ref VertexColors colors, bool isPartial)
	{
		if (IsActive())
		{
			const int size = 400 * 2; //Relates to the draw dimensions of the noise texture

			Origin = new Vector2((int)(Main.screenPosition.X / size), (int)(Main.screenPosition.Y / size)) * size;
			Color tint = Color.LightPink;

			//Apply vertex colors
			colors.BottomLeftColor = colors.BottomLeftColor.MultiplyRGB(tint);
			colors.BottomRightColor = colors.BottomRightColor.MultiplyRGB(tint);

			if (Framing.GetTileSafely(x, y - 1).LiquidAmount == 0)
			{
				colors.TopLeftColor = colors.TopLeftColor.MultiplyRGB(tint) * 1.5f;
				colors.TopRightColor = colors.TopRightColor.MultiplyRGB(tint) * 1.5f;
			}
			else
			{
				colors.TopLeftColor = colors.TopLeftColor.MultiplyRGB(tint);
				colors.TopRightColor = colors.TopRightColor.MultiplyRGB(tint);
			}

			return true;
		}

		return false;
	}
	#endregion

	public override int ChooseWaterfallStyle() => ModContent.GetInstance<SaltWaterfallStyle>().Slot;
	public override int GetSplashDust() => DustID.Water;
	public override int GetDropletGore() => GoreID.WaterDrip;
	public override Asset<Texture2D> GetRainTexture() => RainTexture;
	public override Color BiomeHairColor() => Color.Pink;
}