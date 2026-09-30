using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Particles;

public class StarParticle : Particle
{
	private Color starColor;
	private Color bloomColor;
	private float opacity;

	private readonly float rotSpeed;
	private readonly Action<Particle> _action;

	public Color Color { get; protected set; }

	public StarParticle(Vector2 position, Vector2 velocity, Color StarColor, Color BloomColor, float scale, int maxTime, float rotationSpeed = 1f, Action<Particle> extraUpdateAction = null)
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
	}

	public StarParticle(Vector2 position, Vector2 velocity, Color color, float scale, int maxTime, float rotationSpeed = 1f, Action<Particle> extraUpdateAction = null) : this(position, velocity, color, color, scale, maxTime, rotationSpeed, extraUpdateAction) { }

	public override void Update(ref ParticleRendererSettings settings)
	{
		opacity = (float)Math.Sin(Progress * MathHelper.Pi);
		Color = bloomColor * opacity;
		Lighting.AddLight(Position, Color.R / 255f, Color.G / 255f, Color.B / 255f);
		Velocity *= 0.98f;
		Rotation += rotSpeed * (Velocity.X > 0 ? 0.07f : -0.07f);

		_action?.Invoke(this);

		base.Update(ref settings);
	}

	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
	{
		Texture2D basetexture = Texture;
		Texture2D bloomtexture = AssetLoader.LoadedTextures["Bloom"].Value;

		spriteBatch.Draw(bloomtexture, Position + settings.AnchorPosition, null, bloomColor * opacity * 0.5f, 0, bloomtexture.Size() / 2, Scale / 2, SpriteEffects.None, 0);

		spriteBatch.Draw(basetexture, Position + settings.AnchorPosition, null, starColor * opacity * 0.5f, Rotation * 1.5f, basetexture.Size() / 2, Scale * 0.75f, SpriteEffects.None, 0);
		spriteBatch.Draw(basetexture, Position + settings.AnchorPosition, null, starColor * opacity * 0.5f, -Rotation * 1.5f, basetexture.Size() / 2, Scale * 0.75f, SpriteEffects.None, 0);

		spriteBatch.Draw(basetexture, Position + settings.AnchorPosition, null, starColor * opacity, Rotation, basetexture.Size() / 2, Scale, SpriteEffects.None, 0);
	}
}
