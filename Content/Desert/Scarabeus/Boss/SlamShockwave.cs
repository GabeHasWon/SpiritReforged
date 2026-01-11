using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;

namespace SpiritReforged.Content.Desert.Scarabeus.Boss;

public class SlamShockwave : ModProjectile
{
	private const int MAX_TIMELEFT = 60;

	private const float TOP_OFFSET_RATIO = 0.25f;

	public override string Texture => AssetLoader.EmptyTexture;

	public override void SetStaticDefaults() => base.SetStaticDefaults();

	public override void SetDefaults()
	{
		Projectile.Size = new(640, 320);
		Projectile.hostile = true;
		Projectile.tileCollide = false;
		Projectile.hide = true;
		Projectile.penetrate = -1;
		Projectile.timeLeft = MAX_TIMELEFT;
	}

	public override void AI()
	{

	}

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		//parallelogram collision here
		return false;
	}

	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => behindNPCsAndTiles.Add(index);

	public override bool PreDraw(ref Color lightColor)
	{
		Effect shockwaveEffect = Mod.Assets.Request<Effect>("Assets/Shaders/GroundShockwave", AssetRequestMode.ImmediateLoad).Value;

		shockwaveEffect.Parameters["uTexture"].SetValue(AssetLoader.LoadedTextures["vnoise"].Value);
		shockwaveEffect.Parameters["textureStretch"].SetValue(new Vector2(3, 0.16f) * 1.5f);
		shockwaveEffect.Parameters["pixelDimensions"].SetValue(new Vector2(Projectile.width, Projectile.height) / 4);

		shockwaveEffect.Parameters["uColor"].SetValue(Color.LightGoldenrodYellow.ToVector4());
		shockwaveEffect.Parameters["uColor2"].SetValue(Color.SandyBrown.ToVector4());
		shockwaveEffect.Parameters["uColor3"].SetValue(Color.SaddleBrown.ToVector4());

		float progress = EaseFunction.EaseCircularOut.Ease(1 - Projectile.timeLeft / (float)MAX_TIMELEFT);
		shockwaveEffect.Parameters["finalIntensityMod"].SetValue(1.3f);
		shockwaveEffect.Parameters["numColors"].SetValue(12);
		shockwaveEffect.Parameters["scroll"].SetValue(new Vector2(-Projectile.ai[0] * progress * 0.66f, -progress * 0.66f));
		shockwaveEffect.Parameters["progress"].SetValue(progress);
		shockwaveEffect.Parameters["direction"].SetValue(Projectile.ai[0]);

		float topOffset = Projectile.height * TOP_OFFSET_RATIO * Projectile.ai[0] * 2.5f;

		var square = new SquarePrimitive
		{
			Color = lightColor,
			Length = Projectile.width,
			Height = Projectile.height,
			BottomPosOffset = -topOffset
		};

		square.SetTopPosition(-Main.screenPosition + Projectile.Top + Vector2.UnitX * topOffset);

		PrimitiveRenderer.DrawPrimitiveShape(square, shockwaveEffect);

		return false;
	}
}