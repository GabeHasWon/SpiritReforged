using SpiritReforged.Common.ConfigurationCommon;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.RenderTargets;
using System.Runtime.CompilerServices;
using Terraria.DataStructures;

namespace SpiritReforged.Content.SaltFlats.Tiles.Salt;

public class SaltBlockVisuals : ILoadable
{
	private static class Gradient
	{
		private static Texture2D DistanceMap;

		/// <summary> Gets a gradient texture for shader mapping. </summary>
		/// <param name="width"> The pre-upscaled width of the texture. </param>
		/// <param name="height"> The pre-upscaled height of the texture.</param>
		public static Texture2D CreateTilemap(int width, int height)
		{
			if (DistanceMap != null)
				return DistanceMap;

			const int taper = 2; //Opacity taper downscaled

			var data = new Color[width * height];
			for (int i = 0; i < data.Length; i++)
			{
				int y = i / width;

				float pixelStrength = 1f - (float)y / 255; //Divide by a full band rather than 'height' to avoid distorting the reflection Y
				float fadeStrength = 1f - (float)y / height;
				float taperOpacity = Math.Min((float)y / taper, 1);

				data[i] = new Color(0, pixelStrength, EaseFunction.EaseCubicOut.Ease(Math.Clamp(fadeStrength * 1.7f, 0, 1)) * taperOpacity); //Green: reflected pixels - Blue: static opacity
			}

			var textureToCache = new Texture2D(Main.graphics.GraphicsDevice, width, height);
			textureToCache.SetData(data);

			return DistanceMap = textureToCache;
		}
	}

	/// <summary> Whether reflection detail is high enough to calculate. </summary>
	public static bool Enabled => Lighting.NotRetro && Detail > 0;
	public static int Detail => ModContent.GetInstance<ReforgedClientConfig>().ReflectionDetail;

	public static bool Drawing { get; private set; }
	public static readonly HashSet<Point16> ReflectionPoints = [];

	/// <summary> Whether screen dimensions are beyond 1920x1080.<br/>Included in a bandaid fix for tiles not reflecting correctly on high resolutions. </summary>
	public static bool HighResolution { get; private set; }

	public static readonly Asset<Texture2D> TileMap = DrawHelpers.RequestLocal(typeof(SaltBlockVisuals), "SaltBlockReflectiveMap", false);
	public static readonly Asset<Texture2D> Noise = Main.Assets.Request<Texture2D>("Images/Misc/noise");

	public static ModTarget2D MapTarget { get; } = new(CanDraw, DrawMapTarget);
	public static ModTarget2D TileTarget { get; } = new(CanDraw, DrawTileTarget);
	public static ModTarget2D ReflectionTarget { get; } = new(CanDraw, DrawAndHandleReflectionTarget, false);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawPlayers_AfterProjectiles")]
	private static extern void DrawPlayers_AfterProjectiles(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawPlayers_BehindNPCs")]
	private static extern void DrawPlayers_BehindNPCs(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawProjectiles")]
	private static extern void DrawProjectiles(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawDust")]
	private static extern void DrawDust(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawGore")]
	private static extern void DrawGore(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawNPCs")]
	private static extern void DrawNPCs(Main main, bool behindTiles = false);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawBG")]
	private static extern void DrawBG(Main main);

	public void Load(Mod mod)
	{
		DrawOverHandler.PostDrawTilesSolid += DrawFullReflection;
		TileEvents.AddPreDrawAction(true, ReflectionPoints.Clear);

		TargetSetup.OnResizeRendertargets += static () => HighResolution = Main.graphics.GraphicsDevice.Viewport.Width > 1920 || Main.graphics.GraphicsDevice.Viewport.Height > 1080;
	}

	private static bool CanDraw()
	{
		if (ReflectionPoints.Count > 0)
		{
			Drawing = true;
			return true;
		}

		return false;
	}

	/// <summary> Draws gradients at <see cref="ReflectionPoints"/> into <see cref="MapTarget"/>, which tells our shader which pixels to reflect and at what opacity. </summary>
	private static void DrawMapTarget(SpriteBatch spriteBatch)
	{
		const float scale = 2;
		var gradient = Gradient.CreateTilemap(8, 180);

		foreach (var pt in ReflectionPoints)
		{
			int i = pt.X;
			int j = pt.Y;

			if (ReflectionPoints.Contains(new(i, j - 1)))
				continue;

			Rectangle source = new(0, 0, gradient.Width, gradient.Height);
			Tile t = Main.tile[i, j];

			if (t.IsHalfBlock)
			{
				spriteBatch.Draw(gradient, new Vector2(i, j) * 16 - Main.screenPosition + new Vector2(0, 8), source, Color.White, 0, Vector2.Zero, scale, default, 0);
				continue;
			}
			else if (t.Slope is SlopeType.SlopeDownLeft or SlopeType.SlopeDownRight)
			{
				for (int x = 0; x < 8; x++)
				{
					var position = (t.Slope == SlopeType.SlopeDownLeft) ? new Vector2(x, x) * 2 : new Vector2(6 - x, x) * 2;
					spriteBatch.Draw(gradient, new Vector2(i, j) * 16 - Main.screenPosition + position, source with { Width = 2 }, Color.White, 0, Vector2.Zero, scale, default, 0);
				}

				continue;
			}

			spriteBatch.Draw(gradient, new Vector2(i, j) * 16 - Main.screenPosition, source, Color.White, 0, Vector2.Zero, scale, default, 0);
		}
	}

	/// <summary> Draws the tile textures that we want to appear reflective into <see cref="TileTarget"/>. </summary>
	private static void DrawTileTarget(SpriteBatch spriteBatch)
	{
		var texture = TileMap.Value;

		foreach (var pt in ReflectionPoints)
		{
			int i = pt.X;
			int j = pt.Y;

			var t = Main.tile[i, j];
			var source = new Rectangle(t.TileFrameX, t.TileFrameY, 16, 16);

			if (t.Slope != SlopeType.Solid || t.IsHalfBlock)
			{
				TileExtensions.DrawSloped(i, j, texture, Color.White, Vector2.Zero);
				continue;
			}

			spriteBatch.Draw(texture, new Vector2(i, j) * 16 - Main.screenPosition, source, Color.White, 0, Vector2.Zero, 1, default, 0);
		}
	}

	/// <summary> Draws the objects that we want to reflect into <see cref="ReflectionTarget"/>. </summary>
	private static void DrawAndHandleReflectionTarget(SpriteBatch spriteBatch)
	{
		var gd = Main.graphics.GraphicsDevice;
		var storedZoom = Main.GameViewMatrix.Zoom;
		Main.GameViewMatrix.Zoom = Vector2.One;

		gd.SetRenderTarget(ReflectionTarget.Target);
		gd.Clear(Color.Transparent);

		//Draw the actual contents
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);

		Main.tileBatch.Begin();
		Main.instance.DrawSimpleSurfaceBackground(Main.screenPosition, Main.screenWidth, Main.screenHeight);
		Main.tileBatch.End();

		if (Detail > 1)
		{
			DrawBG(Main.instance);

			if (!HighResolution)
				spriteBatch.Draw(Main.instance.wallTarget, Main.sceneWallPos - Main.screenPosition, Color.White);

			DrawNPCs(Main.instance, true);

			if (!HighResolution)
			{
				spriteBatch.Draw(Main.instance.tileTarget, Main.sceneTilePos - Main.screenPosition, Color.White);
				spriteBatch.Draw(Main.instance.tile2Target, Main.sceneTile2Pos - Main.screenPosition, Color.White);
			}
		}

		DrawNPCs(Main.instance);

		if (Detail > 2)
		{
			DrawGore(Main.instance);
			Main.instance.DrawItems();
		}

		spriteBatch.End();

		if (Detail > 2)
		{
			DrawDust(Main.instance);
			DrawProjectiles(Main.instance);
		}

		DrawPlayers_BehindNPCs(Main.instance);
		DrawPlayers_AfterProjectiles(Main.instance);

		if (Detail > 1)
		{
			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
			spriteBatch.Draw(Main.waterTarget, Main.sceneWaterPos - Main.screenPosition, Color.White);
			spriteBatch.End();
		}

		gd.SetRenderTarget(null);

		Main.GameViewMatrix.Zoom = storedZoom;
	}

	private static void DrawFullReflection()
	{
		if (!Drawing || ReflectionTarget.Target is null || MapTarget.Target is null || TileTarget.Target is null)
			return;

		var s = AssetLoader.LoadedShaders["Reflection"].Value;
		var n = Noise.Value;

		s.Parameters["mapTexture"].SetValue(MapTarget);
		s.Parameters["distortionTexture"].SetValue(n);
		s.Parameters["tileTexture"].SetValue(TileTarget);

		s.Parameters["reflectionHeight"].SetValue(ReflectionTarget.Target.Height / 4);
		s.Parameters["fade"].SetValue(3f);
		s.Parameters["distortionScale"].SetValue(new Vector2((float)n.Width / Main.screenWidth, (float)n.Height / Main.screenHeight));
		s.Parameters["distortionStrength"].SetValue(new Vector2(0.3f));
		s.Parameters["distortionPower"].SetValue(1);

		Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, s, Main.Transform);

		Color tint = Color.White.Additive(230) * 0.9f;
		Main.spriteBatch.Draw(ReflectionTarget, Vector2.Zero, null, tint, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
		Main.spriteBatch.End();

		Drawing = false;
	}

	public void Unload() { }
}