namespace SpiritReforged.Common.Particle;

/// <summary>
/// Represents a particle with a position, velocity, rotation, scale and transparency.
/// TL;DR: Better dust.
/// </summary>
public class Particle
{
	public int ID; // you don't have to use this
	public int Type; // you don't have to use this
	public Vector2 Position;
	public Vector2 Velocity;
	public Vector2 Origin;
	public Color Color;
	public float Rotation;
	public float Scale;
	public uint TimeActive;
	public int MaxTime;

	public Texture2D Texture => ParticleHandler.GetTexture(Type);
	public float Progress => TimeActive / (float)MaxTime;

	/// <summary>
	/// Determines whether the particle is drawn using its default drawing method, the default drawing method with additive color blending, or a custom drawing type.
	/// If set to Custom, the CustomDraw method will be called.
	/// </summary>
	public virtual ParticleDrawType DrawType => ParticleDrawType.DefaultAlphaBlend;

	/// <summary>
	/// Determines which layer the particle is drawn on, i.e. when the drawing is called relative to other vanilla entities.
	/// Defaults to before projectiles are drawn.
	/// </summary>
	public virtual ParticleLayer DrawLayer => ParticleLayer.BelowProjectile;

	/// <summary>
	/// Call this when you want to clear your particle and remove it from the world.
	/// </summary>
	public void Kill()
	{
		OnKill();
		ParticleHandler.DeleteParticleAtIndex(ID);
	}		

	/// <summary>
	/// Called every tick. Update your particle in this method.
	/// Particle velocity is automatically added to the particle position for you, and TimeAlive is incremented.
	/// </summary>
	public virtual void Update() { }

	/// <summary>
	/// Called when a particle is killed
	/// </summary>
	public virtual void OnKill() { }

	/// <summary>
	/// Allows you to do custom drawing for your particle. Only called if Particle.UseCustomDrawing is true.
	/// </summary>
	public virtual void CustomDraw(SpriteBatch spriteBatch) { }
}
