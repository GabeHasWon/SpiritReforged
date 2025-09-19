namespace SpiritReforged.Common.PrimitiveRendering.Trail_Components;

public abstract class BaseTrail
{
	public bool CanBeDisposed { get; protected set; }
	private bool _dissolving = false;

	public void Update()
	{
		if (_dissolving)
			OnDissolve();
		else
			OnUpdate();
	}

	public void Dissolve()
	{
		if (!_dissolving)
		{
			OnStartDissolve();
			_dissolving = true;
		}
	}

	/// <summary> Behavior for the trail every tick, only called before the trail begins dying. </summary>
	protected virtual void OnUpdate() { }

	/// <summary> Behavior for the trail after it starts its death, called every tick after the trail begins dying. </summary>
	protected virtual void OnDissolve() { }

	/// <summary> Additional behavior for the trail upon starting its death. </summary>
	protected virtual void OnStartDissolve() { }

	/// <summary> How the trail is drawn to the screen. </summary>
	public virtual void Draw(Effect effect, BasicEffect effect2, GraphicsDevice device) { }
}