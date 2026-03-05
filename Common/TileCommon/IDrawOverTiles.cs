using SpiritReforged.Common.Visuals.RenderTargets;

namespace SpiritReforged.Common.TileCommon;

/// <summary> Can be applied to projectiles. </summary>
internal interface IDrawOverTiles
{
	public void DrawOverTiles(SpriteBatch spriteBatch);
}

internal class DrawOverHandler : ModSystem
{
	public static event Action PostDrawTilesSolid;

	public static bool Drawing => CacheValue ?? false;

	internal static readonly HashSet<int> DrawTypes = [];
	private static bool? CacheValue = null;
	private static readonly HashSet<Projectile> DrawCache = [];

	internal static ModTarget2D TileTarget { get; } = new(StartCache, DrawTileTarget);
	internal static ModTarget2D OverlayTarget { get; } = new(StartCache, DrawOverlayTarget);

	public override void Load()
	{
		On_Main.DoDraw_Tiles_Solid += static (orig, self) =>
		{
			orig(self);
			PostDrawTilesSolid?.Invoke();
		};

		PostDrawTilesSolid += DrawTargetContents;
	}

	public override void SetStaticDefaults()
	{
		foreach (var c in Mod.GetContent<ModProjectile>())
		{
			if (c is IDrawOverTiles)
				DrawTypes.Add(c.Type);
		}
	}

	private static bool StartCache()
	{
		if (CacheValue is bool value)
			return value;

		foreach (var p in Main.ActiveProjectiles)
		{
			if (DrawTypes.Contains(p.type))
				DrawCache.Add(p);
		}

		return (CacheValue = DrawCache.Count != 0) is true;
	}

	private static void DrawTileTarget(SpriteBatch spriteBatch)
	{
		spriteBatch.Draw(Main.instance.tileTarget, Main.sceneTilePos - Main.screenPosition, Color.White);
		//spriteBatch.Draw(Main.instance.tile2Target, Main.sceneTile2Pos - Main.screenPosition, Color.White);
	}

	private static void DrawOverlayTarget(SpriteBatch spriteBatch)
	{
		foreach (var p in DrawCache)
		{
			if (p.ModProjectile is IDrawOverTiles t)
				t.DrawOverTiles(spriteBatch);
		}
	}

	private static void DrawTargetContents()
	{
		bool drawing = Drawing;
		DrawCache.Clear();
		CacheValue = null;

		if (!drawing || OverlayTarget.Target is null || TileTarget.Target is null)
			return;

		var s = AssetLoader.LoadedShaders["SimpleMultiply"].Value;
		s.Parameters["tileTexture"].SetValue(TileTarget);
		s.Parameters["lightness"].SetValue(100);

		Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, Main.Rasterizer, s, Main.GameViewMatrix.TransformationMatrix);

		Main.spriteBatch.Draw(OverlayTarget, Vector2.Zero, null, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
		Main.spriteBatch.End();
	}
}