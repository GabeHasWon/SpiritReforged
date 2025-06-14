using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals.RenderTargets;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.Tiles.Amber;

/// <summary> Handles rendertarget visuals for <see cref="ShiningAmber"/>. </summary>
public class ShiningAmberVisuals : ILoadable
{
	public static bool CanDraw => Drawing = ReflectionPoints.Count != 0;
	public static bool Drawing { get; private set; }

	public static ModTarget2D TileTarget { get; } = new(() => CanDraw, DrawTileTarget);
	public static ModTarget2D OverlayTarget { get; } = new(() => CanDraw, DrawOverlayTarget);

	public static readonly HashSet<Point16> ReflectionPoints = [];

	public void Load(Mod mod)
	{
		DrawOverHandler.PostDrawTilesSolid += DrawShine;
		TileEvents.AddPreDrawAction(true, ReflectionPoints.Clear);
	}

	public void Unload() { }

	private static void DrawTileTarget(SpriteBatch spriteBatch)
	{
		foreach (var pt in ReflectionPoints)
			ShiningAmber.CustomDraw(pt.X, pt.Y, spriteBatch, true);
	}

	private static void DrawOverlayTarget(SpriteBatch spriteBatch)
	{
		const float scale = 4;

		var noise = TextureAssets.Extra[193].Value;
		float scroll = (float)Main.timeForVisualEffects / 4000f % 1;
		float opacity = (0.5f + (float)Math.Sin(Main.timeForVisualEffects / 100f) * 0.1f) * 0.25f;

		for (int x = 0; x < Main.screenWidth / (noise.Width * scale) + 1; x++)
		{
			for (int y = 0; y < Main.screenHeight / (noise.Height * scale) + 1; y++)
			{
				var position = new Vector2(noise.Width * scale * (x - scroll), noise.Height * scale * (y - scroll));
				spriteBatch.Draw(noise, position, null, (Color.Goldenrod * opacity).Additive(), 0, Vector2.Zero, scale, default, 0);
			}
		}
	}

	private static void DrawShine()
	{
		if (!Drawing || TileTarget.Target is null || OverlayTarget.Target is null)
			return;

		var s = AssetLoader.LoadedShaders["SimpleMultiply"];
		s.Parameters["tileTexture"].SetValue(TileTarget);

		Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, Main.Rasterizer, s, Main.GameViewMatrix.TransformationMatrix);

		Main.spriteBatch.Draw(OverlayTarget, Vector2.Zero, null, Color.White, 0, Vector2.Zero, 1, default, 0);
		Main.spriteBatch.End();

		Drawing = false;
	}
}