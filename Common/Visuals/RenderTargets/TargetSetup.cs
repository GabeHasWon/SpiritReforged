namespace SpiritReforged.Common.Visuals.RenderTargets;

/// <summary> Handles <see cref="ModTarget2D"/> hooks. Does not exist on the server. </summary>
[Autoload(Side = ModSide.Client)]
internal class TargetSetup : ILoadable
{
	/// <summary> Called with <see cref="On_Main.CheckMonoliths"/>. </summary>
	public static event Action DrawIntoRendertargets;

	public void Load(Mod mod)
	{
		On_Main.CheckMonoliths += static (orig) =>
		{
			orig();

			if (!Main.gameMenu && !Main.dedServ)
				DrawIntoRendertargets?.Invoke();
		};

		DrawIntoRendertargets += DrawIntoTargets;
		Main.OnResolutionChanged += ResizeAll;
	}

	private static void DrawIntoTargets()
	{
		foreach (var target in ModTarget2D.Targets)
		{
			if (target.Active)
				target.Prepare(Main.spriteBatch);
		}
	}

	private static void ResizeAll(Vector2 obj)
	{
		foreach (var t in ModTarget2D.Targets)
		{
			var viewport = Main.instance.GraphicsDevice.Viewport;
			t.Resize(new(viewport.Width, viewport.Height));
		}
	}

	public void Unload() => Main.OnResolutionChanged -= ResizeAll;
}