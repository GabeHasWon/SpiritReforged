using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Particles;

public class SharpStarParticle : Particle
{
	private Color starColor;
	private Color bloomColor;
	private float progress;

	private bool addLight = true;
	private float _scaleFactor;

	private readonly float rotSpeed;
	private readonly Action<Particle> _action;

	public SharpStarParticle(Vector2 position, Vector2 velocity, Color StarColor, Color BloomColor, float scale, int maxTime, float rotationSpeed = 1f, Action<Particle> extraUpdateAction = null, bool AddLight = true, float scaleFactor = 0.05f)
	{
		Position = position;
		Velocity = velocity;
		starColor = StarColor.Additive();
		bloomColor = BloomColor.Additive();
		Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
		Scale = scale;
		MaxTime = maxTime;
		rotSpeed = rotationSpeed;
		_action = extraUpdateAction;
		addLight = AddLight;

		_scaleFactor = scaleFactor;
	}

	public SharpStarParticle(Vector2 position, Vector2 velocity, Color color, float scale, int maxTime, float rotationSpeed = 1f, Action<Particle> extraUpdateAction = null, bool AddLight = true, float scaleFactor = 0.05f) : this(position, velocity, color, color, scale, maxTime, rotationSpeed, extraUpdateAction, AddLight, scaleFactor) { }

	public override void Update(ref ParticleRendererSettings settings)
	{
		base.Update(ref settings);
		progress = (float)Math.Sin(Progress * MathHelper.Pi);

		if (addLight)
			Lighting.AddLight(Position, bloomColor.R / 255f * progress, bloomColor.G / 255f * progress, bloomColor.B / 255f * progress);

		Velocity *= 0.98f;
		Rotation += rotSpeed * progress * (Velocity.X > 0 ? 0.07f : -0.07f);

		_action?.Invoke(this);
	}

	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
	{
		Texture2D basetexture = Texture;
		Texture2D bloomtexture = AssetLoader.LoadedTextures["Bloom"].Value;
		
		float scale = Scale + (float)Math.Sin(TimeActive * 0.5f) * _scaleFactor;

		spriteBatch.Draw(bloomtexture, Position + settings.AnchorPosition, null, bloomColor * 0.25f, 0, bloomtexture.Size() / 2, scale * 0.66f * progress, SpriteEffects.None, 0);
		spriteBatch.Draw(basetexture, Position + settings.AnchorPosition, null, starColor, Rotation, basetexture.Size() / 2, scale / 2 * progress, SpriteEffects.None, 0);
	}
}
