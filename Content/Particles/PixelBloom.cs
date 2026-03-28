using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;

namespace SpiritReforged.Content.Particles;
public class PixelBloom : Particle
{
	private Color startColor;
	private Color endColor;
	private float progress;

	private readonly Action<Particle> _action;

	public override ParticleDrawType DrawType => ParticleDrawType.Custom;

	public PixelBloom(Vector2 position, Vector2 velocity, Color StartColor, Color EndColor, float scale, int maxTime, Action<Particle> extraUpdateAction = null)
	{
		Position = position;
		Velocity = velocity;
		startColor = StartColor.Additive();
		endColor = EndColor.Additive();
		Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
		Scale = scale;
		MaxTime = maxTime;
		_action = extraUpdateAction;
	}

	public PixelBloom(Vector2 position, Vector2 velocity, Color color, float scale, int maxTime, Action<Particle> extraUpdateAction = null) : this(position, velocity, color, color, scale, maxTime, extraUpdateAction) { }

	public override void Update()
	{
		progress = (float)Math.Sin(Progress * MathHelper.Pi);
		Lighting.AddLight(Position, Color.R / 255f, Color.G / 255f, Color.B / 255f);
		Velocity *= 0.98f;

		_action?.Invoke(this);
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		Texture2D basetexture = ParticleHandler.GetTexture(Type);
		Texture2D bloomtexture = AssetLoader.LoadedTextures["Bloom"].Value;

		Color color = Color.Lerp(startColor, endColor, 1f - progress);

		spriteBatch.Draw(bloomtexture, Position - Main.screenPosition, null, color * 0.25f, 0, bloomtexture.Size() / 2, Scale * 0.2f * progress, SpriteEffects.None, 0);
		
		spriteBatch.Draw(bloomtexture, Position - Main.screenPosition, null, Color.White.Additive() * 0.1f, 0, bloomtexture.Size() / 2, Scale * 0.1f * progress, SpriteEffects.None, 0);

		spriteBatch.Draw(basetexture, Position - Main.screenPosition, null, Color.White * 0.5f, 0, basetexture.Size() / 2, Scale * progress, SpriteEffects.None, 0);
	}
}
