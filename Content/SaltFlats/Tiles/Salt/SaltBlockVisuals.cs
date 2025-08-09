using SpiritReforged.Common.ConfigurationCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.RenderTargets;
using System.Runtime.CompilerServices;
using Terraria.DataStructures;

namespace SpiritReforged.Content.SaltFlats.Tiles.Salt;

public class SaltBlockVisuals : ILoadable
{
	public static readonly Asset<Texture2D> GradientMap = ModContent.Request<Texture2D>(DrawHelpers.RequestLocal(typeof(SaltBlockVisuals), "GradientMap"));

	/// <summary> Whether reflection detail is high enough to calculate. </summary>
	public static bool Enabled => Lighting.NotRetro && Detail > 0;
	public static int Detail => ModContent.GetInstance<ReforgedClientConfig>().ReflectionDetail;

	public static bool Drawing { get; private set; }
	public static readonly HashSet<Point16> ReflectionPoints = [];

	/// <summary> Whether screen dimensions are beyond 1920x1080- bandaid fix for tiles not reflecting correctly on high resolutions. </summary>
	private static bool HighResolution;

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

		TargetSetup.OnResizeRendertargets += () => HighResolution = Main.screenWidth > 1920 || Main.screenHeight > 1080;
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

	private static void DrawMapTarget(SpriteBatch spriteBatch)
	{
		var gradient = GradientMap.Value;

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
				spriteBatch.Draw(gradient, new Vector2(i, j) * 16 - Main.screenPosition + new Vector2(0, 8), source, Color.White, 0, Vector2.Zero, 1, default, 0);
				continue;
			}
			else if (t.Slope is SlopeType.SlopeDownLeft or SlopeType.SlopeDownRight)
			{
				for (int x = 0; x < 8; x++)
				{
					Vector2 position = (t.Slope == SlopeType.SlopeDownLeft) ? new(2 * x, 2 * x) : new(2 * (7 - x), 2 * x);
					spriteBatch.Draw(gradient, new Vector2(i, j) * 16 - Main.screenPosition + position, source with { Width = 2 }, Color.White, 0, Vector2.Zero, 1, default, 0);
				}

				continue;
			}

			spriteBatch.Draw(gradient, new Vector2(i, j) * 16 - Main.screenPosition, source, Color.White, 0, Vector2.Zero, 1, default, 0);
		}
	}

	private static void DrawTileTarget(SpriteBatch spriteBatch)
	{
		var texture = TextureAssets.Tile[ModContent.TileType<SaltBlockReflective>()].Value;

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

	private static void DrawAndHandleReflectionTarget(SpriteBatch spriteBatch)
	{
		var gd = Main.graphics.GraphicsDevice;

		var storedZoom = Main.GameViewMatrix.Zoom;
		Main.GameViewMatrix.Zoom = Vector2.One;

		gd.SetRenderTarget(ReflectionTarget.Target);
		gd.Clear(Color.Transparent);

		//Draw the actual contents
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);

		if (Detail > 1)
		{
			Main.tileBatch.Begin();
			Main.instance.DrawSimpleSurfaceBackground(Main.screenPosition, Main.screenWidth, Main.screenHeight);
			Main.tileBatch.End();

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

		bool lowDetail = Detail == 1;
		var s = AssetLoader.LoadedShaders["Reflection"].Value;
		var n = AssetLoader.LoadedTextures["supPerlin"].Value;

		s.Parameters["mapTexture"].SetValue(MapTarget);
		s.Parameters["distortionTexture"].SetValue(n);
		s.Parameters["tileTexture"].SetValue(TileTarget);

		s.Parameters["reflectionHeight"].SetValue(ReflectionTarget.Target.Height / 4);
		s.Parameters["fade"].SetValue(lowDetail ? 10f : 3f);
		s.Parameters["distortionScale"].SetValue(new Vector2((float)n.Width / Main.screenWidth, (float)n.Height / Main.screenHeight));
		s.Parameters["distortionStrength"].SetValue(new Vector2(0.3f));
		s.Parameters["distortionPower"].SetValue(1);

		Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, s, Main.Transform);

		Color tint = lowDetail ? Color.Black * 0.5f : Main.ColorOfTheSkies.Additive(220) * 0.9f;
		Main.spriteBatch.Draw(ReflectionTarget, Vector2.Zero, null, tint, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
		Main.spriteBatch.End();

		Drawing = false;
	}

	public void Unload() { }
}