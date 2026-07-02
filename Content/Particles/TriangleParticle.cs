using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;

namespace SpiritReforged.Content.Particles;
public class TriangleParticle : Particle
{
	private Color bloomColor;
	private bool addLight = true;

	private int _variant;

	private readonly Action<Particle> _action;
	public ParticleLayer Layer { get; set; } = ParticleLayer.BelowProjectile;
	public override ParticleLayer DrawLayer => Layer;

	public override ParticleDrawType DrawType => ParticleDrawType.CustomBatchedAdditiveBlend;

	public TriangleParticle(Vector2 position, Vector2 velocity, Color color, Color BloomColor, float scale, int maxTime, Action<Particle> extraUpdateAction = null, bool AddLight = true)
	{
		Position = position;
		Velocity = velocity;
		Color = color;
		bloomColor = BloomColor;

		Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
		Scale = scale;
		MaxTime = maxTime;
		_action = extraUpdateAction;
		addLight = AddLight;

		_variant = Main.rand.Next(4);
	}

	public override void Update()
	{
		if (addLight)
			Lighting.AddLight(Position, Color.R / 255f * (1f - Progress), Color.G / 255f * (1f - Progress), Color.B / 255f * (1f - Progress));

		Velocity *= 0.95f;
		Rotation += Velocity.Length() * 0.03f;

		_action?.Invoke(this);
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		Texture2D basetexture = ParticleHandler.GetTexture(Type);
		Texture2D bloomtexture = AssetLoader.LoadedTextures["Bloom"].Value;

		float progress = EaseBuilder.EaseQuinticOut.Ease(1f - Progress);

		float scale = Scale * progress;

		var frame = basetexture.Frame(1, 4, 0, _variant);

		spriteBatch.Draw(bloomtexture, Position - Main.screenPosition, null, bloomColor * 0.35f, 0, bloomtexture.Size() / 2, scale * 0.33f, SpriteEffects.None, 0);

		spriteBatch.Draw(basetexture, Position - Main.screenPosition, frame, Color, Rotation, frame.Size() / 2, scale, SpriteEffects.None, 0);

		spriteBatch.Draw(bloomtexture, Position - Main.screenPosition, null, Color * 0.25f, 0, bloomtexture.Size() / 2, scale * 0.25f, SpriteEffects.None, 0);

	}
}
