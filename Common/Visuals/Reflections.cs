using SpiritReforged.Common.ConfigurationCommon;
using SpiritReforged.Common.Visuals.RenderTargets;
using System.Runtime.CompilerServices;

namespace SpiritReforged.Common.Visuals;

public sealed class Reflections : ILoadable
{
	/// <summary> Whether reflection detail is high enough to calculate. </summary>
	public static bool Enabled => Lighting.NotRetro && Detail > 0;
	public static int Detail => ModContent.GetInstance<ReforgedClientConfig>().ReflectionDetail;

	/// <summary> Whether screen dimensions are beyond 1920x1080.<br/>Included in a bandaid fix for tiles not reflecting correctly on high resolutions. </summary>
	public static bool HighResolution { get; private set; }
	/// <summary> Whether any reflection is being drawn. </summary>
	public static bool DrawingReflection { get; set; }

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "CacheNPCDraws")]
	internal static extern void CacheNPCDraws(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "CacheProjDraws")]
	internal static extern void CacheProjDraws(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawCachedNPCs")]
	internal static extern void DrawCachedNPCs(Main main, List<int> npcCache, bool behindTiles);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawCachedProjs")]
	internal static extern void DrawCachedProjs(Main main, List<int> projectileCache, bool startSpritebatch = true);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawBackGore")]
	internal static extern void DrawBackGore(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawTileEntities")]
	internal static extern void DrawTileEntities(Main main, bool solidLayer, bool overRenderTargets, bool intoRenderTargets);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawPlayers_AfterProjectiles")]
	internal static extern void DrawPlayers_AfterProjectiles(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawPlayers_BehindNPCs")]
	internal static extern void DrawPlayers_BehindNPCs(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawProjectiles")]
	internal static extern void DrawProjectiles(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawDust")]
	internal static extern void DrawDust(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawRain")]
	private static extern void DrawRainInternal(Main main);

	internal static void DrawRain()
	{
		//Pauses game so the rain doesnt get updated twice 
		bool cachedPauseState = Main.gamePaused;
		Main.gamePaused = true;
		DrawRainInternal(Main.instance);
		Main.gamePaused = cachedPauseState;
	}

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawGore")]
	internal static extern void DrawGore(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawNPCs")]
	internal static extern void DrawNPCs(Main main, bool behindTiles = false);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawBG")]
	internal static extern void DrawBG(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawStarsInBackground")]
	internal static extern void DrawStarsInBackground(Main main, Main.SceneArea sceneArea, bool artificial);

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "bgParallax")]
	internal static extern ref double GetBgParallax(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawBlack")]
	internal static extern void DrawBlack(Main main, bool force = false);

	/// <summary> Gets a gradient texture for shader mapping. </summary>
	/// <param name="width"> The pre-upscaled width of the texture. </param>
	/// <param name="height"> The pre-upscaled height of the texture.</param>
	public static Texture2D CreateTilemap(int width, int height)
	{
		const int taper = 5; //Opacity taper downscaled

		var data = new Color[width * height];
		for (int i = 0; i < data.Length; i++)
		{
			int y = i / width;

			float strengthR = Math.Clamp((float)y / 255, 0, 1);
			float strengthG = Math.Clamp((float)y / 255 - 1, 0, 1);
			float strengthB = Math.Clamp((float)y / 255 - 2, 0, 1);
			float opacity = Math.Min((float)y / taper, 1);

			if (y > taper)
				opacity = 1f - Math.Max((float)(y - (height - taper)) / taper, 0);

			data[i] = new Color(1f - strengthR, 1f - strengthG, 1f - strengthB, opacity);
		}

		var texture = new Texture2D(Main.graphics.GraphicsDevice, width, height);
		texture.SetData(data);

		return texture;
	}

	public void Load(Mod mod) => TargetSetup.OnResizeRendertargets += static () => HighResolution = Main.graphics.GraphicsDevice.Viewport.Width > 1920 || Main.graphics.GraphicsDevice.Viewport.Height > 1080;
	public void Unload() { }
}