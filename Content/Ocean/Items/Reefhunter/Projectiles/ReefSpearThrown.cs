using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Content.Ocean.Items.Reefhunter.Particles;
using SpiritReforged.Content.Particles;
using System.IO;
using Terraria.Audio;

namespace SpiritReforged.Content.Ocean.Items.Reefhunter.Projectiles;

public class ReefSpearThrown : ModProjectile
{
	public bool HasTarget
	{
		get => Projectile.ai[2] == 1;
		set => Projectile.ai[2] = value ? 1 : 0;
	}

	public const float MAX_SPEED = 13;

	private Vector2 relativePoint = Vector2.Zero;

	public override LocalizedText DisplayName => Language.GetText("Mods.SpiritReforged.Items.ReefSpear.DisplayName");

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = 6;
		ProjectileID.Sets.TrailingMode[Type] = 2;
	}

	public override void SetDefaults()
	{
		Projectile.width = 30;
		Projectile.height = 30;
		Projectile.friendly = true;
		Projectile.penetrate = -1;
		Projectile.tileCollide = true;
		Projectile.DamageType = DamageClass.Melee;
		Projectile.aiStyle = 0;

		MoRHelper.SetSpearBonus(Projectile);
	}

	public override bool? CanDamage() => !HasTarget;
	public override bool? CanCutTiles() => !HasTarget;

	public override void AI()
	{
		if (!HasTarget)
		{
			Projectile.velocity.Y += 0.3f;
			Projectile.rotation = Projectile.velocity.ToRotation();
			Projectile.tileCollide = Projectile.ai[0]++ > 6;

			Projectile.TryShimmerBounce();
		}
		else
		{
			NPC npc = Main.npc[(int)Projectile.ai[1]];

			if (!npc.active)
			{
				Projectile.netUpdate = true;
				Projectile.tileCollide = true;
				Projectile.timeLeft *= 2;
				Projectile.velocity *= 0;

				HasTarget = false;
				return;
			}

			Projectile.ai[0]++;
			float factor = 1 - Projectile.ai[0] / 10f;
			if (Projectile.ai[0] >= 10f)
				factor = 0;

			relativePoint += Projectile.velocity * factor * 0.1f;

			Projectile.Center = npc.Center + relativePoint;
			Projectile.ignoreWater = true;

			if (Projectile.timeLeft <= 10)
				Projectile.alpha += 25;
		}
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/Impact_Hard") with { PitchVariance = 0.2f, Pitch = -1.6f, Volume = 2.75f, MaxInstances = 2 }, Projectile.Center);
		SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/Impact_Slimy") with { PitchVariance = 0.25f, Pitch = 1f, Volume = 1.6f, MaxInstances = 2 }, Projectile.Center);

		MakeParticles(Projectile.velocity);
		if (!target.active)
			return;

		Projectile.ai[0] = 0;
		Projectile.ai[1] = target.whoAmI;
		Projectile.tileCollide = false;
		Projectile.timeLeft = 300;
		Projectile.netUpdate = true;

		HasTarget = true;
		relativePoint = Projectile.Center - target.Center;

		MakeParticles(Projectile.velocity);

		for (int i = 0; i < Main.rand.Next(5, 7); i++)
		{
			Vector2 offset = (Vector2.UnitY.RotatedBy(Projectile.velocity.ToRotation()) * Main.rand.NextFloat(-15, 15)).RotatedByRandom(0.2f);
			ParticleHandler.SpawnParticle(new BubbleParticle(Projectile.Center + offset, Vector2.Normalize(Projectile.velocity) * Main.rand.NextFloat(1f, 4f), Main.rand.NextFloat(0.1f, 0.2f), Main.rand.Next(30, 61)));
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D projTex = TextureAssets.Projectile[Type].Value;
		const int halfTipWidth = 15;
		var drawOrigin = new Vector2(Projectile.spriteDirection > 0 ? projTex.Width - halfTipWidth : halfTipWidth, projTex.Height / 2);

		if (!HasTarget)
			Projectile.QuickDrawTrail(Main.spriteBatch, 0.25f, drawOrigin: drawOrigin);

		Projectile.QuickDraw(Main.spriteBatch, origin: drawOrigin);
		return false;
	}

	public override void OnKill(int timeLeft)
	{
		if (HasTarget || Main.dedServ)
			return;

		SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
		SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/Impact_Hard") with { PitchVariance = 0.2f, Pitch = -2f, Volume = 2.5f }, Projectile.Center);

		Vector2 goreVel = Projectile.oldVelocity / 2;
		Vector2 pos = Projectile.Center;

		for (int i = 1; i <= 6; i++)
		{
			if (i >= 4)
				pos -= Vector2.Normalize(Projectile.oldVelocity) * (15 + (i - 3) * 3);

			var g = Gore.NewGorePerfect(Projectile.GetSource_Death(), pos, goreVel, Mod.Find<ModGore>("Trident" + i).Type);
			g.timeLeft = 0;
			g.rotation = Projectile.rotation;
		}

		MakeParticles(Projectile.oldVelocity);
	}

	private void MakeParticles(Vector2 velocity)
	{
		if (Main.dedServ)
			return;

		float velocityRatio = Math.Min(velocity.Length() / MAX_SPEED, 1);

		ParticleHandler.SpawnParticle(new ReefSpearImpact(null,
			Projectile.Center - Vector2.Normalize(velocity) * 6,
			Vector2.Normalize(velocity) * 2 * velocityRatio,
			240,
			100,
			velocity.ToRotation(),
			25,
			1.2f));

		var particle = new TexturedPulseCircle(
				Projectile.Center + velocity * 3f,
				new Color(230, 27, 112) * 0.75f,
				0.75f,
				100 * velocityRatio,
				20,
				"noise",
				new Vector2(4, 0.75f),
				EaseFunction.EaseCircularOut,
				false,
				0.3f).WithSkew(0.8f, Projectile.velocity.ToRotation() - MathHelper.Pi).UsesLightColor();

		particle.Velocity = Vector2.Normalize(velocity) * -velocityRatio / 3;
		ParticleHandler.SpawnParticle(particle);
	}

	public NPC GetStuckNPC() => Main.npc[(int)Projectile.ai[1]];

	public override void SendExtraAI(BinaryWriter writer) => writer.WriteVector2(relativePoint);
	public override void ReceiveExtraAI(BinaryReader reader) => relativePoint = reader.ReadVector2();
}
