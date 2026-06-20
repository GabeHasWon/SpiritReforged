using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using Terraria.Map;

namespace SpiritReforged.Content.Particles;
public class SharpStarParticle : Particle
{
	private Color starColor;
	private Color bloomColor;
	private float progress;

	private bool addLight = true;

	private readonly float rotSpeed;
	private readonly Action<Particle> _action;
	public ParticleLayer Layer { get; set; } = ParticleLayer.BelowProjectile;
	public override ParticleLayer DrawLayer => Layer;

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;

	public SharpStarParticle(Vector2 position, Vector2 velocity, Color StarColor, Color BloomColor, float scale, int maxTime, float rotationSpeed = 1f, Action<Particle> extraUpdateAction = null, bool AddLight = true)
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
	}

	public SharpStarParticle(Vector2 position, Vector2 velocity, Color color, float scale, int maxTime, float rotationSpeed = 1f, Action<Particle> extraUpdateAction = null, bool AddLight = true) : this(position, velocity, color, color, scale, maxTime, rotationSpeed, extraUpdateAction, AddLight) { }

	public override void Update()
	{
		progress = (float)Math.Sin(Progress * MathHelper.Pi);
		Color = bloomColor;
		if (addLight)
			Lighting.AddLight(Position, Color.R / 255f * progress, Color.G / 255f * progress, Color.B / 255f * progress);
		Velocity *= 0.98f;
		Rotation += rotSpeed * progress * (Velocity.X > 0 ? 0.07f : -0.07f);

		_action?.Invoke(this);
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		Texture2D basetexture = ParticleHandler.GetTexture(Type);
		Texture2D bloomtexture = AssetLoader.LoadedTextures["Bloom"].Value;
		
		float scale = Scale + (float)Math.Sin(TimeActive * 0.5f) * 0.05f;

		spriteBatch.Draw(bloomtexture, Position - Main.screenPosition, null, bloomColor * 0.25f, 0, bloomtexture.Size() / 2, scale * 0.66f * progress, SpriteEffects.None, 0);

		spriteBatch.Draw(basetexture, Position - Main.screenPosition, null, starColor, Rotation, basetexture.Size() / 2, scale / 2 * progress, SpriteEffects.None, 0);
	}
}
