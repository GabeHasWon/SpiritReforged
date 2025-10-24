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

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawPlayers_AfterProjectiles")]
	internal static extern void DrawPlayers_AfterProjectiles(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawPlayers_BehindNPCs")]
	internal static extern void DrawPlayers_BehindNPCs(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawProjectiles")]
	internal static extern void DrawProjectiles(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawDust")]
	internal static extern void DrawDust(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawGore")]
	internal static extern void DrawGore(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawNPCs")]
	internal static extern void DrawNPCs(Main main, bool behindTiles = false);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawBG")]
	internal static extern void DrawBG(Main main);

	/// <summary> Gets a gradient texture for shader mapping. </summary>
	/// <param name="width"> The pre-upscaled width of the texture. </param>
	/// <param name="height"> The pre-upscaled height of the texture.</param>
	public static Texture2D CreateTilemap(int width, int height)
	{
		const int maximum_height = 255 * 3;
		const int taper = 10; //Opacity taper downscaled

		var data = new Color[width * height];
		for (int i = 0; i < data.Length; i++)
		{
			int y = i / width;

			float strengthR = Math.Clamp((float)y / 255, 0, 1);
			float strengthG = Math.Clamp((float)y / 255 - 1, 0, 1);
			float strengthB = Math.Clamp((float)y / 255 - 2, 0, 1);
			float opacity = Math.Min((float)y / taper, 1);

			if (y > taper)
				opacity = 1f - Math.Max((float)(y - (maximum_height - taper)) / taper, 0);

			data[i] = new Color(1f - strengthR, 1f - strengthG, 1f - strengthB, opacity);
		}

		var texture = new Texture2D(Main.graphics.GraphicsDevice, width, height);
		texture.SetData(data);

		return texture;
	}

	public void Load(Mod mod) => TargetSetup.OnResizeRendertargets += static () => HighResolution = Main.graphics.GraphicsDevice.Viewport.Width > 1920 || Main.graphics.GraphicsDevice.Viewport.Height > 1080;
	public void Unload() { }
}