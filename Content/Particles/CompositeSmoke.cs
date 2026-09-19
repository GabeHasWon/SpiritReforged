using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.RenderTargets;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Particles;

/// <summary> Renders a composite smoke effect<br/>
/// Partially referenced from https://github.com/IbanPlay/FablesRelease/blob/c83ceb82fdf976226619b11ab34f5834b66f3c09/Particles/BlendedSmoke.cs#L119 </summary>
[Autoload(Side = ModSide.Client)]
public class CompositeRenderer : ModSystem
{
	public interface ICompositeRendering
	{
		public Color Color { get; set; }

		public void TargetDraw(SpriteBatch spriteBatch, Color color);
	}

	private readonly static BlendState Max = new()
	{
		AlphaBlendFunction = BlendFunction.Max,
		ColorBlendFunction = BlendFunction.Max,
		ColorSourceBlend = Blend.One,
		ColorDestinationBlend = Blend.One,
		AlphaSourceBlend = Blend.One,
		AlphaDestinationBlend = Blend.One
	};

	public static readonly HashSet<ICompositeRendering> CompositeItems = [];

	// there are NINE particle layers! so we need a render target with 9 "frames"

	private static readonly EasyTarget CompositeTarget = new(new Vector2(0.5f));

	public override void Load() => TargetSetup.DrawIntoRendertargets += SetupTarget;

	public override void PostUpdateEverything() =>
			CompositeItems.RemoveAll(p => p.TimeActive > p.MaxTime);

	private static void SetupTarget()
	{
		if (CompositeItems.Count == 0) // Don't restart the spritebatch if there are no particles present
			return;

		SpriteBatch spriteBatch = Main.spriteBatch;
		spriteBatch.GraphicsDevice.SetRenderTarget(CompositeTarget.Value);
		spriteBatch.GraphicsDevice.Clear(Color.Transparent);

		RasterizerState oldRasterizer = spriteBatch.GraphicsDevice.RasterizerState;
		Rectangle oldBounds = spriteBatch.GraphicsDevice.ScissorRectangle;
		bool oldTestEnable = oldRasterizer.ScissorTestEnable;

		RasterizerState rasterizer = RasterizerState.CullNone;
		rasterizer.ScissorTestEnable = true;

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, rasterizer);

		foreach (ParticleRenderer renderer in ParticleRenderers.Renderers)
		{
			foreach (IParticle particle in renderer.Particles)
			{
				if (particle is ICompositeRendering composite)
					composite.TargetDraw(spriteBatch, Color.Black);
			}
		}

		spriteBatch.End();
		spriteBatch.BeginDefault();

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Immediate, Max, SamplerState.PointClamp, DepthStencilState.None, rasterizer);

		foreach (ParticleRenderer renderer in ParticleRenderers.Renderers)
		{
			foreach (IParticle particle in renderer.Particles)
			{
				if (particle is ICompositeRendering composite)
					composite.TargetDraw(spriteBatch, composite.Color);
			}
		}

		spriteBatch.End();
		spriteBatch.BeginDefault();

		spriteBatch.GraphicsDevice.RasterizerState = oldRasterizer;
		spriteBatch.GraphicsDevice.ScissorRectangle = oldBounds;
		oldRasterizer.ScissorTestEnable = oldTestEnable;
		spriteBatch.GraphicsDevice.SetRenderTarget(null);
	}

	/// <summary> Draws the composite smoke with the Y frame dependent on the layer. <br/>
	/// Called in ParticleDetours. </summary>
	/// <param name="frameY">0-8, corresponds to each layer of ParticleLayer. </param>
	/// <param name="startBatch"> Whether to begin the spritebatch as default. </param>
	public static void DrawComposite(int frameY, bool startBatch)
	{
		if (CompositeTarget?.Value != null)
		{
			SpriteBatch spriteBatch = Main.spriteBatch;

			if (startBatch)
				spriteBatch.BeginDefault();

			var sourceRectangle = CompositeTarget.Target.Frame(1, 9, 0, frameY);

			spriteBatch.Draw(CompositeTarget.Target, Vector2.Zero, sourceRectangle, Color.White * 0.4f, 0f, Vector2.Zero, 2f, 0f, 0f);

			if (startBatch)
				spriteBatch.End();
		}
	}
}

public class CompositeSmoke : Particle, CompositeRenderer.ICompositeRendering
{
	internal bool addedToList = false;

	internal bool _addLight;
	internal bool _addBloom;

	internal int _variant;

	internal float _bloomOpacity;

	internal readonly Action<Particle> _action;

	public virtual int VerticalFrames => 5;
	public virtual int HorizontalFrames => 3;

	public Color Color { get; set; }

	public CompositeSmoke(Vector2 position, Vector2 velocity, Color color, int maxTime, bool addLight = true, bool addBloom = true, Action<Particle> extraUpdateAction = null, float bloomOpacity = 0.08f)
	{
		LocalPosition = position;
		Velocity = velocity;
		Rotation = 0f;
		Scale = Vector2.One;
		TimeMax = maxTime;

		Color = color;

		_addLight = addLight;
		_addBloom = addBloom;
		_action = extraUpdateAction;

		if (HorizontalFrames > 1)
			_variant = Main.rand.Next(HorizontalFrames);
		else
			_variant = 0;

		_bloomOpacity = bloomOpacity;
	}

	public override void Update(ref ParticleRendererSettings settings)
	{
		if (!addedToList)
		{
			SmokeTargetRenderer.CompositeItems.Add(this);
			addedToList = true;
		}

		Velocity *= 0.98f;

		if (_addLight)
			Lighting.AddLight(LocalPosition, Color.ToVector3() * (1f - Progress));

		_action?.Invoke(this);
	}

	public override void OnKill() => SmokeTargetRenderer.CompositeItems.Remove(this);

	public void TargetDraw(SpriteBatch spriteBatch, Color color)
	{
		Texture2D texture = Texture;
		Rectangle frame = Texture.Frame(HorizontalFrames, VerticalFrames, _variant, (int)MathHelper.Lerp(0, VerticalFrames, Progress));
		float progress = Progress;
		float fadeOut = 1f;
		
		if (progress < 0.1f)
			fadeOut = progress / 0.1f;

		if (progress > 0.5f)
			fadeOut = 1f - (progress - 0.5f) / 0.5f;

		spriteBatch.Draw(texture, (LocalPosition - Main.screenPosition) / 2, frame, color * fadeOut, Rotation, frame.Size() / 2, Scale / 2, SpriteEffects.None, 0);
	}

	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
	{
		Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;
		float progress = Progress;
		float fadeOut = 1f;

		if (progress < 0.1f)
			fadeOut = progress / 0.1f;

		if (progress > 0.5f)
			fadeOut = 1f - (progress - 0.5f) / 0.5f;

		if (_addBloom)
			spritebatch.Draw(bloom, LocalPosition + settings.AnchorPosition, null, Color.Additive() * _bloomOpacity * fadeOut, Rotation, bloom.Size() / 2, Scale * 0.5f, SpriteEffects.None, 0);
	}
}

/// <summary> Can be attached to an entity </summary>
public class AttachedCompositeSmoke : CompositeSmoke
{
	internal Entity Parent;
	internal Vector2 _offset;

	public AttachedCompositeSmoke(Entity parent, Vector2 offset, Vector2 velocity, Color color, int maxTime, bool addLight = true, bool addBloom = true, Action<Particle> extraUpdateAction = null, float bloomOpacity = 0.08f) : base(Vector2.Zero, velocity, color, maxTime, addLight, addBloom, extraUpdateAction, bloomOpacity)
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
	public SmallCompositeSmoke(Vector2 position, Vector2 velocity, Color color, int maxTime, bool addLight = true, bool addBloom = true, Action<Particle> extraUpdateAction = null, float bloomOpacity = 0.08f) : base(position, velocity, color, maxTime, addLight, addBloom, extraUpdateAction, bloomOpacity)
	{

	}
}
