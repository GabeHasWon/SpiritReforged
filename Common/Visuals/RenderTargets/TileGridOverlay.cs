using SpiritReforged.Common.TileCommon;
using Terraria.DataStructures;

namespace SpiritReforged.Common.Visuals.RenderTargets;

public abstract class TileGridOverlay
{
	public static readonly Action<SpriteBatch, RenderTarget2D, Action> PrepareDefault = (sb, target, action) =>
	{
		var gd = Main.graphics.GraphicsDevice;

		gd.SetRenderTarget(target);
		gd.Clear(Color.Transparent);

		sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, Main.Rasterizer, null);
		action.Invoke();

		sb.End();
		gd.SetRenderTarget(null);
	};

	public readonly ModTarget2D tileTarget;
	public readonly ModTarget2D overlayTarget;

	protected readonly HashSet<Point16> _grid = [];
	private readonly TileEvents.PreDrawDelegate _preDrawDelegate;
	private bool _canDraw;

	public virtual bool CanDraw => _canDraw || _grid.Count != 0;

	public TileGridOverlay()
	{
		tileTarget = new(() => CanDraw, RenderTileTarget, false);
		overlayTarget = new(() => CanDraw, RenderOverlayTarget, false);

		_preDrawDelegate = TileEvents.AddPreDrawAction(true, _grid.Clear);
	}

	/// <summary> Must be called before an instance can be safely disposed of. </summary>
	public void Unload() => TileEvents.OnPreDrawTiles -= _preDrawDelegate;

	public void AddToGrid(int i, int j)
	{
		_grid.Add(new Point16(i, j));
		_canDraw = true;
	}

	public virtual void RenderTileTarget(SpriteBatch spriteBatch) => PrepareDefault.Invoke(spriteBatch, tileTarget, () =>
	{
		foreach (var pt in _grid)
			TileExtensions.DrawSingleTile(pt.X, pt.Y, true, Vector2.Zero);
	});

	public abstract void RenderOverlayTarget(SpriteBatch spriteBatch);
	protected abstract void DrawContents(SpriteBatch spriteBatch);

	public void Draw()
	{
		if (CanDraw)
			DrawContents(Main.spriteBatch);

		_canDraw = false;
	}
}