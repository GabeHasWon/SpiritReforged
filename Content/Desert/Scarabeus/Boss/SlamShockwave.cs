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
		Effect shockwaveEffect = AssetLoader.LoadedShaders["GroundShockwave"].Value;

		shockwaveEffect.Parameters["uTexture"].SetValue(AssetLoader.LoadedTextures["vnoise"].Value);
		shockwaveEffect.Parameters["textureStretch"].SetValue(new Vector2(4, 0.33f));
		shockwaveEffect.Parameters["pixelDimensions"].SetValue(new Vector2(Projectile.width, Projectile.height) / 4);

		shockwaveEffect.Parameters["uColor"].SetValue(Color.LightGoldenrodYellow.ToVector4());
		shockwaveEffect.Parameters["uColor2"].SetValue(Color.SandyBrown.ToVector4());
		shockwaveEffect.Parameters["uColor3"].SetValue(Color.SaddleBrown.ToVector4());

		shockwaveEffect.Parameters["finalIntensityMod"].SetValue(1.5f);
		shockwaveEffect.Parameters["numColors"].SetValue(16);
		shockwaveEffect.Parameters["scroll"].SetValue(new Vector2(Projectile.ai[0] * Projectile.timeLeft / 120f, Projectile.timeLeft / 120f));
		shockwaveEffect.Parameters["progress"].SetValue(1 - EaseFunction.EaseCircularIn.Ease(Projectile.timeLeft / 60f));

		float topOffset = Projectile.height * TOP_OFFSET_RATIO * Projectile.ai[0] * 1.5f;

		var square = new SquarePrimitive
		{
			Color = lightColor * EaseFunction.EaseQuadOut.Ease(EaseFunction.EaseCircularOut.Ease(Projectile.timeLeft / 60f)),
			Length = Projectile.width,
			Height = Projectile.height,
			BottomPosOffset = -topOffset
		};

		square.SetTopPosition(-Main.screenPosition + Projectile.Top + Vector2.UnitX * topOffset - Vector2.UnitY * 120);

		PrimitiveRenderer.DrawPrimitiveShape(square, shockwaveEffect);

		return false;
	}
}