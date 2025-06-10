namespace SpiritReforged.Common.TileCommon;

/// <summary> Can be applied to projectiles. </summary>
internal interface IDrawOverTiles
{
	public void DrawOverTiles(SpriteBatch spriteBatch);
}

internal class DrawOverHandler : ModSystem
{
	public static event Action PostDrawTilesSolid;

	internal static readonly HashSet<int> DrawTypes = [];
	public static bool Drawing { get; private set; }
	internal static RenderTarget2D TileTarget { get; private set; }
	internal static RenderTarget2D OverlayTarget { get; private set; }

	public override void Load()
	{
		Main.QueueMainThreadAction(() =>
		{
			var gd = Main.instance.GraphicsDevice;

			TileTarget = new RenderTarget2D(gd, gd.Viewport.Width, gd.Viewport.Height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
			OverlayTarget = new RenderTarget2D(gd, gd.Viewport.Width, gd.Viewport.Height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
		});

		On_Main.CheckMonoliths += DrawIntoTargets;
		On_Main.DoDraw_Tiles_Solid += static (orig, self) =>
		{
			orig(self);
			PostDrawTilesSolid?.Invoke();
		};

		PostDrawTilesSolid += DrawTargets;
	}

	public override void SetStaticDefaults()
	{
		foreach (var c in Mod.GetContent<ModProjectile>())
		{
			if (c is IDrawOverTiles)
				DrawTypes.Add(c.Type);
		}
	}

	private static void DrawIntoTargets(On_Main.orig_CheckMonoliths orig)
	{
		orig();

		if (Main.gameMenu || Main.dedServ)
			return;

		HashSet<Projectile> cached = [];
		foreach (var p in Main.ActiveProjectiles)
		{
			if (DrawTypes.Contains(p.type))
				cached.Add(p);
		}

		if (!(Drawing = cached.Count != 0))
			return;

		var spriteBatch = Main.spriteBatch;
		var gd = Main.graphics.GraphicsDevice;

		DrawTileTarget();
		DrawOverlayTarget();

		void DrawOverlayTarget()
		{
			gd.SetRenderTarget(OverlayTarget);
			gd.Clear(Color.Transparent);

			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null);

			foreach (var p in cached)
				(p.ModProjectile as IDrawOverTiles).DrawOverTiles(Main.spriteBatch);

			spriteBatch.End();
			gd.SetRenderTarget(null);
		}

		void DrawTileTarget()
		{
			gd.SetRenderTarget(TileTarget);
			gd.Clear(Color.Transparent);

			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null);

			spriteBatch.Draw(Main.instance.tileTarget, Main.sceneTilePos - Main.screenPosition, Color.White);
			//spriteBatch.Draw(Main.instance.tile2Target, Main.sceneTile2Pos - Main.screenPosition, Color.White);

			spriteBatch.End();
			gd.SetRenderTarget(null);
		}
	}

	private static void DrawTargets()
	{
		if (!Drawing || OverlayTarget is null || TileTarget is null)
			return;

		var s = AssetLoader.LoadedShaders["SimpleMultiply"];
		s.Parameters["tileTexture"].SetValue(TileTarget);

		Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, Main.Rasterizer, s, Main.GameViewMatrix.TransformationMatrix);

		Main.spriteBatch.Draw(OverlayTarget, Vector2.Zero, null, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
		Main.spriteBatch.End();
	}
}