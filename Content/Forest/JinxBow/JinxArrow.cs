using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Forest.JinxBow;

public class JinxArrow : ModProjectile
{
	private const int MAX_TIMELEFT = 80;
	private const int SPAWN_DELAY = 15;

	private ref float StuckNPC => ref Projectile.ai[0];
	private bool HasStruckNPC { get => Projectile.ai[1] == 1; set => Projectile.ai[1] = value ? 1 : 0; }

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.MinionShot[Projectile.type] = true;
		ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
		ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
	}

	public override void SetDefaults()
	{
		Projectile.Size = new(2);
		Projectile.friendly = true;
		Projectile.penetrate = -1;
		Projectile.DamageType = DamageClass.Summon;
		Projectile.timeLeft = MAX_TIMELEFT + SPAWN_DELAY;
		Projectile.tileCollide = false;
		Projectile.usesIDStaticNPCImmunity = true;
		Projectile.idStaticNPCHitCooldown = -1;
	}

	public override void AI()
	{
		const float offsetMagnitude = 5;

		NPC stuckNPC = Main.npc[(int)StuckNPC];
		Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
		if(!stuckNPC.active)
		{
			Projectile.Kill();
			return;
		}

		if(Projectile.timeLeft == MAX_TIMELEFT)
			SpawnFX(stuckNPC);

		float timeLeftProgress = Math.Min(Projectile.timeLeft / (float)MAX_TIMELEFT, 1);
		timeLeftProgress = EaseFunction.EaseCubicIn.Ease(EaseFunction.EaseCircularIn.Ease(timeLeftProgress));
		Projectile.position = stuckNPC.Center + Vector2.Lerp(-Projectile.velocity, Projectile.velocity / 4, 1 - timeLeftProgress) * offsetMagnitude;
		Lighting.AddLight(Projectile.Center, Color.MediumPurple.ToVector3() / 3);
	}

	private void SpawnFX(NPC stuckNPC)
	{
		Color particleColor = Color.MediumPurple.Additive(50);
		Vector2 startArrowParticlePos = Projectile.Center - Projectile.velocity * 3f;

		SoundEngine.PlaySound(SoundID.DD2_FlameburstTowerShot with { Pitch = 1.25f }, Projectile.Center);
		ParticleHandler.SpawnParticle(new ImpactLinePrim(startArrowParticlePos, Projectile.velocity / 2, particleColor, new(0.75f, 3), 16, 1, stuckNPC));

		float ringRotation = Projectile.velocity.ToRotation() + MathHelper.Pi;
		JinxBowMinion.JinxArrowRing(startArrowParticlePos, Projectile.velocity / 60, 120, ringRotation, 0.9f);
		JinxBowMinion.JinxArrowRing(startArrowParticlePos, Projectile.velocity / 30, 80, ringRotation, 0.9f);

		JinxBowMinion.JinxArrowRing(stuckNPC.Center, Projectile.velocity / 120, 80, ringRotation, 0.8f);
		ParticleHandler.SpawnParticle(new ImpactLinePrim(stuckNPC.Center, Vector2.Zero, Color.MediumPurple.Additive(), new(1, 4), 14, 1));
		ParticleHandler.SpawnParticle(new LightBurst(stuckNPC.Center, Main.rand.NextFloatDirection(), Color.MediumPurple.Additive(), 0.66f, 25));

		void GlowParticleSpawn(Vector2 positionOffset, Vector2 baseVelocity)
		{
			Vector2 velocity = baseVelocity * Main.rand.NextFloat(0.5f, 4);
			float scale = Main.rand.NextFloat(0.3f, 0.7f);
			int lifeTime = Main.rand.Next(12, 40);
			static void DelegateAction(Particle p) => p.Velocity *= 0.9f;

			ParticleHandler.SpawnParticle(new GlowParticle(stuckNPC.Center + positionOffset, velocity, Color.MediumPurple.Additive(), scale, lifeTime, 1, DelegateAction));
			ParticleHandler.SpawnParticle(new GlowParticle(stuckNPC.Center + positionOffset, velocity, Color.White.Additive(), scale, lifeTime, 1, DelegateAction));
		}

		for (int i = 0; i < 12; i++)
			GlowParticleSpawn(Vector2.Zero, Main.rand.NextVector2Unit() * 1.5f);

		for (int i = 0; i < 8; i++)
			GlowParticleSpawn(Main.rand.NextVector2Unit() * Main.rand.NextFloat(12), Projectile.velocity / 6);
	}

	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		modifiers.FinalDamage *= 1.5f;
		modifiers.SetCrit();
		HasStruckNPC = true;
	}

	public override bool? CanHitNPC(NPC target) => target.whoAmI == (int)StuckNPC;

	public override bool? CanDamage() => !HasStruckNPC && Projectile.timeLeft <= MAX_TIMELEFT;

	public override bool ShouldUpdatePosition() => false;

	public override bool PreDraw(ref Color lightColor)
	{
		if (Projectile.timeLeft > MAX_TIMELEFT)
			return false;

		Texture2D projTex = TextureAssets.Projectile[Projectile.type].Value;

		Color drawColor = Color.MediumPurple.Additive(50) * EaseFunction.EaseCircularOut.Ease(Projectile.timeLeft / (float)MAX_TIMELEFT);
		Vector2 drawPos = Projectile.Center - Main.screenPosition;

		Projectile.QuickDrawTrail(baseOpacity: 0.25f, drawColor: drawColor);
		for(int i = 0; i < 12; i++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / 12f);

			Main.EntitySpriteDraw(projTex, drawPos + offset, null, drawColor * 0.2f, Projectile.rotation, projTex.Size() / 2, Projectile.scale, SpriteEffects.None);
		}

		Projectile.QuickDraw(drawColor: drawColor);
		return false;
	}
}