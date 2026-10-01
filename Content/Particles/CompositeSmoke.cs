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

		public void TargetDraw(SpriteBatch spriteBatch, Color color, int layer);
	}

	public static int RendererCount => ParticleRenderers.Renderers.Length;

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

	private static readonly EasyTarget CompositeTarget = new(new Vector2(0.5f, 0.5f * RendererCount));

	public override void Load()
	{
		TargetSetup.DrawIntoRendertargets += SetupTarget;
		ParticleRenderers.OnDrawParticles += DrawComposite;
	}

	private static void SetupTarget()
	{
		if (CompositeItems.Count == 0) // Don't restart the spritebatch if there are no composite items present
			return;

		SpriteBatch spriteBatch = Main.spriteBatch;
		spriteBatch.GraphicsDevice.SetRenderTarget(CompositeTarget.Value);
		spriteBatch.GraphicsDevice.Clear(Color.Transparent);

		RasterizerState oldRasterizer = spriteBatch.GraphicsDevice.RasterizerState;
		Rectangle oldBounds = spriteBatch.GraphicsDevice.ScissorRectangle;
		bool oldTestEnable = oldRasterizer.ScissorTestEnable;

		RasterizerState rasterizer = RasterizerState.CullNone;
		rasterizer.ScissorTestEnable = true;

		spriteBatch.Begin(SpriteSortMode.Immediate, Max, SamplerState.PointClamp, DepthStencilState.None, rasterizer);

		for (int i = 0; i < RendererCount; i++)
		{
			ParticleRenderer renderer = ParticleRenderers.Renderers[i];
			Rectangle crop = CompositeTarget.Value.Frame(1, RendererCount, 0, i);
			spriteBatch.GraphicsDevice.ScissorRectangle = crop;

			foreach (IParticle particle in renderer.Particles)
			{
				if (particle is ICompositeRendering composite)
					composite.TargetDraw(spriteBatch, composite.Color, i);
			}
		}

		spriteBatch.End();

		spriteBatch.GraphicsDevice.RasterizerState = oldRasterizer;
		spriteBatch.GraphicsDevice.ScissorRectangle = oldBounds;
		oldRasterizer.ScissorTestEnable = oldTestEnable;
		spriteBatch.GraphicsDevice.SetRenderTarget(null);

		CompositeItems.Clear();
	}

	/// <summary> Draws the composite smoke with the Y frame dependent on the layer. <br/>
	/// Called in ParticleDetours. </summary>
	public static void DrawComposite(ParticleRenderer renderer)
	{
		if (CompositeTarget?.Value != null)
		{
			SpriteBatch spriteBatch = Main.spriteBatch;

			//spriteBatch.End();
			//spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.Transform);

			int index = Array.FindIndex(ParticleRenderers.Renderers, x => x == renderer);
			Rectangle crop = CompositeTarget.Value.Frame(1, RendererCount, 0, index);

			spriteBatch.Draw(CompositeTarget.Value, Vector2.Zero, crop, Color.White, 0f, Vector2.Zero, 2f, 0f, 0f);

			//spriteBatch.End();
			//spriteBatch.BeginDefault();
		}
	}
}

public class CompositeSmoke : Particle, CompositeRenderer.ICompositeRendering
{
	public readonly Action<Particle> Action;

	public Entity Parent { get; private set; }
	public Color Color { get; set; }
	public int Variant { get; set; }

	private readonly bool _addLight;
	private readonly bool _addBloom;
	private readonly float _bloomOpacity;

	private Vector2 _offset; //Offset only when attached

	public CompositeSmoke(Vector2 position, Vector2 velocity, Color color, int maxTime, bool addLight = true, bool addBloom = true, Action<Particle> extraUpdateAction = null, float bloomOpacity = 0.08f)
	{
		Position = position;
		Velocity = velocity;
		Rotation = 0f;
		MaxTime = maxTime;

		_addLight = addLight;
		_addBloom = addBloom;
		_bloomOpacity = bloomOpacity;

		Color = color;
		Variant = Main.rand.Next(3); //Choose a random large variant by default
		Action = extraUpdateAction;
	}

	public CompositeSmoke AttachTo(Entity entity)
	{
		Parent = entity;
		_offset = entity.Center - Position;

		return this;
	}

	public override void Update(ref ParticleRendererSettings settings)
	{
		base.Update(ref settings);

		if (Parent != null)
		{
			Position = Parent.Center + _offset;
			_offset -= Velocity;
		}

		Velocity *= 0.98f;

		if (_addLight)
			Lighting.AddLight(Position, Color.ToVector3() * (1f - Progress));

		Action?.Invoke(this);
	}

	public void TargetDraw(SpriteBatch spriteBatch, Color color, int layer)
	{
		Texture2D texture = Texture;
		Rectangle frame = Texture.Frame(6, 5, Variant, (int)MathHelper.Lerp(0, 5, Progress));
		Vector2 position = Position - Main.screenPosition + Vector2.UnitY * Main.screenHeight * layer;
		float progress = Progress;
		float fadeOut = 1f;
		
		if (progress < 0.1f)
			fadeOut = progress / 0.1f;

		if (progress > 0.5f)
			fadeOut = 1f - (progress - 0.5f) / 0.5f;

		IDrawPixelated.PixelateDrawPosition(ref position);
		spriteBatch.Draw(texture, position, frame, color * fadeOut, Rotation, frame.Size() / 2, Scale / 2, SpriteEffects.None, 0);
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
			spritebatch.Draw(bloom, Position + settings.AnchorPosition, null, Color.Additive() * _bloomOpacity * fadeOut, Rotation, bloom.Size() / 2, Scale * 0.5f, SpriteEffects.None, 0);

		CompositeRenderer.CompositeItems.Add(this);
	}
}