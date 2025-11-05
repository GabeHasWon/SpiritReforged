using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.RenderTargets;

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

	public override void Load() => DrawOverHandler.PostDrawTilesSolid += DrawShine;

	private static void DrawAndHandleWaterTarget(SpriteBatch spriteBatch) => spriteBatch.Draw(Main.waterTarget, Main.sceneWaterPos - Main.screenPosition, Color.White);
	private static void DrawOverlayTarget(SpriteBatch spriteBatch)
	{
		float scroll = (float)Main.timeForVisualEffects / 4000f % 1;

		DrawCaustics(spriteBatch, ref Origin, new(2), Color.White * 0.4f, new Vector2(scroll));
		DrawCaustics(spriteBatch, ref Origin, new(2), Color.White * 0.4f, new Vector2(-scroll));
	}

	private static void DrawShine()
	{
		if (!IsActive() || OverlayTarget.Target is null || WaterTarget.Target is null)
			return;

		var s = AssetLoader.LoadedShaders["SimpleMultiply"].Value;
		s.Parameters["tileTexture"].SetValue(WaterTarget);
		s.Parameters["lightness"].SetValue(3);

		Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, Main.Rasterizer, s, Main.GameViewMatrix.TransformationMatrix);

		Main.spriteBatch.Draw(OverlayTarget, Vector2.Zero, null, Color.White, 0, Vector2.Zero, 1, default, 0);
		Main.spriteBatch.End();
	}

	/// <summary> Draws screen-wide causics with the given arguments. </summary>
	public static void DrawCaustics(SpriteBatch spriteBatch, ref Vector2 origin, Vector2 scale, Color color, Vector2 offset = default)
	{
		var noise = AssetLoader.LoadedTextures["waterNoise"].Value;
		var screenPos = Main.screenPosition;

		float width = noise.Width * scale.X;
		float height = noise.Height * scale.Y;

		for (int x = -1; x < Main.screenWidth / width + 1f; x++)
		{
			for (int y = -1; y < Main.screenHeight / height + 1f; y++)
			{
				var position2 = origin + new Vector2(noise.Width * scale.X * (x + offset.X), noise.Height * scale.Y * (y + offset.Y));
				spriteBatch.Draw(noise, position2 - screenPos, null, color.Additive(), 0, Vector2.Zero, scale, default, 0);
			}
		}

		origin = new Vector2((int)(Main.screenPosition.X / width), (int)(Main.screenPosition.Y / height)) * new Vector2(width, height);
	}
	#endregion

	public override int ChooseWaterfallStyle() => ModContent.GetInstance<SaltWaterfallStyle>().Slot;
	public override int GetSplashDust() => DustID.Water;
	public override int GetDropletGore() => GoreID.WaterDrip;
	public override Asset<Texture2D> GetRainTexture() => RainTexture;
	public override Color BiomeHairColor() => new(145, 145, 255);
}