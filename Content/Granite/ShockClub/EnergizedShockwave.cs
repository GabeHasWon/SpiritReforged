using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using SpiritReforged.Common.PrimitiveRendering;
using Terraria.DataStructures;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using SpiritReforged.Common.Easing;

namespace SpiritReforged.Content.Granite.ShockClub;

public class EnergizedShockwave : ModProjectile
{
	private float ScaleX
	{
		get => Projectile.ai[0];
		set => Projectile.ai[0] = value;
	}
	private float ScaleY
	{
		get => Projectile.ai[1];
		set => Projectile.ai[1] = value;
	}

	private readonly int timeLeftMax = 80;

	private readonly float maxScaleX = 2.3f;
	private readonly float maxScaleY = 1.3f;

	public override string Texture => AssetLoader.EmptyTexture;

	public override void SetDefaults()
	{
		Projectile.width = 20;
		Projectile.height = 100;
		Projectile.aiStyle = 0;
		Projectile.penetrate = -1;
		Projectile.ignoreWater = true;
		Projectile.timeLeft = timeLeftMax;
		Projectile.friendly = true;
		DrawOriginOffsetY = 8;
		Projectile.extraUpdates = 1;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
	}

	public override void OnSpawn(IEntitySource source) { }

	public override void AI()
	{
		if (Projectile.timeLeft == timeLeftMax)
			ScaleX = maxScaleX; //Just spawned

		if (ScaleY < maxScaleY)
			ScaleY += 0.1f;

		ScaleX = MathHelper.Max(0, ScaleX - (float)(maxScaleX / (timeLeftMax + 2)));
		Projectile.Opacity = Projectile.timeLeft / (float)timeLeftMax;

		Projectile.velocity.Y = 8;
		Projectile.velocity = Vector2.Lerp(Projectile.velocity, new Vector2(0, Projectile.velocity.Y), 0.035f);

		Collision.StepUp(ref Projectile.position, ref Projectile.velocity, Projectile.width, Projectile.height, ref Projectile.stepSpeed, ref Projectile.gfxOffY); //Automatically move up 1 tile tall walls

		int heightMod = 2;
		Vector2 basePos = Projectile.Center + Vector2.UnitY * (Projectile.height - heightMod) * 0.5f;
		if(Main.rand.NextFloat() < EaseFunction.EaseQuadOut.Ease(Projectile.timeLeft / (float)timeLeftMax))
		{
			Vector2 spawnPos = basePos + Vector2.UnitX * Main.rand.NextFloat(-1, 1) * Projectile.width;
			static void DelegateAction(Particle p) => p.Velocity *= 0.8f;

			ParticleHandler.SpawnParticle(new GlowParticle(spawnPos, -Vector2.UnitY * Main.rand.NextFloat(6), new Color(140, 200, 255), Color.Cyan, Main.rand.NextFloat(0.2f, 0.4f), Main.rand.Next(20, 30), 6, DelegateAction));
		}

		Lighting.AddLight(basePos, Color.LightCyan.ToVector3() * Projectile.Opacity);
		Lighting.AddLight(Projectile.Center, Color.Cyan.ToVector3() / 2 * Projectile.Opacity);
	}

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		//Projectile.velocity.X = oldVelocity.X;
		return false;
	}

	public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
	{
		fallThrough = false;
		return base.TileCollideStyle(ref width, ref height, ref fallThrough, ref hitboxCenterFrac);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D ray = AssetLoader.LoadedTextures["Ray"].Value;
		var scale = new Vector2(ScaleX, ScaleY);

		Vector2 center = Projectile.Center + Vector2.UnitY * Projectile.gfxOffY;
		Vector2 position = Projectile.position + new Vector2(Projectile.width / 2, Projectile.height + Projectile.gfxOffY);
		var origin = new Vector2(ray.Width / 2, ray.Height);

		Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;
		Main.EntitySpriteDraw(bloom, center + Vector2.UnitY * (Projectile.height / 2) - Main.screenPosition, null, Color.LightBlue.Additive() * .4f, 0, bloom.Size() / 2, ScaleX * .25f, SpriteEffects.None, 0);

		Effect blurEffect = AssetLoader.LoadedShaders["BlurLine"];
		var blurLine = new SquarePrimitive()
		{
			Position = center + Vector2.UnitY * (Projectile.height / 2) - Main.screenPosition,
			Height = 125 * ScaleX,
			Length = 18 * ScaleX,
			Rotation = 1.57f,
			Color = Color.LightBlue.Additive()
		};
		PrimitiveRenderer.DrawPrimitiveShape(blurLine, blurEffect);

		for (int i = 0; i < 3; i++)
		{
			Color color = i switch
			{
				0 => Color.Blue,
				1 => Color.Cyan,
				_ => new Color(140, 200, 255)
			};

			Vector2 lerp = (((float)Main.timeForVisualEffects + i * 5) / 30).ToRotationVector2() * .03f;
			Main.EntitySpriteDraw(ray, position - Main.screenPosition, null, (color with { A = 0 }) * Projectile.Opacity, Projectile.rotation, origin, scale + lerp, SpriteEffects.FlipVertically, 0);
		}

		return false;
	}
}
