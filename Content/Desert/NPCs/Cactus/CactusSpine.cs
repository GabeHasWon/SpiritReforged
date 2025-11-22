using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;

namespace SpiritReforged.Content.Desert.NPCs.Cactus;

public class CactusSpine : ModProjectile
{
	public const int NumColumns = 4;

	private bool _spawned = true;
	public ref float Frame => ref Projectile.ai[0];

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = 20;
		ProjectileID.Sets.TrailingMode[Type] = 2;
		Main.projFrames[Type] = 3; //Rows
	}

	public override void SetDefaults()
	{
		Projectile.Size = new(6);
		Projectile.DamageType = DamageClass.Magic;
		Projectile.hostile = true;
		Projectile.penetrate = -1;
		Projectile.extraUpdates = 3;
		Projectile.timeLeft = 300;
		Projectile.scale = Main.rand.NextFloat(0.7f, 1.1f);
	}

	public override void AI()
	{
		if (_spawned) //Create a trail
		{
			if (!Main.dedServ)
				TrailSystem.ProjectileRenderer.CreateTrail(Projectile, new VertexTrail(new LightColorTrail(new Color(87, 35, 88) * 0.2f, Color.Transparent), new RoundCap(), new EntityTrailPosition(Projectile), new DefaultShader(), 8 * Projectile.scale, 75));

			_spawned = false;
		}

		Projectile.velocity.Y += 0.02f;
		Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Projectile.frame = (int)Frame;

		Texture2D texture = TextureAssets.Projectile[Type].Value;
		int column = Projectile.frame / Main.projFrames[Type];
		int row = Projectile.frame % Main.projFrames[Type];
		Rectangle source = texture.Frame(NumColumns, Main.projFrames[Type], column, row, -2, -2);

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), source, Projectile.GetAlpha(lightColor), Projectile.rotation, source.Size() / 2, Projectile.scale, default);
		return false;
	}
}