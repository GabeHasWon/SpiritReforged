namespace SpiritReforged.Common.Visuals.RenderTargets;

/// <summary> Modular <see cref="RenderTarget2D"/> instance handler. Can be used by either creating a derivative or using the provided constructor. </summary>
public class ModTarget2D : ILoadable
{
	public static readonly HashSet<ModTarget2D> Targets = [];

	private bool IsDerived => GetType() != typeof(ModTarget2D); //It's important that we use typeof explicitly here
	public virtual bool Active => _activeCondition.Invoke();

	public RenderTarget2D Target { get; protected set; }

	private readonly Func<bool> _activeCondition;
	private readonly Action<SpriteBatch> _drawAction;
	private readonly bool _prepare;

	protected ModTarget2D() { } //Include an empty constructor so that ILoadable can function

	/// <param name="activeCondition"></param>
	/// <param name="drawAction"></param>
	/// <param name="prepare"> Whether <see cref="Prepare"/> should be called. This logic should be handled manually in <paramref name="drawAction"/> otherwise. </param>
	public ModTarget2D(Func<bool> activeCondition, Action<SpriteBatch> drawAction = null, bool prepare = true)
	{
		_activeCondition = activeCondition;
		_drawAction = drawAction;
		_prepare = prepare;

		Register();
	}

	private void Register()
	{
		if (Main.dedServ)
			return;

		Main.QueueMainThreadAction(() =>
		{
			var gd = Main.instance.GraphicsDevice;
			Target = new RenderTarget2D(gd, gd.Viewport.Width, gd.Viewport.Height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
		});

		Targets.Add(this);
	}

	/// <summary> Draws the contents of <see cref="DrawInto(SpriteBatch)"/> and automatically handles <paramref name="spriteBatch"/> modes.<br/>
	/// Check <see cref="Active"/> before calling this method. </summary>
	public virtual void Prepare(SpriteBatch spriteBatch) //You don't normally need to override this
	{
		if (!IsDerived)
		{
			if (_drawAction is null)
			{
				return; //Return because DrawInto was never overridden, and therefore draws nothing
			}
			else if (!_prepare)
			{
				DrawInto(spriteBatch);
				return;
			}
		}

		var gd = Main.graphics.GraphicsDevice;

		gd.SetRenderTarget(Target);
		gd.Clear(Color.Transparent);

		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null);
		DrawInto(spriteBatch);

		spriteBatch.End();
		gd.SetRenderTarget(null);
	}

	protected virtual void DrawInto(SpriteBatch spriteBatch) => _drawAction.Invoke(spriteBatch);

	public void Load(Mod mod)
	{
		if (!IsDerived)
			return; //Don't load the dummy instance

		Register();
		Load();
	}

	public virtual void Load() { }
	public void Unload() { }

	public static implicit operator RenderTarget2D(ModTarget2D e) => e.Target;
}