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
using SpiritReforged.Common.PrimitiveRendering.Trails;

namespace SpiritReforged.Content.Ocean.Items.Reefhunter.Projectiles;

[AutoloadGlowmask("Method:Content.Ocean.Items.Reefhunter.Projectiles.UrchinBall GlowColor")]
public class UrchinBall : ModProjectile
{
	public static readonly SoundStyle Impact = new("SpiritReforged/Assets/SFX/Projectile/Impact_Slimy")
	{
		PitchVariance = 0.2f,
		MaxInstances = 2
	};

	public static readonly SoundStyle LiquidExplosion = new("SpiritReforged/Assets/SFX/Projectile/Explosion_Liquid")
	{
		PitchVariance = 0.2f,
		Volume = 0.8f,
		MaxInstances = 2
	};

	public static readonly SoundStyle GenericExplosion = new("SpiritReforged/Assets/SFX/Projectile/Explosion_Generic")
	{
		PitchVariance = 0.2f,
		Volume = 0.5f,
		MaxInstances = 2
	};

	public static readonly SoundStyle BalloonExplosion = new("SpiritReforged/Assets/SFX/Projectile/Explosion_Balloon")
	{
		PitchVariance = 0.2f,
		Volume = 0.2f,
		MaxInstances = 2
	};

	public bool HasTarget => _targetIndex > 0;

	private int _targetIndex;

	private bool _spawned = true;
	private Vector2 _relativePoint = Vector2.Zero;
	private bool _stuckInTile = false;
	private Point16 _stuckTilePos = Point16.Zero;
	private int _squishTime = 0;

	private const int MAX_LIFETIME = 180;
	private const int DETONATION_TIME = 90;
	public const float MAX_SPEED = 10f; //Used by the staff to shoot the projectile
	private const int MAX_SQUISHTIME = 20;

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = 6;
		ProjectileID.Sets.TrailingMode[Type] = 2;
	}

	public override void SetDefaults()
	{
		Projectile.width = 16;
		Projectile.height = 16;
		Projectile.DamageType = DamageClass.Magic;
		Projectile.friendly = true;
		Projectile.penetrate = -1;
	}

	public void CreateTrail(ProjectileTrailRenderer renderer)
	{
		if (Main.dedServ)
			return;

		var position = new EntityTrailPosition(Projectile);

		renderer.CreateTrail(Projectile, new VertexTrail(new LightColorTrail(new Color(87, 35, 88) * 0.3f, Color.Transparent), new RoundCap(), position, new DefaultShader(), 30, 100));
		renderer.CreateTrail(Projectile, new VertexTrail(new LightColorTrail(new Color(87, 35, 88) * 0.3f, Color.Transparent), new RoundCap(), position, new DefaultShader(), 15, 50));
	}

	public override bool? CanDamage() => HasTarget ? false : null;
	public override bool? CanCutTiles() => HasTarget ? false : null;

	public override void AI()
	{
		if (_spawned)
		{
			CreateTrail(TrailSystem.ProjectileRenderer);
			_spawned = false;
		}

		if (HasTarget)
		{
			NPC target = Main.npc[_targetIndex];

			if (Projectile.tileCollide == true)
				PostHitNPC(target);

			if (!target.CanBeChasedBy(this) && target.type != NPCID.TargetDummy)
			{
				Projectile.netUpdate = true;
				Projectile.tileCollide = true;
				Projectile.timeLeft *= 2;

				_targetIndex = -1;
				return;
			}

			Projectile.Center = target.Center + _relativePoint;
		}
		else
		{
			if (_stuckInTile) //Check if tile it's stuck in is still active
			{
				Projectile.velocity = Vector2.Zero;
				if (!Main.tile[_stuckTilePos.X, _stuckTilePos.Y].HasTile) //If not, update and let the projectile fall again
				{
					_stuckInTile = false;
					_stuckTilePos = Point16.Zero;
					Projectile.netUpdate = true;
				}
			}
			else
			{
				Projectile.velocity.Y += 0.25f;
				Projectile.rotation += 0.1f * Math.Sign(Projectile.velocity.X);
			}
		}

		Projectile.scale = 1 - FlashStrength() / MathHelper.Lerp(6, 3, FlashTimer());
		_squishTime = Math.Max(_squishTime - 1, 0);
		float squishScale = EaseFunction.EaseCubicIn.Ease((float)_squishTime / MAX_SQUISHTIME);
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
			SoundEngine.PlaySound(Impact with { Volume = 1.3f }, Projectile.Center);
			Projectile.timeLeft = MAX_LIFETIME;
		}

		Projectile.velocity = Vector2.Zero;
		_stuckInTile = true;
		_stuckTilePos = (Projectile.Center + oldVelocity).ToTileCoordinates16();
		Projectile.netUpdate = true;
		HitEffects(oldVelocity);

		return false;
	}

	public void PostHitNPC(NPC target)
	{
		Projectile.tileCollide = false;
		Projectile.timeLeft = MAX_LIFETIME;
		Projectile.velocity = new Vector2(0, -0.4f);

		_relativePoint = Projectile.Center - target.Center;

		if (!Main.dedServ)
		{
			HitEffects(Projectile.velocity);
			SoundEngine.PlaySound(Impact with { Volume = 1.9f }, Projectile.Center);
		}
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		_targetIndex = target.whoAmI;
		Projectile.netUpdate = true;
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

		TrailSystem.ProjectileRenderer.DissolveTrail(Projectile);
		_squishTime = MAX_SQUISHTIME;
	}

	public override void OnKill(int timeLeft)
	{
		if (Projectile.owner == Main.myPlayer)
		{
			for (int i = 0; i < 8; ++i)
			{
				Vector2 vel = new Vector2(Main.rand.NextFloat(3f, 4f), 0).RotatedBy(i * MathHelper.TwoPi / 8f).RotatedByRandom(0.33f);
				Vector2 spawnPos = Projectile.Center + (HasTarget ? Vector2.Normalize(_relativePoint) * 6 : vel);

				float spikeDamage = 0.75f;
				Projectile.NewProjectile(Projectile.GetSource_Death(), spawnPos, vel, ModContent.ProjectileType<UrchinSpike>(), (int)(Projectile.damage * spikeDamage), Projectile.knockBack, Projectile.owner);
			}
		}

		if (Main.dedServ)
			return;

		float angle = Main.rand.NextFloat(-0.1f, 0.1f) - MathHelper.PiOver2;

		for (int i = 0; i < 2; i++)
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

		ParticleHandler.SpawnParticle(new DissipatingImage(Projectile.Center, OrangeVFXColor(70), 0f, 0.125f, Main.rand.NextFloat(0.1f, 0.2f), "Scorch", new(0.5f, 0.5f), new(4, 0.5f), 30) 
		{ 
			DistortEasing = EaseFunction.EaseQuadInOut, 
			Intensity = 1.5f
		});

		SoundEngine.PlaySound(LiquidExplosion, Projectile.Center);
		SoundEngine.PlaySound(GenericExplosion, Projectile.Center);
		SoundEngine.PlaySound(BalloonExplosion, Projectile.Center);

	}

	public override bool PreDraw(ref Color lightColor)
	{
		if (!HasTarget && !_stuckInTile)
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
		writer.Write(_targetIndex);
		writer.Write(_stuckInTile);
		writer.WriteVector2(_relativePoint);
		writer.WritePoint16(_stuckTilePos);
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
		_targetIndex = reader.ReadInt32();
		_stuckInTile = reader.ReadBoolean();
		_relativePoint = reader.ReadVector2();
		_stuckTilePos = reader.ReadPoint16();
	}
}