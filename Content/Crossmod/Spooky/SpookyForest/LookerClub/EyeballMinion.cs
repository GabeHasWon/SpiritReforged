using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Tiles;
using Terraria;
using Terraria.Audio;

namespace SpiritReforged.Content.Crossmod.Spooky.SpookyForest.LookerClub;
public class EyeballMinion : BaseMinion
{
	public EyeballMinion() : base(400, 600, new(12)) { }

	public ref float AiTimer => ref Projectile.ai[0];
	public ref float NoCollideTimer => ref Projectile.ai[1];

	private float eyeballRotation;
	private float targetEyeballRotation;

	public override void AbstractSetStaticDefaults()
	{
		ProjectileID.Sets.TrailingMode[Type] = 2;
		ProjectileID.Sets.TrailCacheLength[Type] = 5;
		Main.projFrames[Type] = 3;
	}

	public override void AbstractSetDefaults()
	{
		Projectile.minionSlots = 0;
		Projectile.DamageType = ModContent.GetInstance<HybridDamageClass>().Clone()
			.AddSubClass(new(DamageClass.Melee, 1f))
			.AddSubClass(new(DamageClass.Summon, 1f));

		Projectile.timeLeft = 600;
		Projectile.penetrate = 4;
		Projectile.tileCollide = true;

		Projectile.usesLocalNPCImmunity = false;
		Projectile.usesIDStaticNPCImmunity = true;
		Projectile.idStaticNPCHitCooldown = 8;

		Projectile.scale *= Main.rand.NextFloat(1.15f, 1.35f);
		Projectile.hide = true;
	}

	public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
	{
		fallThrough = false;
		return true;
	}

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		return false;
	}

	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
	{
		overPlayers.Add(index);
	}

	public override void IdleMovement(Player player)
	{
		float playerDist = Projectile.DistanceSQ(player.Center);

		Projectile.timeLeft -= 2;

		if (!Grounded())
		{
			Projectile.frame = 2;

			Projectile.velocity.Y += 0.35f;
			if (Projectile.velocity.Y > 16f)
				Projectile.velocity.Y = 16f;

			if (playerDist < 50 * 50) // slow down when near player
				Projectile.velocity.X *= 0.933f;

			Projectile.rotation = Utils.AngleLerp(Projectile.rotation, Projectile.velocity.ToRotation(), 0.1f);
			targetEyeballRotation = Projectile.velocity.ToRotation();

			if (Main.rand.NextBool(15))
				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(15f, 15f), DustID.Smoke, -Projectile.velocity * 0.5f, 175, default, 1f).noGravity = true;
		}
		else
		{
			Projectile.frame = 0;

			Projectile.velocity *= 0.95f;
			Projectile.velocity.Y = 0;

			AiTimer++;

			Projectile.frame = 0;

			if (AiTimer > 15 || AiTimer < 5)
				Projectile.frame = 1;

			if (AiTimer >= 20)
			{
				NoCollideTimer = 5;
				Jump(Player.Center - Vector2.UnitY * 20f, -Vector2.UnitY * 1.5f, 5.5f);
				AiTimer = 0;
			}

			Projectile.rotation = Utils.AngleLerp(Projectile.rotation, 0, 0.15f);
			targetEyeballRotation = Projectile.DirectionTo(player.Center).ToRotation();
		}
	}

	public override void TargettingBehavior(Player player, NPC target)
	{
		float targetDist = Projectile.DistanceSQ(target.Center);

		if (!Grounded())
		{
			Projectile.frame = 2;

			Projectile.velocity.Y += 0.35f;
			if (Projectile.velocity.Y > 16f)
				Projectile.velocity.Y = 16f;

			if (targetDist < 50 * 50) // slow down when near target
				Projectile.velocity.X *= 0.933f;

			Projectile.rotation = Utils.AngleLerp(Projectile.rotation, Projectile.velocity.ToRotation(), 0.1f);
			targetEyeballRotation = Projectile.velocity.ToRotation();

			if (Main.rand.NextBool(15))
				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(15f, 15f), DustID.Smoke, -Projectile.velocity * 0.5f, 175, default, 1f).noGravity = true;
		}
		else
		{
			Projectile.velocity *= 0.95f;
			Projectile.velocity.Y = 0;

			AiTimer++;

			Projectile.frame = 0;

			if (AiTimer > 15 || AiTimer < 5)
				Projectile.frame = 1;

			if (AiTimer >= 20)
			{
				NoCollideTimer = 5;
				Jump(target.Center - Vector2.UnitY * 20f, -Vector2.UnitY * 2.5f, 8f);
				AiTimer = 0;
			}

			Projectile.rotation = Utils.AngleLerp(Projectile.rotation, 0, 0.15f);
			targetEyeballRotation = Projectile.DirectionTo(target.Center).ToRotation();
		}
	}

	void Jump(Vector2 targetPosition, Vector2 extraVelocity, float speed)
	{
		SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing with { Volume = 0.5f, Pitch = 0.3f}, Projectile.Center);

		Projectile.velocity += Projectile.DirectionTo(targetPosition) * speed + extraVelocity;

		var easeFunction = EaseBuilder.EaseCubicOut;
		float ringWidth = 0.4f;
		int lifetime = 40;
		float zRotation = 0.9f;

		ParticleHandler.SpawnParticle(new TexturedPulseCircle(
			Projectile.Bottom,
			new Color(206, 206, 206) * 0.66f,
			Color.White * 0.66f,
			ringWidth,
			90,
			lifetime,
			"supPerlin",
			new Vector2(2, 3),
			easeFunction).WithSkew(zRotation, -1.33f * Math.Sign(Projectile.velocity.X)).UsesLightColor());

		ParticleHandler.SpawnParticle(new SmokeCloud(Projectile.Center + Main.rand.NextVector2Circular(15, 15), -Vector2.UnitY.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.33f, 1.25f), Color.LightGray * 0.65f, Main.rand.NextFloat(0.03f, 0.075f), EaseFunction.EaseQuadOut, 40 + Main.rand.Next(20))
		{
			Pixellate = true,
			PixelDivisor = 2
		});
	}

	public override void PostAI()
	{
		if (NoCollideTimer > 0)
		{
			Projectile.tileCollide = false;
			NoCollideTimer--;
		}
		else if (!Projectile.tileCollide)
			Projectile.tileCollide = true;

		eyeballRotation = Utils.AngleLerp(eyeballRotation, targetEyeballRotation, 0.19f);

		if (InGround())
			Projectile.velocity.Y -= 0.5f;
	}

	bool Grounded() => Collision.SolidCollision(Projectile.Center - Vector2.UnitY * 4, Projectile.width, Projectile.height);
	bool InGround() => Collision.SolidCollision(Projectile.Top, Projectile.width, Projectile.height); // if stuck in ground
	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		int oldDirection = Math.Sign(Projectile.velocity.X);

		Projectile.velocity *= 0;
		Projectile.velocity.Y -= 4f;
		Projectile.velocity.X = oldDirection * -2f;

		for (int i = 0; i < 2; i++)
		{
			Dust.NewDustPerfect(Projectile.Center, DustID.Blood, Main.rand.NextVector2Circular(2f, 2f), 125, default, 0.95f).noGravity = true;
		}
	}

	public override void OnKill(int timeLeft)
	{
		SoundEngine.PlaySound(SoundID.NPCHit8 with { Volume = 0.45f, PitchVariance = 0.4f}, Projectile.Center);

		for (int i = 0; i < 3; i++)
		{
			Dust.NewDustPerfect(Projectile.Center, DustID.Blood, Main.rand.NextVector2Circular(4f, 4f), 150, default, 1.5f).noGravity = true;
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		var texture = ModContent.Request<Texture2D>(Texture).Value;

		var frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);

		if (!Grounded())
		{
			for (int i = 0; i < Projectile.oldPos.Length; i++)
			{
				float lerp = 1f - i / (float)Projectile.oldPos.Length;

				Main.spriteBatch.Draw(texture, Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition, frame, new Color(161, 140, 140, 0) * 0.33f * lerp, Projectile.oldRot[i], frame.Size() / 2f, 1f, 0f, 0f);
			}
		}

		return true;
	}

	public override void PostDraw(Color lightColor)
	{
		var eyeballTexture = ModContent.Request<Texture2D>(Texture + "_Eye").Value;
		var bloom = AssetLoader.LoadedTextures["BloomNonPremult"].Value;

		Vector2 eyeballPos = Projectile.Center + Vector2.One.RotatedBy(eyeballRotation - MathHelper.PiOver2) * 1f - Main.screenPosition;

		Main.spriteBatch.End();
		Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

		Main.spriteBatch.Draw(bloom, eyeballPos, null, Color.Black, eyeballRotation + MathHelper.Pi, bloom.Size() / 2f, 0.25f, 0f, 0f);

		Main.spriteBatch.End();
		Main.spriteBatch.BeginDefault();

		Main.spriteBatch.Draw(eyeballTexture, eyeballPos, null, Color.White, eyeballRotation + MathHelper.Pi, eyeballTexture.Size() / 2f, 1f, 0f, 0f);
	}

	public override bool DoAutoFrameUpdate(ref int framespersecond, ref int startframe, ref int endframe) => false;
}
