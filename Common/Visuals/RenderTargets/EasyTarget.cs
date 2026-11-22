namespace SpiritReforged.Common.Visuals.RenderTargets;

/// <summary>Represents a modular <see cref="RenderTarget2D"/> handler.</summary>
public class EasyTarget : IDisposable
{
	public RenderTarget2D Value { get; protected set; }

	/// <summary>The scale of this render target relative to the screen size.<br/>
	/// Typically used for pixelation effects (0.5x scale RT drawn at 2x scale), defaults to 1x scale</summary>
	private readonly Vector2? _scale;

	/// <param name="scale">The desired scale of the render target relative to the screen size.</param>
	public EasyTarget(Vector2? scale = null)
	{
		if (Main.dedServ)
			return;

		if (scale.HasValue)
			_scale = scale;
		else
			_scale = Vector2.One;

		Main.QueueMainThreadAction(() =>
		{
			var gd = Main.instance.GraphicsDevice;
			Value = new RenderTarget2D(gd, (int)(gd.Viewport.Width * _scale.Value.X), (int)(gd.Viewport.Height * _scale.Value.Y), false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
		});

		Main.OnResolutionChanged += Resize;
	}

	/// <summary> Resizes the render target to <paramref name="size"/>. Automatically queued on the main thread. </summary>
	public void Resize(Vector2 size) => Main.QueueMainThreadAction(() =>
	{
		GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;

		Value.Dispose();
		Value = new RenderTarget2D(graphicsDevice, (int)(size.X * _scale.Value.X), (int)(size.Y * _scale.Value.Y), false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
	});

	public void Dispose()
	{
		Value?.Dispose();
		Main.OnResolutionChanged -= Resize;

		GC.SuppressFinalize(this);
	}
}