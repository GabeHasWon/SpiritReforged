using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals.RenderTargets;
using System.Linq;

namespace SpiritReforged.Content.Particles;

/// <summary>
/// Renders a composite smoke
/// Partially referenced from https://github.com/IbanPlay/FablesRelease/blob/c83ceb82fdf976226619b11ab34f5834b66f3c09/Particles/BlendedSmoke.cs#L119
/// </summary>
public class SmokeTargetSystem : ModSystem
{
	private readonly static BlendState Max = new()
	{
		AlphaBlendFunction = BlendFunction.Max,
		ColorBlendFunction = BlendFunction.Max,
		ColorSourceBlend = Blend.One,
		ColorDestinationBlend = Blend.One,
		AlphaSourceBlend = Blend.One,
		AlphaDestinationBlend = Blend.One
	};

	public static readonly List<CompositeSmoke> particles = [];

	// there are NINE particle layers! so we need a render target with 9 "frames"

	private static readonly ModTarget2D SmokeTarget = new(static () => particles.Count != 0, BuildTarget, scale: new Vector2(0.5f, 9 * 0.5f));

	public override void PostUpdateEverything() =>
			particles.RemoveAll(p => p.TimeActive > p.MaxTime);

	private static void BuildTarget(SpriteBatch spriteBatch)
	{
		if (particles.Count == 0) // Don't restart the spritebatch if there are no particles present
			return;

		bool resetSpriteBatch = false;
		
		var oldRasterizer = spriteBatch.GraphicsDevice.RasterizerState;
		var oldBounds = spriteBatch.GraphicsDevice.ScissorRectangle;
		var oldTestEnable = oldRasterizer.ScissorTestEnable;

		var rasterizer = RasterizerState.CullNone;
		rasterizer.ScissorTestEnable = true;

		for (int i = 0; i < 9; i++)
		{
			var bounds = SmokeTarget.Target.Frame(1, 9, 0, i);

			var layer = (ParticleLayer)i;

			// do not reset spriteBatch unless the layer is actively being rendered
			if (!particles.Any(p => p.DrawLayer == layer))
				continue;

			resetSpriteBatch = true;

			spriteBatch.End();
			spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, rasterizer);

			spriteBatch.GraphicsDevice.ScissorRectangle = bounds;

			foreach (CompositeSmoke p in particles.Where(p => p.DrawLayer == layer))
			{
				if (p is null)
					continue;

				p.TargetDraw(spriteBatch, Color.Black, (int)(SmokeTarget.Target.Size().Y / 9) * i);
			}
		}

		for (int i = 0; i < 9; i++)
		{
			var bounds = SmokeTarget.Target.Frame(1, 9, 0, i);

			var layer = (ParticleLayer)i;

			// do not reset spriteBatch unless the layer is actively being rendered
			if (!particles.Any(p => p.DrawLayer == layer))
				continue;

			spriteBatch.End();
			spriteBatch.Begin(SpriteSortMode.Immediate, Max, SamplerState.PointClamp, DepthStencilState.None, rasterizer);

			spriteBatch.GraphicsDevice.ScissorRectangle = bounds;

			foreach (CompositeSmoke p in particles.Where(p => p.DrawLayer == layer))
			{
				if (p is null)
					continue;

				p.TargetDraw(spriteBatch, p.Color, (int)(SmokeTarget.Target.Size().Y / 9) * i);
			}
		}

		spriteBatch.GraphicsDevice.RasterizerState = oldRasterizer;
		spriteBatch.GraphicsDevice.ScissorRectangle = oldBounds;
		oldRasterizer.ScissorTestEnable = oldTestEnable;

		if (resetSpriteBatch)
		{
			spriteBatch.End();
			spriteBatch.BeginDefault();
		}
	}

	/// <summary>
	/// Draws the composite smoke with the Y frame dependent on the layer
	/// Called in ParticleDetours.cs
	/// </summary>
	/// <param name="frameY">0-8, corresponds to each layer of ParticleLayer</param>
	public static void DrawCompositeSmoke(int frameY, bool startBatch)
	{
		if (SmokeTarget != null && SmokeTarget.Active)
		{
			SpriteBatch spriteBatch = Main.spriteBatch;

			if (startBatch)
				spriteBatch.BeginDefault();

			var sourceRectangle = SmokeTarget.Target.Frame(1, 9, 0, frameY);

			spriteBatch.Draw(SmokeTarget.Target, Vector2.Zero, sourceRectangle, Color.White * 0.4f, 0f, Vector2.Zero, 2f, 0f, 0f);

			if (startBatch)
				spriteBatch.End();
		}
	}
}

public class CompositeSmoke : Particle
{
	internal bool addedToList = false;

	internal bool _addLight;
	internal bool _addBloom;

	internal int _variant;

	internal readonly Action<Particle> _action;

	public virtual int VerticalFrames => 5;
	public virtual int HorizontalFrames => 3;

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;
	public ParticleLayer Layer { get; set; } = ParticleLayer.BelowProjectile;
	public override ParticleLayer DrawLayer => Layer;
	public CompositeSmoke(Vector2 position, Vector2 velocity, Color color, int maxTime, bool addLight = true, bool addBloom = true, Action<Particle> extraUpdateAction = null)
	{
		Position = position;
		Velocity = velocity;
		Rotation = 0f;
		Scale = 1f;
		MaxTime = maxTime;

		Color = color;

		_addLight = addLight;
		_addBloom = addBloom;
		_action = extraUpdateAction;

		if (HorizontalFrames > 1)
			_variant = Main.rand.Next(HorizontalFrames);
		else
			_variant = 0;
	}

	public override void Update()
	{
		if (!addedToList)
		{
			SmokeTargetSystem.particles.Add(this);
			addedToList = true;
		}

		Velocity *= 0.98f;

		if (_addLight)
			Lighting.AddLight(Position, Color.R / 255f, Color.G / 255f, Color.B / 255f);

		_action?.Invoke(this);
	}

	public override void OnKill() => SmokeTargetSystem.particles.Remove(this);

	public void TargetDraw(SpriteBatch spriteBatch, Color color, int yOffset)
	{
		var texture = Texture;

		float progress = Progress;

		var frame = Texture.Frame(HorizontalFrames, VerticalFrames, _variant, (int)MathHelper.Lerp(0, VerticalFrames, progress));

		float fadeOut = 1f;
		
		if (progress < 0.1f)
			fadeOut = progress / 0.1f;

		if (progress > 0.5f)
			fadeOut = 1f - (progress - 0.5f) / 0.5f;

		spriteBatch.Draw(texture, (Position - Main.screenPosition) / 2 + Vector2.UnitY * yOffset, frame, color * fadeOut, Rotation, frame.Size() / 2, Scale / 2, SpriteEffects.None, 0);
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		var bloom = AssetLoader.LoadedTextures["Bloom"].Value;

		float progress = Progress;

		float fadeOut = 1f;

		if (progress < 0.1f)
			fadeOut = progress / 0.1f;

		if (progress > 0.5f)
			fadeOut = 1f - (progress - 0.5f) / 0.5f;

		if (_addBloom)
			spriteBatch.Draw(bloom, Position - Main.screenPosition, null, Color with { A = 0 } * 0.33f * fadeOut, Rotation, bloom.Size() / 2, Scale * 0.5f, SpriteEffects.None, 0);
	}
}

/// <summary>
/// Can be attached to an entity
/// </summary>
public class AttachedCompositeSmoke : CompositeSmoke
{
	internal Entity Parent;
	internal Vector2 _offset;

	public AttachedCompositeSmoke(Entity parent, Vector2 offset, Vector2 velocity, Color color, int maxTime, bool addLight = true, bool addBloom = true, Action<Particle> extraUpdateAction = null) : base(Vector2.Zero, velocity, color, maxTime, addLight, addBloom, extraUpdateAction)
	{
		Parent = parent;
		_offset = offset;

		Position = parent.Center + offset;
	}

	public override void Update()
	{
		Position = Parent.Center + _offset;

		_offset -= Velocity;
		Velocity *= 0.98f;

		Rotation += Velocity.Length() * 0.01f;

		if (_addLight)
			Lighting.AddLight(Position, Color.R / 255f, Color.G / 255f, Color.B / 255f);

		_action?.Invoke(this);
	}
}

public class SmallCompositeSmoke : CompositeSmoke
{
	public SmallCompositeSmoke(Vector2 position, Vector2 velocity, Color color, int maxTime, bool addLight = true, bool addBloom = true, Action<Particle> extraUpdateAction = null) : base(position, velocity, color, maxTime, addLight, addBloom, extraUpdateAction)
	{

	}
}
