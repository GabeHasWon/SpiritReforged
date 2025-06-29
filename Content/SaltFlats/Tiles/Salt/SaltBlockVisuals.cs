using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.RenderTargets;
using System.Runtime.CompilerServices;
using Terraria.DataStructures;
using Terraria.Graphics;

namespace SpiritReforged.Content.SaltFlats.Tiles.Salt;

public class SaltBlockVisuals : ILoadable
{
	public static readonly Asset<Texture2D> GradientMap = ModContent.Request<Texture2D>(DrawHelpers.RequestLocal(typeof(SaltBlockVisuals), "GradientMap"));

	public static bool Drawing { get; private set; }
	public static readonly HashSet<Point16> ReflectionPoints = [];

	public static ModTarget2D MapTarget { get; } = new(CanDraw, DrawMapTarget);
	public static ModTarget2D TileTarget { get; } = new(CanDraw, DrawTileTarget);
	public static ModTarget2D ReflectionTarget { get; } = new(CanDraw, DrawAndHandleReflectionTarget, false);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawPlayers_AfterProjectiles")]
	private static extern void DrawPlayers_AfterProjectiles(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawNPCs")]
	private static extern void DrawNPCs(Main main, bool behindTiles = false);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawBG")]
	private static extern void DrawBG(Main main);

	public void Load(Mod mod)
	{
		DrawOverHandler.PostDrawTilesSolid += DrawFullReflection;
		TileEvents.AddPreDrawAction(true, ReflectionPoints.Clear);
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
		var texture = GradientMap.Value;

		foreach (var pt in ReflectionPoints)
		{
			int i = pt.X;
			int j = pt.Y;

			if (ReflectionPoints.Contains(new(i, j - 1)) && WorldGen.SolidOrSlopedTile(i, j - 1))
				continue;

			var t = Main.tile[i, j];
			if (t.IsHalfBlock)
			{
				spriteBatch.Draw(texture, new Vector2(i, j) * 16 - Main.screenPosition + new Vector2(0, 8), null, Color.White, 0, Vector2.Zero, 1, default, 0);
				continue;
			}
			else if (t.Slope != SlopeType.Solid)
			{
				for (int x = 0; x < 8; x++)
				{
					//Does not account for upside-down slopes
					Vector2 position = (t.Slope == SlopeType.SlopeDownLeft) ? new(2 * x, 2 * x) : new(2 * (7 - x), 2 * x);
					spriteBatch.Draw(texture, new Vector2(i, j) * 16 - Main.screenPosition + position, new Rectangle(0, 0, 2, texture.Height), Color.White, 0, Vector2.Zero, 1, default, 0);
				}

				continue;
			}

			spriteBatch.Draw(texture, new Vector2(i, j) * 16 - Main.screenPosition, null, Color.White, 0, Vector2.Zero, 1, default, 0);
		}
	}

	private static void DrawTileTarget(SpriteBatch spriteBatch)
	{
		var texture = TextureAssets.Tile[ModContent.TileType<SaltBlock>()].Value;

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

		DrawBG(Main.instance);

		spriteBatch.Draw(Main.instance.wallTarget, Main.sceneWallPos - Main.screenPosition, Color.White);
		spriteBatch.Draw(Main.instance.tileTarget, Main.sceneTilePos - Main.screenPosition, Color.White);
		spriteBatch.Draw(Main.instance.tile2Target, Main.sceneTile2Pos - Main.screenPosition, Color.White);

		DrawNPCs(Main.instance);

		spriteBatch.End();

		DrawPlayers_AfterProjectiles(Main.instance);

		gd.SetRenderTarget(null);

		Main.GameViewMatrix.Zoom = storedZoom;
	}

	private static void DrawFullReflection()
	{
		if (!Drawing || ReflectionTarget.Target is null || MapTarget.Target is null || TileTarget.Target is null)
			return;

		var s = AssetLoader.LoadedShaders["Reflection"];
		s.Parameters["mapTexture"].SetValue(MapTarget);
		s.Parameters["distortionTexture"].SetValue(AssetLoader.LoadedTextures["supPerlin"].Value);
		s.Parameters["tileTexture"].SetValue(TileTarget);

		s.Parameters["reflectionHeight"].SetValue(ReflectionTarget.Target.Height / 4);
		s.Parameters["fade"].SetValue(3f);
		s.Parameters["distortMult"].SetValue(new Vector2(1));
		s.Parameters["distortStrength"].SetValue(new Vector2(0.3f, 0));

		Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, s, Main.Transform);

		Color tint = new Color(255, 190, 200, 220) * 0.9f;
		Main.spriteBatch.Draw(ReflectionTarget, Vector2.Zero, null, tint, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
		Main.spriteBatch.End();

		Drawing = false;
	}

	public void Unload() { }
}