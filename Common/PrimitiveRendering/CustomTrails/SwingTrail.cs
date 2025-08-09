using SpiritReforged.Common.Easing;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using static SpiritReforged.Common.Easing.EaseFunction;

namespace SpiritReforged.Common.PrimitiveRendering.CustomTrails;

public struct SwingTrailParameters(float radians, float rotation, float distance, float width)
{
	public readonly Color GetSecondaryColor => SecondaryColor ?? Color;
	public readonly float GetMaxWidth => MaxWidth ?? Width;
	public readonly float GetMaxDist => MaxDistance ?? Distance;

	public Color Color = Color.White;
	public Color? SecondaryColor = null;

	public float Radians = radians;
	public float Intensity = 1f;
	public float TrailLength = 1f;
	public float DissolveThreshold = 0.9f;
	public float Rotation = rotation;

	public float Distance = distance;
	public float Width = width;
	public float? MaxDistance = null;
	public float? MaxWidth = null;

	public EaseFunction DistanceEasing = Linear;

	public bool UseLightColor = true;
}

public class SwingTrail(Projectile projectile, SwingTrailParameters parameters, Func<Projectile, float> SwingProgress, Func<SwingTrail, Effect> ShaderParams, TrailLayer layer = TrailLayer.UnderProjectile) : BaseTrail(projectile, layer)
{
	private const int TIMELEFT_MAX = 30;

	public SwingTrailParameters Parameters { get; } = parameters;
	public Projectile Projectile { get; } = projectile;

	public float DissolveProgress => _timeLeft / (float)TIMELEFT_MAX;

	public string EffectPass = "CleanStreakPass";

	private Player Owner => Main.player[Projectile.owner];

	private int Direction => Main.player[Projectile.owner].direction;

	private Vector2 _center;

	private int _timeLeft = TIMELEFT_MAX;

	private float _swingProgress;

	public override void Dissolve()
	{
		_timeLeft--;

		if (_timeLeft == 0)
			Dead = true;
	}

	public override void Update()
	{
		_swingProgress = SwingProgress(Projectile);
		_center = Owner.MountedCenter;
		MyProjectile = Projectile;

		if (_swingProgress > Parameters.DissolveThreshold)
		{
			_timeLeft -= 5;
			StartDissolve();
		}
	}

	public float GetSwingProgress() => _swingProgress;

	public override void Draw(Effect effect, BasicEffect _, GraphicsDevice device)
	{
		if (Dead || _timeLeft <= 1) 
			return;

		effect = ShaderParams(this);

		float minDist = Parameters.Distance;
		float maxDist = Parameters.GetMaxDist;
		float minWidth = Parameters.Width;
		float maxWidth = Parameters.GetMaxWidth;

		Vector2 pos = _center - Main.screenPosition;
		var slash = new PrimitiveSlashArc
		{
			BasePosition = pos,
			MinDistance = minDist,
			MaxDistance = maxDist,
			Width = minWidth,
			MaxWidth = maxWidth,
			AngleRange = new Vector2(Parameters.Radians / 2 * Direction, -Parameters.Radians / 2 * Direction) * -1,
			DirectionUnit = (Direction * Parameters.Rotation).ToRotationVector2().RotatedBy(Direction < 0 ? MathHelper.Pi : 0),
			Color = Color.White,
			UseLightColor = Parameters.UseLightColor,
			DistanceEase = Parameters.DistanceEasing,
			SlashProgress = _swingProgress,
			RectangleCount = 70
		};

		PrimitiveRenderer.DrawPrimitiveShape(slash, effect, EffectPass);
	}

	public static Effect BasicSwingShaderParams(SwingTrail swingTrail)
	{
		Effect effect;
		effect = AssetLoader.LoadedShaders["SwingTrails"].Value;
		effect.Parameters["baseColorLight"].SetValue(swingTrail.Parameters.Color.ToVector4());
		effect.Parameters["baseColorDark"].SetValue(swingTrail.Parameters.GetSecondaryColor.ToVector4());

		effect.Parameters["trailLength"].SetValue(swingTrail.Parameters.TrailLength * EaseQuadIn.Ease(swingTrail.DissolveProgress));
		effect.Parameters["taperStrength"].SetValue(0.25f);
		effect.Parameters["fadeStrength"].SetValue(0.5f);

		effect.Parameters["progress"].SetValue(swingTrail.GetSwingProgress());
		effect.Parameters["intensity"].SetValue(swingTrail.Parameters.Intensity * EaseCubicIn.Ease(swingTrail.DissolveProgress));

		return effect;
	}

	public static Effect NoiseSwingShaderParams(SwingTrail swingTrail, string texturePath, Vector2 coordMods)
	{
		Effect effect;
		effect = AssetLoader.LoadedShaders["SwingTrails"].Value;
		effect.Parameters["baseTexture"].SetValue(AssetLoader.LoadedTextures[texturePath].Value);
		effect.Parameters["baseColorLight"].SetValue(swingTrail.Parameters.Color.ToVector4());
		effect.Parameters["baseColorDark"].SetValue(swingTrail.Parameters.GetSecondaryColor.ToVector4());

		effect.Parameters["coordMods"].SetValue(coordMods);
		effect.Parameters["trailLength"].SetValue(swingTrail.Parameters.TrailLength * EaseQuadIn.Ease(swingTrail.DissolveProgress));
		effect.Parameters["taperStrength"].SetValue(0.5f);
		effect.Parameters["fadeStrength"].SetValue(3);
		effect.Parameters["textureExponent"].SetValue(new Vector2(0.6f, 3));

		effect.Parameters["timer"].SetValue(0.5f * Main.GlobalTimeWrappedHourly / coordMods.X);
		effect.Parameters["progress"].SetValue(swingTrail.GetSwingProgress());
		effect.Parameters["intensity"].SetValue(swingTrail.Parameters.Intensity * EaseCubicIn.Ease(swingTrail.DissolveProgress));
		swingTrail.EffectPass = "NoiseStreakPass";

		return effect;
	}

	public static Effect FireSwingShaderParams(SwingTrail swingTrail, Vector2 coordMods)
	{
		Effect effect;
		effect = AssetLoader.LoadedShaders["SwingTrails"].Value;
		effect.Parameters["baseTexture"].SetValue(AssetLoader.LoadedTextures["fbmNoise"].Value);
		effect.Parameters["baseColorLight"].SetValue(swingTrail.Parameters.Color.ToVector4());
		effect.Parameters["baseColorDark"].SetValue(swingTrail.Parameters.GetSecondaryColor.ToVector4());

		effect.Parameters["coordMods"].SetValue(coordMods);
		effect.Parameters["trailLength"].SetValue(swingTrail.Parameters.TrailLength * swingTrail.DissolveProgress);
		effect.Parameters["taperStrength"].SetValue(0.85f);
		effect.Parameters["fadeStrength"].SetValue(0.75f + 6 * EaseCubicOut.Ease(1 - swingTrail.DissolveProgress));
		effect.Parameters["textureExponent"].SetValue(Vector2.Lerp(new Vector2(0.5f, 3), Vector2.Zero, EaseCircularIn.Ease(1 - swingTrail.DissolveProgress)));

		float globalTimer = (Main.GlobalTimeWrappedHourly / coordMods.X);
		float scrollSpeed = MathHelper.Lerp(-2f, 0f, EaseQuadOut.Ease(1 - swingTrail.DissolveProgress));
		effect.Parameters["timer"].SetValue(scrollSpeed * globalTimer);
		effect.Parameters["progress"].SetValue(swingTrail.GetSwingProgress());
		effect.Parameters["intensity"].SetValue(swingTrail.Parameters.Intensity * EaseCubicIn.Ease(swingTrail.DissolveProgress));
		swingTrail.EffectPass = "FlameTrailPass";

		return effect;
	}
}