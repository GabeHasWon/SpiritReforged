using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using System.Linq;
using Terraria.Audio;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items.Crook;

public class CrookLocust : ModProjectile
{
	public sealed class LocustExplosion : ModProjectile
	{
		public override string Texture => AssetLoader.EmptyTexture;

		public override void SetDefaults()
		{
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.Size = new Vector2(90);
			Projectile.timeLeft = 10;

			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;

			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.TryGetGlobalNPC<LocustDamageGlobalNPC>(out var globalNPC);

			for (int i = 0; i < 2 + Main.rand.Next(3); i++)
			{
				if (globalNPC?.locusts.Count < LocustDamageGlobalNPC.MAX_LOCUSTS)
				{
					globalNPC.AddLocust(target.whoAmI);
					globalNPC.AttackerWhoAmI = Projectile.owner;
				}
			}
		}
	}

	public sealed class CrookLocustGore : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.Size = new Vector2(6);
			Projectile.timeLeft = 120;
			Projectile.tileCollide = true;
		}

		public override void AI()
		{
			Projectile.velocity *= 0.98f;
			Projectile.rotation += Projectile.velocity.X * 0.05f;

			if (Projectile.timeLeft < 110)
			{
				if (Projectile.velocity.Y < 16f)
				{
					Projectile.velocity.Y += 0.15f;

					if (Projectile.velocity.Y > 0)
						Projectile.velocity.Y *= 1.1f;
				}
				else
					Projectile.velocity.Y = 16f;
			}
		}

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 5; i++)
			{
				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(5, 5), DustID.Poisoned,
						Main.rand.NextVector2Circular(3f, 3f), 50 + Main.rand.Next(100), default, Main.rand.NextFloat(1.2f, 1.3f)).noGravity = true;

				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(50, 50), DustID.Poisoned,
					-Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.2f), 50 + Main.rand.Next(100), default, Main.rand.NextFloat(1.2f, 1.3f)).noGravity = true;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			var tex = TextureAssets.Projectile[Type].Value;
			var solid = TextureColorCache.ColorSolid(tex, Color.White);

			Rectangle sourceRectangle = tex.Frame(1, 3, frameY: (int)Projectile.ai[0]);

			Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, sourceRectangle, Projectile.GetAlpha(lightColor), Projectile.rotation, sourceRectangle.Size() / 2f,
				Projectile.scale, 0, 0f);

			if (Projectile.timeLeft > 90)
			{
				float lerp = (Projectile.timeLeft - 90) / 30f;

				Main.spriteBatch.Draw(solid, Projectile.Center - Main.screenPosition, sourceRectangle, Color.DarkOliveGreen * lerp, Projectile.rotation, sourceRectangle.Size() / 2f,
					Projectile.scale, 0, 0f);
			}

			return false;
		}
	}

	public static readonly SoundStyle HitSound = new SoundStyle("SpiritReforged/Assets/SFX/Projectile/Impact_Slimy") with { Volume = 0.66f, PitchVariance = 0.15f };
	public static readonly SoundStyle DeathSound_01 = SoundID.NPCDeath1 with { Volume = 0.5f };
	public static readonly SoundStyle DeathSound_02 = new SoundStyle("SpiritReforged/Assets/SFX/NPCDeath/BugDeath") with { Volume = 0.33f, PitchVariance = 0.2f };

	public NPC TargetNPC => Main.npc[Target];
	public Projectile ParentProj => Main.projectile[(int)ProjOwner];
	private Player Owner => Main.player[Projectile.owner];

	int Target
	{
		get => (int)Projectile.ai[0];
		set => Projectile.ai[0] = value;
	}

	int AttackTimer
	{
		get => (int)Projectile.ai[1];
		set => Projectile.ai[1] = value;
	}

	ref float Timer => ref Projectile.ai[2];

	ref float ProjOwner => ref Projectile.localAI[0];

	int DashTimer
	{
		get => (int)Projectile.localAI[1];
		set => Projectile.localAI[1] = value;
	}

	internal bool RareDraw;

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.SentryShot[Type] = true;
		Main.projFrames[Type] = 4;
	}

	public override void SetDefaults()
	{
		Projectile.friendly = true;
		Projectile.Size = new Vector2(12);
		Projectile.timeLeft = 600;
		Projectile.aiStyle = -1;
		Projectile.penetrate = 3;
		Projectile.stopsDealingDamageAfterPenetrateHits = true;

		Projectile.usesIDStaticNPCImmunity = true;
		Projectile.idStaticNPCHitCooldown = 20;

		Projectile.tileCollide = false;
		Projectile.hide = true;

		RareDraw = Main.rand.NextBool(5);

		ProjectileID.Sets.TrailCacheLength[Type] = 6;
		ProjectileID.Sets.TrailingMode[Type] = 0;
	}

	public override void AI()
	{
		if (DashTimer > 0)
			DashTimer--;

		if (Projectile.penetrate > 0)
			Timer++;

		// Animates faster when moving faster
		float xVelocity = Utils.Clamp(Math.Abs(Projectile.velocity.X), 0, 15);
		int frameTimer = (int)MathHelper.Lerp(6, 2, xVelocity / 15f);

		if (++Projectile.frameCounter >= frameTimer)
		{
			Projectile.frameCounter = 0;
			if (++Projectile.frame >= Main.projFrames[Projectile.type])
				Projectile.frame = 0;
		}

		if (TargetNPC is null || !TargetNPC.active)
		{
			NPC newTarget = FindTarget();

			if (newTarget is not null)
				Target = newTarget.whoAmI;
			else
			{
				Projectile.velocity *= 0.95f;
				if (Projectile.velocity.Length() < 1f)
				{
					Explode(true);
					Projectile.Kill();
				}
			}

			return;
		}

		Projectile.spriteDirection = -Projectile.direction;

		float dist = Projectile.Distance(TargetNPC.Center);

		float velocityRamp = 1f;
		if (Timer < 60f)
			velocityRamp = Timer / 60f;

		if (Projectile.penetrate == -1 && AttackTimer <= 0)
		{
			ExplodingBehavior(dist);
			return;
		}

		Projectile.rotation = Projectile.velocity.X * 0.05f;

		if (AttackTimer > 0)
		{
			AttackTimer--;
			FlyToTarget(dist, velocityRamp);
		}
		else if (Projectile.penetrate > 0)
			Dash();
	}

	internal void ExplodingBehavior(float dist)
	{
		Vector2 direction = Vector2.Normalize(TargetNPC.Center - Projectile.Center);
		
		if (dist > 100f)
			direction *= 15f;
		else
		{
			if (Main.rand.NextBool(2) && !Main.dedServ)
			{
				Color smokeColor = new Color(5, 5, 5) * 0.2f;
				float scale = Main.rand.NextFloat(0.1f, 0.2f) * Timer / 30f;
				var velSmoke = -Projectile.velocity * 0.05f;
				ParticleHandler.SpawnParticle(new SmokeCloud(Projectile.Center + Main.rand.NextVector2Circular(5f, 5f), velSmoke, Color.DarkSeaGreen * 0.25f, smokeColor, scale, 
					EaseFunction.EaseQuadOut, Main.rand.Next(30, 40)));

				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(5, 5), DustID.Poisoned,
					Main.rand.NextVector2Circular(15f, 15f) * Timer / 30f, 50 + Main.rand.Next(100), default, Main.rand.NextFloat(1.2f, 2f)).noGravity = true;
			}

			Projectile.rotation += Main.rand.NextFloat(-0.2f, 0.2f);
			Projectile.position += TargetNPC.velocity / 2;
			Projectile.velocity *= 0.9f;
			if (++Timer > 30)
				Explode();

			return;
		}

		Projectile.velocity = Vector2.Lerp(Projectile.velocity, direction, 0.05f);
	}

	// natural death is whether it explodes on an enemy or explodes from being inactive
	internal void Explode(bool naturalDeath = false)
	{
		int loops = naturalDeath ? 3 : 15;

		for (int i = 0; i < loops; i++)
		{
			Color smokeColor = new Color(5, 5, 5) * 0.25f;
			float scale = Main.rand.NextFloat(0.07f, 0.15f);
			var velSmoke = -Vector2.UnitY * Main.rand.NextFloat(2f, 5f);
			ParticleHandler.SpawnParticle(new SmokeCloud(Projectile.Center + Main.rand.NextVector2Circular(50f, 50f), velSmoke, Color.DarkSeaGreen * 0.35f, smokeColor, scale, 
				EaseFunction.EaseQuadOut, Main.rand.Next(30, 40)));

			Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(5, 5), DustID.Poisoned,
						Main.rand.NextVector2Circular(9f, 9f), 50 + Main.rand.Next(100), default, Main.rand.NextFloat(1.2f, 1.3f)).noGravity = true;

			Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(50, 50), DustID.Poisoned,
						velSmoke, 50 + Main.rand.Next(100), default, Main.rand.NextFloat(1.2f, 1.4f)).noGravity = true;

			Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(25, 25), DustID.Sluggy,
						Main.rand.NextVector2Circular(3f, 3f), 50 + Main.rand.Next(100), default, Main.rand.NextFloat(0.2f, 1f));
		}

		if (Main.myPlayer == Projectile.owner)
		{
			if (!naturalDeath)
				Projectile.NewProjectile(Projectile.GetSource_Death("Crook Locust Explode"), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<LocustExplosion>(),
					22, 2f, Projectile.owner);

			for (int i = 0; i < 3; i++)
			{
				float mult = Main.rand.NextFloat(5f, 8f);
				if (naturalDeath)
					mult *= 0.2f;

				Projectile.NewProjectile(Projectile.GetSource_Death("Locust Gore"), Projectile.Center, -Vector2.UnitY.RotatedByRandom(Math.PI) * mult, 
					ModContent.ProjectileType<CrookLocustGore>(), 0, 0, Projectile.owner, i);
			}
		}

		Projectile.Kill();
	}

	internal void FlyToTarget(float dist, float velocityRamp)
	{
		if (DashTimer <= 0 && Main.rand.NextBool(4) && !Main.dedServ)
		{
			Color smokeColor = new Color(5, 5, 5) * 0.16f;
			float scale = Main.rand.NextFloat(0.1f, 0.15f);
			var velSmoke = -Projectile.velocity * 0.05f;
			ParticleHandler.SpawnParticle(new SmokeCloud(Projectile.Center + Main.rand.NextVector2Circular(5f, 5f), velSmoke, Color.DarkSeaGreen * 0.25f, smokeColor, scale,
				EaseFunction.EaseQuadOut, Main.rand.Next(30, 40)));

			Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(25, 25), DustID.Poisoned,
				-Projectile.velocity * Main.rand.NextFloat(), 50 + Main.rand.Next(100), default, Main.rand.NextFloat(0.9f, 1.25f)).noGravity = true;

			if (Main.rand.NextBool())
				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(25, 25), DustID.Poisoned,
					-Vector2.UnitY, 50 + Main.rand.Next(100), default, Main.rand.NextFloat(1.1f, 1.35f)).noGravity = true;
		}

		Vector2 direction = TargetNPC.Center - Projectile.Center;
		direction.Normalize();
		if (dist > 400f)
			direction *= 15f * velocityRamp;
		else
		{
			float mult = MathHelper.Lerp(15f, 5f, 1f - Projectile.Distance(TargetNPC.Center) / 500f);
			direction *= mult * velocityRamp;
		}

		Projectile.velocity = Vector2.Lerp(Projectile.velocity, direction, 0.05f);
	}

	internal void Dash()
	{
		Projectile.velocity = Projectile.DirectionTo(TargetNPC.Center) * 15f;
		AttackTimer = 30;
		DashTimer = 30;
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		SoundEngine.PlaySound(HitSound, Projectile.Center);

		if (Projectile.penetrate == 1)
			Timer = 0;

		//Vector2 normalized = Projectile.velocity.SafeNormalize(Vector2.One);

		//ParticleHandler.SpawnParticle(new CartoonHit(Projectile.Center, 20, 1, -Projectile.velocity.ToRotation(), -normalized));

		for (int i = 0; i < 4; i++)
		{
			Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(25, 25), DustID.Sluggy,
				-Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(1f), 50 + Main.rand.Next(100), default, Main.rand.NextFloat(1.2f, 2.25f)).noGravity = true;

			Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(25, 25), DustID.Ichor,
				-Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(1f), 50 + Main.rand.Next(100), default, Main.rand.NextFloat(0.5f, 1f)).noGravity = true;

			Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(25, 25), DustID.Poisoned,
				-Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.5f), 50 + Main.rand.Next(100), default, Main.rand.NextFloat(1.2f, 2.25f)).noGravity = true;
		}
	}

	public override void OnKill(int timeLeft)
	{
		SoundEngine.PlaySound(DeathSound_01, Projectile.Center);
		SoundEngine.PlaySound(DeathSound_02, Projectile.Center);
	}

	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
	{
		if (Projectile.penetrate > 0 && Timer < 30)
			behindNPCsAndTiles.Add(index);
		else
			behindProjectiles.Add(index);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		float fadeIn = 1f;
		if (Timer < 30f && Projectile.penetrate > 0)
			fadeIn = Timer / 30f;

		var tex = TextureAssets.Projectile[Type].Value;
		var bloom = AssetLoader.LoadedTextures["Bloom"].Value;
		var star = AssetLoader.LoadedTextures["Star"].Value;
		var solid = TextureColorCache.ColorSolid(tex, Color.White);

		Rectangle sourceRectangle = tex.Frame(2, Main.projFrames[Projectile.type], frameX: RareDraw ? 0 : 1, frameY: Projectile.frame);

		float scale = 1f;

		Vector2 drawPos = Projectile.Center;

		if (Projectile.penetrate < 0)
		{
			float shakeStrength = MathHelper.Lerp(0.25f, 2f, EaseFunction.EaseQuadOut.Ease(Timer / 30f));

			scale = MathHelper.Lerp(1f, 1.25f, EaseFunction.EaseQuadOut.Ease(Timer / 30f));
			drawPos += Main.rand.NextVector2Circular(shakeStrength, shakeStrength);
		}

		for (int i = 0; i < Projectile.oldPos.Length; i++)
		{
			Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f;
			float lerp = 1f - i / (float)Projectile.oldPos.Length;

			Color drawColor = Color.DarkSeaGreen * 0.5f;
			if (DashTimer > 0)
				drawColor = Color.Lerp(Color.GreenYellow.Additive(), Color.DarkSeaGreen * 0.5f, 1f - DashTimer / 30f);

			Main.spriteBatch.Draw(tex, pos - Main.screenPosition, sourceRectangle, drawColor * lerp * 0.5f * fadeIn,
			  Projectile.rotation, sourceRectangle.Size() / 2f, Projectile.scale, Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
		}

		Main.spriteBatch.Draw(tex, drawPos - Main.screenPosition, sourceRectangle, Projectile.GetAlpha(lightColor) * fadeIn, Projectile.rotation, sourceRectangle.Size() / 2f,
			Projectile.scale * scale, Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);

		if (DashTimer > 0)
		{
			float flashTimer = DashTimer / 30f;

			Vector2 eyePos = drawPos + new Vector2(6f * -Projectile.spriteDirection, 0f).RotatedBy(Projectile.rotation);

			Main.spriteBatch.Draw(bloom, eyePos - Main.screenPosition, null, new Color(255, 150, 0, 0) * 0.5f, 0f, bloom.Size() / 2f, 0.3f * flashTimer, 0f, 0f);
			Main.spriteBatch.Draw(star, eyePos - Main.screenPosition, null, new Color(255, 150, 0, 0), Projectile.rotation + MathHelper.TwoPi * EaseFunction.EaseQuadIn.Ease(flashTimer), star.Size() / 2f, 0.2f * flashTimer, 0f, 0f);
			Main.spriteBatch.Draw(star, eyePos - Main.screenPosition, null, new Color(255, 255, 255, 0) * 0.8f, Projectile.rotation + MathHelper.TwoPi * EaseFunction.EaseQuadIn.Ease(flashTimer), star.Size() / 2f, 0.1f * flashTimer, 0f, 0f);
		}

		if (Projectile.penetrate < 0)
		{
			float progress = Timer / 30f;

			Main.spriteBatch.Draw(bloom, drawPos - Main.screenPosition, null, Color.DarkOliveGreen.Additive() * 0.5f, 0f, bloom.Size() / 2f, 0.5f * progress, 0f, 0f);

			Main.spriteBatch.Draw(solid, drawPos - Main.screenPosition, sourceRectangle, Color.Lerp(Color.Black, Color.DarkOliveGreen, progress) * progress, Projectile.rotation, sourceRectangle.Size() / 2f,
				Projectile.scale * scale, Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
		}

		return false;
	}

	internal NPC FindTarget()
	{
		const float DistanceSquared = 800 * 800f;

		if (Owner.HasMinionAttackTargetNPC && Main.npc[Owner.MinionAttackTargetNPC].DistanceSQ(Projectile.Center) < DistanceSquared)
			return Main.npc[Owner.MinionAttackTargetNPC];

		return Main.npc.Take(Main.maxNPCs).Where(n => n.CanBeChasedBy() && n.DistanceSQ(Projectile.Center) < DistanceSquared).OrderBy(n => n.Distance(Projectile.Center)).FirstOrDefault();
	}
}