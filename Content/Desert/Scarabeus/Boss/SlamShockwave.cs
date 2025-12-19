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
		Projectile.Size = new(160, 64);
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

		float topOffset = Projectile.height * TOP_OFFSET_RATIO * -Projectile.ai[0];

		var square = new SquarePrimitive
		{
			Color = Color.White,
			Length = Projectile.width,
			Height = Projectile.height,
			BottomPosOffset = -topOffset
		};

		square.SetTopPosition(Projectile.Top + Vector2.UnitX * topOffset);

		PrimitiveRenderer.DrawPrimitiveShape(square, shockwaveEffect);

		return false;
	}
}