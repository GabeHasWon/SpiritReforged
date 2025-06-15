using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.CustomTrails;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using static SpiritReforged.Common.Easing.EaseFunction;
using static Microsoft.Xna.Framework.MathHelper;
using System.IO;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Underground.Items.OreClubs;

class PlatinumClubProj : BaseClubProj, ITrailProjectile
{
	private bool _inputHeld = true;

	public override float HoldAngle_Intial => Pi * 2.4f;
	public override float HoldAngle_Final => -base.HoldAngle_Final / 4;
	public override float WindupTimeRatio => 2.2f;
	public override float PullbackWindupRatio => 0.6f; 
	public override float LingerTimeRatio => 0.7f;
	public override float SwingPhaseThreshold => 0.3f;

	public PlatinumClubProj() : base(new Vector2(84)) { }

	public override bool? CanDamage() => (GetWindupProgress < 0.75f || CheckAIState(AIStates.SWINGING)) ? null : false;

	public void DoTrailCreation(TrailManager tM)
	{
		Vector2 trailDist = new(82 * MeleeSizeModifier);
		Vector2 trailWidth = new(26 * MeleeSizeModifier);
		static float windupSwingProgress(Projectile proj) => (proj.ModProjectile is BaseClubProj club) ? EaseCubicInOut.Ease(EaseCircularOut.Ease(Lerp(club.GetWindupProgress, 0, club.PullbackWindupRatio))) : 0;

		Func<Projectile, float> uSwingFunc = CheckAIState(AIStates.SWINGING) ? GetSwingProgressStatic : windupSwingProgress;
		int swingDirection = CheckAIState(AIStates.SWINGING) ? 1 : -1;
		float uRange = AngleRange;
		float uRot = HoldAngle_Final - PiOver4 / 2;
		float dissolveThreshold = 0.95f;
		float uLength = 0.8f;

		if (CheckAIState(AIStates.CHARGING))
		{
			//Reduce starting width and distance, then reduce overall distance further
			trailWidth.X *= 0.2f;
			trailDist.X *= 0.2f;
			trailDist *= 0.9f;

			uLength *= 0.33f;
			uRot += PiOver2 - PiOver4 / 2;
			uRange = Math.Abs(HoldAngle_Final - HoldAngle_Intial);
			dissolveThreshold *= EaseCubicInOut.Ease(EaseCircularOut.Ease(1 - PullbackWindupRatio));
		}

		if(FullCharge)
		{
			trailWidth *= 1.2f;
			trailDist *= 1.1f;
			uRange *= 1.1f;
		}

		SwingTrailParameters parameters = new(swingDirection * uRange, uRot, trailDist.X, trailWidth.X)
		{
			Color = new Color(246, 216, 235, 160),
			SecondaryColor = new Color(178, 188, 220),
			TrailLength = uLength,
			Intensity = 2.5f,
			DissolveThreshold = dissolveThreshold,
			DistanceEasing = EaseQuarticOut,
			MaxDistance = trailDist.Y,
			MaxWidth = trailWidth.Y,
		};

		tM.CreateCustomTrail(new SwingTrail(Projectile, parameters, uSwingFunc, s => SwingTrail.NoiseSwingShaderParams(s, "FlameTrail", new Vector2(3f, 0.25f)), TrailLayer.UnderProjectile));
		tM.CreateCustomTrail(new SwingTrail(Projectile, parameters, uSwingFunc, s => SwingTrail.NoiseSwingShaderParams(s, "supPerlin", new Vector2(1.5f, 1.25f)), TrailLayer.UnderProjectile));
	}

	public override void SafeSetDefaults() => _parameters.ChargeColor = Color.CadetBlue;

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (CheckAIState(AIStates.CHARGING))
		{
			if (target.knockBackResist > 0 && target.gravity != 0)
				target.velocity.Y = -Projectile.knockBack;

			target.velocity.X -= Projectile.knockBack * Projectile.direction * target.knockBackResist * 0.8f;
		}

		if (!Main.dedServ)
		{
			var direction = BaseRotation.ToRotationVector2();

			var position = Vector2.Lerp(Projectile.Center, target.Center, 0.75f);
			SoundEngine.PlaySound(SoundID.Item70.WithVolumeScale(0.5f), position);

			if (CheckAIState(AIStates.CHARGING))
			{
				for (int i = 0; i < 2; i++)
				{
					float width = Main.rand.NextFloat(80, 120);

					var pos = position;
					float rotation = -Vector2.UnitY.RotatedByRandom(PiOver4 / 3).ToRotation();

					var p = new TexturedPulseCircle(pos, Color.White, Color.Silver * 0.7f, 0.8f, width, Main.rand.Next(15, 20), "FlameTrail", new Vector2(2, 0.5f), EaseCircularOut, false, 0.4f).WithSkew(.7f, rotation + Pi);
					p.Velocity = -Vector2.UnitY / 3;
					p.UseLightColor = true;
					ParticleHandler.SpawnParticle(p);
				}

				for(int i = 0; i < 8; i++)
				{
					float maxOffset = 15;
					float xOffset = Main.rand.NextFloat(-maxOffset, maxOffset);

					float yVel = Lerp(5, 2, EaseQuadOut.Ease(Math.Abs(xOffset) / maxOffset)) * Main.rand.NextFloat(0.9f, 1.1f);

					var p = new ImpactLine(position + Vector2.UnitX * xOffset, -Vector2.UnitY * yVel, Color.White * 0.5f, new Vector2(0.15f, 0.3f), Main.rand.Next(15, 20), 0.92f);
					p.UseLightColor = true;
					ParticleHandler.SpawnParticle(p);
				}
			}

			if(CheckAIState(AIStates.SWINGING))
			{
				var pos = position + direction;
				float width = 230 * TotalScale;
				float rotation = direction.ToRotation();

				var p = new TexturedPulseCircle(pos, Color.White, Color.Silver, 0.6f, width, Main.rand.Next(20, 25), "Star2", new Vector2(4, 1), EaseCircularOut, false, 0.2f).WithSkew(.5f, rotation);
				ParticleHandler.SpawnParticle(p);

				float shineRotation = Main.rand.NextFloatDirection();
				for(int i = 0; i < 3; i++)
					ParticleHandler.SpawnParticle(new DissipatingImage(pos, Color.White, shineRotation, 0.12f * TotalScale, 0, "GodrayCircle", new Vector2(0), new Vector2(4f, 1.5f), 18));
				
				ParticleHandler.SpawnParticle(new Shatter(position, Color.Silver, TotalScale, 40));

				float numLines = 16;
				for(int i = 0; i < numLines; i++)
				{
					Vector2 velocity = Vector2.UnitX.RotatedBy(TwoPi * i / numLines) * TotalScale;
					velocity = velocity.RotatedByRandom(PiOver4);
					velocity *= Main.rand.NextFloat(4, 7);

					var line = new ImpactLine(position, velocity, Color.White * 0.5f, new Vector2(0.15f, 0.4f) * TotalScale, Main.rand.Next(15, 20), 0.9f);
					line.UseLightColor = true;
					ParticleHandler.SpawnParticle(line);
				}
			}
		}
	}

	public override void OnHitPlayer(Player target, Player.HurtInfo info)
	{
		if(CheckAIState(AIStates.CHARGING) && !target.noKnockback)
			target.velocity.Y -= Projectile.knockBack;
	}

	public override void Charging(Player owner)
	{
		if(!_inputHeld)
		{
			TrailManager.TryTrailKill(Projectile);
			_lingerTimer -= 2;
			float lingerProgress = _lingerTimer / (float)LingerTime;

			BaseScale = Lerp(BaseScale, 0, EaseCircularOut.Ease(lingerProgress) / 4);

			if (_lingerTimer <= 0)
				Projectile.Kill();

			BaseRotation -= 0.1f * EaseCircularOut.Ease(lingerProgress);
		}

		else
			base.Charging(owner);
	}

	internal override void WindupComplete(Player owner)
	{
		_inputHeld = owner.controlUseItem;
		Projectile.netUpdate = true;
	}

	public override void OnSwingStart()
	{
		TrailManager.TryTrailKill(Projectile);
		Projectile.ResetLocalNPCHitImmunity();
		TrailManager.ManualTrailSpawn(Projectile);

		int tempDirection = Owner.direction;
		if (Owner == Main.LocalPlayer)
		{
			int newDir = Math.Sign(Main.MouseWorld.X - Owner.Center.X);
			Projectile.velocity.X = newDir == 0 ? Owner.direction : newDir;

			if (newDir != Owner.direction)
				Projectile.netUpdate = true;
		}

		Owner.ChangeDir((int)Projectile.velocity.X);

		if (tempDirection != Owner.direction)
			for (int i = 0; i < Projectile.oldRot.Length; i++)
				Projectile.oldRot[i] = Projectile.oldRot[i] + PiOver2;
	}

	public override void OnSmash(Vector2 position)
	{
		TrailManager.TryTrailKill(Projectile);
		Collision.HitTiles(Projectile.position, Vector2.UnitY, Projectile.width, Projectile.height);

		DustClouds(10);

		if (FullCharge)
		{
			float angle = PiOver4 * 1.25f;
			if (Projectile.direction > 0)
				angle = -angle + Pi;

			DoShockwaveCircle(Vector2.Lerp(Projectile.Center, Owner.Center, 0.5f), 380, angle, 0.4f);
		}

		DoShockwaveCircle(Projectile.Bottom - Vector2.UnitY * 8, 240, PiOver2, 0.4f);
	}

	internal override float ChargedRotationInterpolate(float progress)
	{
		if (GetWindupProgress < 1)
			return Lerp(HoldAngle_Intial, HoldAngle_Final, EaseCubicInOut.Ease(EaseCircularOut.Ease(progress)));

		//Smoother transition between windup and charge
		else
			return Lerp(BaseRotation, HoldAngle_Final, 0.12f);
	}

	internal override float ChargedScaleInterpolate(float progress)
	{
		if (GetWindupProgress < 1)
			return Lerp(0.3f, 1f, EaseQuarticOut.Ease(progress));

		//Smoother transition between windup and charge
		else
			return Lerp(BaseScale, 1, 0.12f);
	}

	public override void Swinging(Player owner) => base.Swinging(owner);

	internal override bool AllowUseTurn => CheckAIState(AIStates.CHARGING) && GetWindupProgress >= 1 && _inputHeld;
	internal override bool AllowRelease => _inputHeld;

	public override bool AllowedAftertrailDraw(ref float trailOpacity)
	{
		if (base.AllowedAftertrailDraw(ref trailOpacity))
			return true;

		trailOpacity = EaseCubicOut.Ease(1 - GetWindupProgress);
		return CheckAIState(AIStates.CHARGING) && GetWindupProgress < 1;
	}

	internal override void SendExtraDataSafe(BinaryWriter writer) => writer.Write(_inputHeld);

	internal override void ReceiveExtraDataSafe(BinaryReader reader) => _inputHeld = reader.ReadBoolean();
}
