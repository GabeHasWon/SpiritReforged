using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals.RenderTargets;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.Tiles.Amber;

public partial class PolishedAmber : ModTile, IAutoloadTileItem
{
	public static ModTarget2D TileTarget { get; } = new(() => ReflectionPoints.Count != 0, DrawTileTarget);
	public static ModTarget2D OverlayTarget { get; } = new(() => ReflectionPoints.Count != 0, DrawOverlayTarget);

	public static readonly HashSet<Point16> ReflectionPoints = [];

	private static void DrawTileTarget(SpriteBatch spriteBatch)
	{
		int type = ModContent.TileType<PolishedAmber>();
		var texture = TextureAssets.Tile[type].Value;

		foreach (var pt in ReflectionPoints)
		{
			var color = Color.White;
			var t = Framing.GetTileSafely(pt);
			var source = new Rectangle(t.TileFrameX, t.TileFrameY, 16, 16);

			spriteBatch.Draw(texture, new Vector2(pt.X, pt.Y) * 16 - Main.screenPosition, source, color, 0, Vector2.Zero, 1, default, 0);
		}
	}

	private static void DrawOverlayTarget(SpriteBatch spriteBatch)
	{
		const float scale = 4;
		var noise = TextureAssets.Extra[193].Value;
		float scroll = (float)Main.timeForVisualEffects / 4000f % 1;
		float opacity = (0.5f + (float)Math.Sin(Main.timeForVisualEffects / 100f) * 0.1f) * 0.25f;

		for (int x = 0; x < 3; x++)
		{
			for (int y = 0; y < 3; y++)
			{
				var position = new Vector2(noise.Width * scale * (x - scroll), noise.Height * scale * (y - scroll));
				spriteBatch.Draw(noise, position, null, (Color.Goldenrod * opacity).Additive(), 0, Vector2.Zero, scale, default, 0);
			}
		}
	}

	public override void Load()
	{
		DrawOverHandler.PostDrawTilesSolid += DrawShine;
		TileEvents.PreDrawAction(true, ReflectionPoints.Clear);
	}

	private static void DrawShine()
	{
		if (TileTarget.Target is null || OverlayTarget.Target is null)
			return;

		var s = AssetLoader.LoadedShaders["SimpleMultiply"];
		s.Parameters["tileTexture"].SetValue(TileTarget);

		Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, Main.Rasterizer, s, Main.GameViewMatrix.TransformationMatrix);

		Main.spriteBatch.Draw(OverlayTarget, Vector2.Zero, null, Color.White, 0, Vector2.Zero, 1, default, 0);
		Main.spriteBatch.End();
	}
}