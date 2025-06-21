using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Content.Ocean.Items.Reefhunter.Particles;
using SpiritReforged.Content.Particles;
using SpiritReforged.Common.Visuals.Glowmasks;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using SpiritReforged.Common.Multiplayer;

namespace SpiritReforged.Content.Ocean.Items.Reefhunter.Projectiles;

[AutoloadGlowmask("Method:Content.Ocean.Items.Reefhunter.Projectiles.UrchinBall GlowColor")]
public class UrchinBall : ModProjectile, ITrailProjectile
{
	private bool hasTarget = false;
	private Vector2 relativePoint = Vector2.Zero;
	private bool stuckInTile = false;
	private Point16 stuckTilePos = new(0, 0);
	private int squishTime = 0;

	private const int MAX_LIFETIME = 180;
	private const int DETONATION_TIME = 90;
	public const float MAX_SPEED = 10f; //Used by the staff to shoot the projectile
	private const int MAX_SQUISHTIME = 20;

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
		ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
	}

	public override void SetDefaults()
	{
		Projectile.width = 16;
		Projectile.height = 16;
		Projectile.DamageType = DamageClass.Magic;
		Projectile.friendly = true;
		Projectile.penetrate = -1;
		Projectile.aiStyle = 0;
	}

	public void DoTrailCreation(TrailManager tm)
	{
		tm.CreateTrail(Projectile, new LightColorTrail(new Color(87, 35, 88) * 0.3f, Color.Transparent), new RoundCap(), new DefaultTrailPosition(), 30, 100);
		tm.CreateTrail(Projectile, new LightColorTrail(new Color(87, 35, 88) * 0.3f, Color.Transparent), new RoundCap(), new DefaultTrailPosition(), 15, 50);
	}

	public override bool? CanDamage() => !hasTarget;
	public override bool? CanCutTiles() => !hasTarget;

	public override void AI()
	{
		if (!hasTarget)
		{
			if (stuckInTile) //Check if tile it's stuck in is still active
			{
				Projectile.velocity = Vector2.Zero;
				if (!Main.tile[stuckTilePos.X, stuckTilePos.Y].HasTile) //If not, update and let the projectile fall again
				{
					stuckInTile = false;
					stuckTilePos = Point16.Zero;
					Projectile.netUpdate = true;
				}
			}
			else
			{
				Projectile.velocity.Y += 0.25f;
				Projectile.rotation += 0.1f * Math.Sign(Projectile.velocity.X);
			}
		}

		else
		{
			NPC npc = Main.npc[(int)Projectile.ai[1]];

			if (!npc.CanBeChasedBy(this) && npc.type != NPCID.TargetDummy)
			{
				Projectile.netUpdate = true;
				Projectile.tileCollide = true;
				Projectile.timeLeft *= 2;

				hasTarget = false;
				return;
			}

			Projectile.Center = npc.Center + relativePoint;
		}

		Projectile.scale = 1 - FlashStrength() / MathHelper.Lerp(6, 3, FlashTimer());
		squishTime = Math.Max(squishTime - 1, 0);
		float squishScale = EaseFunction.EaseCubicIn.Ease((float)squishTime / MAX_SQUISHTIME);
		squishScale = 1 - (float)Math.Sin(MathHelper.Pi * squishScale);

		Projectile.scale *= MathHelper.Lerp(squishScale, 1, 0.85f);
		Projectile.TryShimmerBounce();
	}

	private float FlashTimer() => Math.Max(DETONATION_TIME - Projectile.timeLeft, 0) / (float)DETONATION_TIME;
	private float FlashStrength()
	{
		int numFlashes = 6;
		return EaseFunction.EaseQuarticIn.Ease((float)Math.Sin(EaseFunction.EaseQuadIn.Ease(FlashTimer()) * numFlashes * MathHelper.Pi));
	}

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		if (Projectile.timeLeft > MAX_LIFETIME)
		{
			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/Impact_Slimy") with { PitchVariance = 0.2f, Volume = 1.3f, MaxInstances = 2 }, Projectile.Center);
			Projectile.timeLeft = MAX_LIFETIME;
		}

		Projectile.velocity = Vector2.Zero;
		stuckInTile = true;
		stuckTilePos = (Projectile.Center + oldVelocity).ToTileCoordinates16();
		Projectile.netUpdate = true;
		HitEffects(oldVelocity);

		return false;
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		HitEffects(Projectile.velocity);
		Projectile.ai[1] = target.whoAmI;
		Projectile.tileCollide = false;
		Projectile.netUpdate = true;
		Projectile.timeLeft = MAX_LIFETIME;
		Projectile.velocity = new Vector2(0, -0.4f);

		hasTarget = true;
		relativePoint = Projectile.Center - target.Center;

		SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/Impact_Slimy") with { PitchVariance = 0.2f, Volume = 1.9f, MaxInstances = 2 }, Projectile.Center);
	}

	public static Color OrangeVFXColor(byte alpha = 255)
	{
		var temp = new Color(255, 131, 99);
		temp.A = alpha;
		return temp;
	}

	private void HitEffects(Vector2 velocity)
	{
		if (Main.dedServ)
			return;

		float velocityRatio = Math.Min(velocity.Length() / MAX_SPEED, 1);

		int particleLifetime = 20;
		float particleLength = 60 * velocityRatio;

		ParticleHandler.SpawnParticle(new UrchinImpact(
						Projectile.Center - velocity * 0.8f,
						Vector2.Normalize(velocity) * velocityRatio,
						particleLength * 3.5f,
						particleLength,
						velocity.ToRotation(),
						particleLifetime,
						velocityRatio));

		AssetLoader.VertexTrailManager.TryEndTrail(Projectile, 12);
		squishTime = MAX_SQUISHTIME;
	}

	public override void OnKill(int timeLeft)
	{
		for (int i = 0; i < 8; ++i)
		{
			Vector2 vel = new Vector2(Main.rand.NextFloat(3f, 4f), 0).RotatedBy(i * MathHelper.TwoPi / 8f).RotatedByRandom(0.33f);
			Vector2 spawnPos = Projectile.Center + (hasTarget ? Vector2.Normalize(relativePoint) * 6 : vel);

			float spikeDamage = 0.75f;
			Projectile.NewProjectile(Projectile.GetSource_Death(), spawnPos, vel, ModContent.ProjectileType<UrchinSpike>(), (int)(Projectile.damage * spikeDamage), Projectile.knockBack, Projectile.owner);
		}

		if (Main.dedServ)
			return;

		float angle = Main.rand.NextFloat(-0.1f, 0.1f) - MathHelper.PiOver2;

		for(int i = 0; i < 2; i++)
		{
			var easeFunction = (i == 0) ? EaseFunction.EaseQuadOut : EaseFunction.EaseCubicOut;
			float ringWidth = 0.35f + Main.rand.NextFloat(-0.1f, 0.1f);
			float size = 200 + Main.rand.NextFloat(-25, 50);
			int lifetime = 25 + Main.rand.Next(11);
			float zRotation = Main.rand.NextFloat(0.7f, 0.9f);
			float xyRotation = angle + Main.rand.NextFloat(0.3f, -0.3f);

			ParticleHandler.SpawnParticle(new TexturedPulseCircle(
				Projectile.Center + Main.rand.NextVec2CircularEven(5, 5),
				OrangeVFXColor(100) * 0.5f,
				OrangeVFXColor(100) * 0.1f,
				ringWidth,
				size,
				lifetime,
				"noise",
				new Vector2(3, 0.15f),
				easeFunction).WithSkew(zRotation, xyRotation));
		}

		for (int i = -1; i < 2; i += 2)
		{
			Vector2 baseDirection = Vector2.UnitX.RotatedBy(angle) * i;
			for(int j = 0; j < Main.rand.Next(3, 6); j++)
			{
				Vector2 pos = Projectile.Center;
				Vector2 vel = baseDirection.RotatedByRandom(MathHelper.Pi / 3) * Main.rand.NextFloat(3, 6);
				float scale = Main.rand.NextFloat(0.5f, 1f);
				int maxTime = Main.rand.Next(20, 30);

				ParticleHandler.SpawnParticle(Main.rand.NextBool() ? new UrchinShard(pos, vel, scale, maxTime) : new UrchinShardAlt(pos, vel, scale, maxTime));
			}

			for(int j = 0; j < Main.rand.Next(5, 8); j++)
			{
				Vector2 pos = Projectile.Center;
				Vector2 vel = baseDirection.RotatedByRandom(MathHelper.Pi / 4) * Main.rand.NextFloat(4, 16);
				float scale = Main.rand.NextFloat(0.5f, 0.75f);
				int maxTime = Main.rand.Next(20, 40);

				ParticleHandler.SpawnParticle(new GlowParticle(pos, vel.RotatedByRandom(0.2f) / 3, OrangeVFXColor(255), scale * 0.75f, maxTime, 1, delegate (Particle p) { p.Velocity *= 0.94f; }));
			}
		}

		ParticleHandler.SpawnParticle(new DissipatingImage(Projectile.Center, OrangeVFXColor(70), 0f, 0.125f, Main.rand.NextFloat(0.1f, 0.2f), "Scorch", new(0.5f, 0.5f), new(4, 0.5f), 30) { DistortEasing = EaseFunction.EaseQuadInOut, Intensity = 1.5f});

		SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/Explosion_Liquid") with { PitchVariance = 0.2f, Volume = .8f, MaxInstances = 2 }, Projectile.Center);
		SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/Explosion_Generic") with { PitchVariance = 0.2f, Volume = .5f, MaxInstances = 2 }, Projectile.Center);
		SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/Explosion_Balloon") with { PitchVariance = 0.2f, Volume = .2f, MaxInstances = 2 }, Projectile.Center);

	}

	public override bool PreDraw(ref Color lightColor)
	{
		if(!hasTarget && !stuckInTile)
			Projectile.QuickDrawTrail(Main.spriteBatch, 0.33f);

		Projectile.QuickDraw(Main.spriteBatch);

		return false;
	}

	public static Color GlowColor(object proj)
	{
		var urchinball = (proj as Projectile).ModProjectile as UrchinBall;
		float alpha = 1 - urchinball.FlashStrength();
		return OrangeVFXColor(0) * (1 - alpha);
	}

	public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
	{
		width /= 3;
		height /= 3;
		return base.TileCollideStyle(ref width, ref height, ref fallThrough, ref hitboxCenterFrac);
	}

	public override void SendExtraAI(BinaryWriter writer)
	{
		writer.Write(hasTarget);
		writer.Write(stuckInTile);
		writer.WriteVector2(relativePoint);
		writer.WritePoint16(stuckTilePos);
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
		hasTarget = reader.ReadBoolean();
		stuckInTile = reader.ReadBoolean();
		relativePoint = reader.ReadVector2();
		stuckTilePos = reader.ReadPoint16();
	}
}