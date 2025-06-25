using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;

namespace SpiritReforged.Content.Particles;
public class FireParticle : DissipatingImage
{
	private readonly EaseFunction _acceleration;
	private readonly Vector2 _initialVel;

	public override float FinalScaleMod => 0.5f;
	public override string DistortNoiseString => "swirlNoise";
	public override ParticleLayer DrawLayer => ParticleLayer.AbovePlayer;
	public override bool Pixellate => true;
	public override float DissolveAmount => 1;
	public override EaseFunction DistortEasing => EaseFunction.EaseCubicOut;

	public FireParticle(Vector2 position, Vector2 velocity, Color[] colors, float intensity, float scale, EaseFunction acceleration, int maxTime) : base(position, colors[0], Main.rand.NextFloatDirection(), scale, Main.rand.NextFloat(0.1f, 0.3f), "Fire" + Main.rand.Next(1, 3), new(Main.rand.NextFloat(0.2f, 0.5f)), new(1.25f, 1f), maxTime)
	{
		Velocity = velocity;
		SecondaryColor = colors[1];
		TertiaryColor = colors[2];
		Intensity = intensity;
		_initialVel = velocity;
		_acceleration = acceleration;
	}

	public override void Update()
	{
		base.Update();
		Velocity = (1 - _acceleration.Ease(Progress)) * Vector2.Lerp(_initialVel, -Vector2.UnitY, EaseFunction.EaseQuadOut.Ease(Progress));
	}

	public override void CustomDraw(SpriteBatch spriteBatch)
	{
		Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;
		float scale = _scaleMod * Scale * 512 / 150f;
		Main.EntitySpriteDraw(bloom, Position - Main.screenPosition, null, (SecondaryColor ?? Color).Additive() * EaseFunction.EaseCubicIn.Ease(1 - Progress) * 0.33f * Intensity, 0, bloom.Size() / 2, scale / 2, SpriteEffects.None);

		base.CustomDraw(spriteBatch);
	}
}
