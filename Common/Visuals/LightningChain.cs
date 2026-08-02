using SpiritReforged.Common.Misc;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using Terraria.Utilities;

namespace SpiritReforged.Common.Visuals;

public readonly struct LightningTrailPosition(Func<Vector2> position) : ITrailPosition
{
	public Vector2 GetNextTrailPosition() => position.Invoke();
}

public class LightningChain
{
	public readonly Vector2 start;
	public readonly Vector2 end;
	public readonly int width;
	public readonly Color color;

	private readonly Entity _attached;

	private VertexTrail[] _trails;
	private Vector2 _floatingPosition;

	public LightningChain(Vector2 start, Vector2 end, Color color, int width, Entity attached = null)
	{
		this.start = start;
		this.end = end;
		this.width = width;
		this.color = color;

		_attached = attached;

		Reconfigure();
	}

	public void Reconfigure(int? seed = null)
	{
		_floatingPosition = start;

		ITrailShader shader = new ImageShader(AssetLoader.LoadedTextures["GlowTrail"].Value, Vector2.One);
		ITrailPosition position = (_attached != null) ? new EntityTrailPosition(_attached) : new LightningTrailPosition(() => _floatingPosition);

		float angle = start.AngleTo(end);
		float fullLength = start.Distance(end);

		_trails =
		[
			new(new StandardColorTrail(color), new TriangleCap(), position, shader, width, fullLength),
			new(new StandardColorTrail(Color.White.Additive()), new TriangleCap(), position, shader, width / 2, fullLength)
		];

		float slice = fullLength / 6;
		int div = (int)(fullLength / slice);
		UnifiedRandom random = (seed is int seedValue) ? new(seedValue) : new();

		for (int i = 0; i < div; i++)
		{
			float deviation = random.NextFloat(0.3f, 0.6f) * random.NextFromList(-1, 1);
			float halfSlice = slice / 2;

			_floatingPosition += new Vector2(halfSlice, halfSlice * deviation).RotatedBy(angle);

			foreach (var trail in _trails)
				trail.Update();

			_floatingPosition += new Vector2(halfSlice, -(halfSlice * deviation)).RotatedBy(angle);

			foreach (var trail in _trails)
				trail.Update();
		}
	}

	public void Update()
	{
		foreach (var trail in _trails)
		{
			trail.Update();
			trail.Dissolve();
		}
	}

	public void Draw(SpriteBatch spriteBatch, Matrix view)
	{
		foreach (var trail in _trails)
			trail.Draw(TrailSystem.TrailShaders, spriteBatch.GraphicsDevice, view);
	}
}