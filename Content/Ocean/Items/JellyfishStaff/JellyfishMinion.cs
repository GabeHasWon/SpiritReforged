using System.Linq;
using Terraria.DataStructures;
using Microsoft.CodeAnalysis;
using SpiritReforged.Common.ProjectileCommon;
using Terraria.Audio;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.BuffCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;

namespace SpiritReforged.Content.Ocean.Items.JellyfishStaff;

[AutoloadMinionBuff()]
[AutoloadGlowmask("255, 255, 255", false)]
public class JellyfishMinion : BaseMinion
{
	public JellyfishMinion() : base(600, 800, new Vector2(28, 28)) { }

	public bool IsPink = false;

	private ref float AiState => ref Projectile.ai[0];
	private ref float AiTimer => ref Projectile.ai[1];

	private const int AISTATE_PASSIVEFLOAT = 0; //jellyfish bouncing around player
	private const int AISTATE_FLYTOPLAYER = 1; //ignore tiles and fly to player if too far
	private const int AISTATE_AIMTOTARGET = 2; //slowly aim to target, then charge at them
	private const int AISTATE_PREPARESHOOT = 3; //between the dash and before shooting, adjust velocity and rotation to rise upwards
	private const int AISTATE_SHOOT = 4; //when near target, hover in place and shoot lightning

	//Constants used in drawing methods and for the ai pattern
	private const int SHOOTTIME = 45; //Time between shots
	private const int AIMTIME = 50; //Time it takes to aim the dash
	private const int BOUNCETIME = 90; //General time between bounces
	private const int RISETIME = 20; //Time it takes to rise upwards after the dash

	public static int SHOOT_RANGE { get; set; } = 400; //Static because it's used by the bolt class

	private Color GetColor => IsPink ? new Color(248, 148, 255) : new Color(133, 177, 255);
	private float AdjustedVelocityAngle => Projectile.velocity.ToRotation() + MathHelper.PiOver2;

	public override void AbstractSetStaticDefaults()
	{
		Main.projFrames[Type] = 3;
		ProjectileID.Sets.TrailCacheLength[Type] = 10;
		ProjectileID.Sets.TrailingMode[Type] = 2;
	}

	public override void OnSpawn(IEntitySource source) => IsPink = Main.rand.NextBool(2);

	public override void IdleMovement(Player player)
	{
		Projectile.tileCollide = false;
		if (AiState > AISTATE_FLYTOPLAYER)
		{
			AiTimer = 0;
			AiState = AISTATE_PASSIVEFLOAT;
			Projectile.netUpdate = true;
		}

		int flyToPlayerThreshold = 100; //How far the minion needs to be from the player to start flying to them
		int floatThreshold = flyToPlayerThreshold / 2; //How close the minion needs to be to the player to stop flying to them
		switch (AiState)
		{
			case AISTATE_PASSIVEFLOAT:
				Projectile.rotation -= Projectile.rotation.AngleLerp(0, 0.05f);

				float speed = Projectile.Distance(player.Center) / 50;
				speed = Math.Min(speed, 5f);

				Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(player.Center) * speed, 0.04f);

				//bounce in general direction of player but moved upwards, semi randomized timing
				if (AiTimer >= BOUNCETIME)
				{
					Projectile.velocity += Projectile.DirectionTo(player.Center) * Main.rand.NextFloat(1, 2);
					Projectile.velocity.Y -= Main.rand.NextFloat(3, 4);
					Projectile.velocity = Projectile.velocity.RotatedByRandom(MathHelper.PiOver4);
					AiTimer = Main.rand.Next(-BOUNCETIME / 5, BOUNCETIME / 5);
					Projectile.netUpdate = true;
				}

				if (Projectile.Distance(player.Center) > flyToPlayerThreshold)
				{
					AiState = AISTATE_FLYTOPLAYER;
					AiTimer = 0;
					Projectile.netUpdate = true;
				}

				break;

			case AISTATE_FLYTOPLAYER:
				float flySpeed = 12f;
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(player.Center) * flySpeed, 0.06f);
				Projectile.rotation = Projectile.rotation.AngleLerp(AdjustedVelocityAngle, 0.2f);

				if (Projectile.Distance(player.Center) <= floatThreshold)
				{
					AiState = AISTATE_PASSIVEFLOAT;
					AiTimer = 0;
					Projectile.netUpdate = true;
				}

				break;
		}

		int maxDistFromPlayer = 1200;
		if (Projectile.Distance(player.Center) > maxDistFromPlayer)
		{
			Projectile.Center = player.Center;
			Projectile.netUpdate = true;
		}

		AiTimer++;
	}

	public override void TargettingBehavior(Player player, NPC target)
	{
		Projectile.tileCollide = false;
		AiTimer++;

		if (AiState < AISTATE_AIMTOTARGET)
		{
			AiState = (Projectile.Distance(target.Center) >= SHOOT_RANGE || !Collision.CanHit(Projectile, target)) ? AISTATE_AIMTOTARGET : AISTATE_PREPARESHOOT;
			AiTimer = 0;
			Projectile.netUpdate = true;
		}

		switch (AiState)
		{
			case AISTATE_AIMTOTARGET:
				float aimProgress = AiTimer / AIMTIME;
				float aimSpeed = MathHelper.Lerp(6, 0.25f, aimProgress);
				float interpolationSpeed = MathHelper.Lerp(0.1f, 0.2f, aimProgress);

				Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(target.Center) * aimSpeed, interpolationSpeed);
				Projectile.rotation = Projectile.rotation.AngleLerp(AdjustedVelocityAngle, 0.1f);

				if (AiTimer >= AIMTIME)
				{
					AiTimer = 0;
					AiState = AISTATE_PREPARESHOOT;
				}

				break;

			case AISTATE_PREPARESHOOT:
				float progress = AiTimer / RISETIME;
				Projectile.rotation = Projectile.rotation.AngleLerp(0, 1f / RISETIME);
				float speed = MathHelper.Lerp(2, 0.4f, progress);
				float slowdownLerpSpeed = MathHelper.Lerp(0.05f, 0.2f, progress);
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, -Vector2.UnitY * speed, slowdownLerpSpeed);

				if (AiTimer > RISETIME)
				{
					AiState = AISTATE_SHOOT;
					AiTimer = 0;
					Projectile.netUpdate = true;
				}

				break;

			case AISTATE_SHOOT:
				Vector2 aimDirection = Projectile.DirectionTo(target.Center);
				float ySpeed = 1f;
				float xSpeed = 0.3f;
				Projectile.velocity += new Vector2(xSpeed * aimDirection.X / SHOOTTIME, ySpeed / SHOOTTIME);
				Projectile.rotation = Projectile.rotation.AngleLerp(0, 0.1f);

				if (Projectile.Distance(target.Center) >= SHOOT_RANGE * 0.33f) //Really slow movement towards target if too far
					Projectile.position += Projectile.DirectionTo(target.Center) / 4f;

				if (AiTimer % SHOOTTIME == 0)
				{
					Color particleColor = IsPink ? new Color(255, 161, 225) : new Color(156, 255, 245);
					SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/ElectricZap") with { Pitch = -.55f, Volume = .55f, MaxInstances = 3 }, Projectile.Center);
					SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/ElectricZap2") with { Pitch = -.65f, Volume = .35f, MaxInstances = 3 }, Projectile.Center);
					ParticleHandler.SpawnParticle(new TexturedPulseCircle(
						Projectile.Center + aimDirection * 10,
						Color.White.Additive(),
						particleColor.Additive() * 0.75f,
						1f,
						50,
						20,
						"Lightning",
						new Vector2(1f, 0.2f),
						EaseFunction.EaseCircularOut,
						false,
						0.3f).WithSkew(0.6f, aimDirection.ToRotation() + MathHelper.Pi));

					if (Projectile.owner == Main.myPlayer)
					{
						PreNewProjectile.New(Projectile.GetSource_FromAI(), Projectile.Center, aimDirection * JellyfishBolt.HITSCAN_STEP, ModContent.ProjectileType<JellyfishBolt>(), Projectile.damage, Projectile.knockBack, Projectile.owner, IsPink ? 1 : 0, 0, 3, delegate (Projectile p)
						{
							(p.ModProjectile as JellyfishBolt).startPos = p.Center;
						});

						Projectile.netUpdate = true;
					}

					Projectile.velocity = 0.5f * new Vector2(-xSpeed * aimDirection.X, -ySpeed);
				}

				if (Projectile.Distance(target.Center) >= SHOOT_RANGE || !Collision.CanHit(Projectile, target))
				{
					AiState = AISTATE_AIMTOTARGET;
					AiTimer = 0;
					Projectile.netUpdate = true;
				}

				break;
		}
	}

	public override bool DoAutoFrameUpdate(ref int framespersecond, ref int startframe, ref int endframe)
	{
		framespersecond = AiState == AISTATE_AIMTOTARGET ? 16 : 8;
		return true;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Color drawColor = GetColor;
		for (int i = 0; i < 2; i++)
		{
			if (i == 1)
				drawColor.A = 0; //Makes the rest of the sprites that use this color outside of this block have 0 alpha unless changed back manually, but we want that here so there's no need

			Projectile.QuickDraw(Main.spriteBatch, Projectile.rotation, null, drawColor);
		}

		if (AiState == AISTATE_SHOOT)
		{
			DrawGlowmask(Math.Min(AiTimer / SHOOTTIME, 1) / 5);

			//Make it flash white just after shooting by drawing more additive glowmasks
			float flashOpacity = 1 - AiTimer % SHOOTTIME / SHOOTTIME; //cycle from 1 to 0
			if (AiTimer < SHOOTTIME) //Dont flash before shooting for the first time
				flashOpacity = 0;

			flashOpacity = EaseFunction.EaseCircularIn.Ease(flashOpacity);
			DrawGlowmask(flashOpacity);
		}

		return false;

		void DrawGlowmask(float opacity)
		{
			GlowmaskProjectile.ProjIdToGlowmask.TryGetValue(Projectile.type, out var glowMask);
			Vector2 position = Projectile.Center - Main.screenPosition;
			for (int j = 0; j < 4; j++)
			{
				position += Vector2.UnitX.RotatedBy(MathHelper.PiOver2 * j);
				Main.spriteBatch.Draw(glowMask.Glowmask.Value, position, Projectile.DrawFrame(), Projectile.GetAlpha(drawColor * opacity), Projectile.rotation, Projectile.DrawFrame().Size() / 2, Projectile.scale, SpriteEffects.None, 0);
			}
		}
	}

	public override void PostAI()
	{
		Lighting.AddLight(Projectile.Center, GetColor.ToVector3() * .25f);

		foreach (var p in Main.ActiveProjectiles) //Avoid grouping up
		{
			if (p.whoAmI != Projectile.whoAmI && p.type == Projectile.type && p.owner == Projectile.owner && p.Hitbox.Intersects(Projectile.Hitbox))
				Projectile.velocity += Projectile.DirectionFrom(p.Center) / 10;
		}
	}

	public override bool MinionContactDamage() => false;
}