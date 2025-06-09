using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.CustomTrails;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Content.Particles;
using System.IO;
using Terraria.Audio;
using static Microsoft.Xna.Framework.MathHelper;
using static SpiritReforged.Common.Easing.EaseFunction;

namespace SpiritReforged.Content.Underground.Items.OreClubs;

class GoldClubProj : BaseClubProj, IManualTrailProjectile
{
	private static Color LightGold => new(255, 249, 181);
	private static Color DarkGold => new(227, 197, 105);
	private static Color Ruby => new(216, 13, 13);

	public int Direction { get; set; } = 1;

	public override float WindupTimeRatio => 0.8f;

	public override float HoldAngle_Intial => base.HoldAngle_Intial;
	public override float HoldAngle_Final => Direction * base.HoldAngle_Final / 3 - (Direction < 0 ? PiOver4 * 0.66f : 0);
	public override float SwingAngle_Max => Direction * base.SwingAngle_Max * 1.1f - (Direction < 0 ? PiOver4 * 1.2f : 0);

	public override float LingerTimeRatio => 1.5f;
	public override float SwingPhaseThreshold => 0.3f;
	public override float SwingShrinkThreshold => 0.5f;
	public override float SwingSpeedMult => (Direction == -1 && FullCharge) ? 1 : base.SwingSpeedMult;
	public override float ChargeSpeedMult => Direction == -1 ? 1.4f : 1;

	private bool _inputHeld = false;

	public GoldClubProj() : base(new Vector2(82)) { }

	internal override float ChargedScaleInterpolate(float progress) => (Direction == 1) ? base.ChargedScaleInterpolate(progress) : 1;

	internal override float ChargedRotationInterpolate(float progress) => (Direction == 1) ? base.ChargedRotationInterpolate(progress) : Lerp(WrapAngle(BaseRotation), WrapAngle(HoldAngle_Final), 0.1f);

	internal override float SwingingRotationInterpolate(float progress) => (Direction == 1) ? base.SwingingRotationInterpolate(progress) : Lerp(HoldAngle_Final, SwingAngle_Max, EaseCubicOut.Ease(progress));

	internal override bool CanCollide(float progress) => base.CanCollide(progress) && Direction == 1;

	public void DoTrailCreation(TrailManager tM)
	{
		float trailDist = 78 * MeleeSizeModifier;
		float trailWidth = 25 * MeleeSizeModifier;
		float intensity = 3;
		float trailLengthMod = 1f;
		float rotation = HoldAngle_Final - PiOver4 / 2;
		Func<Projectile, float> swingFunc = GetSwingProgressStatic;

		if (Direction < 0)
		{
			trailLengthMod *= 1.5f;
			trailWidth *= 1.25f;
			rotation += PiOver4;
			swingFunc = p => GetSwingProgressStatic(p, EaseCubicOut);
		}

		if (FullCharge)
		{
			trailWidth *= 1.6f;
			intensity *= 1.4f;
			trailLengthMod *= 1.6f;
		}

		SwingTrailParameters parameters = new(AngleRange, rotation, trailDist, trailWidth)
		{
			Color = LightGold,
			SecondaryColor = DarkGold,
			Intensity = intensity,
			TrailLength = 0.3f * trailLengthMod,
			DissolveThreshold = 0.85f
		};

		if (Direction < 0)
		{
			SwingTrailParameters upswingTrailParam = new(AngleRange * 1.1f, rotation, trailDist, 50 * MeleeSizeModifier)
			{
				Color = Color.White,
				SecondaryColor = DarkGold,
				Intensity = 0.75f,
				TrailLength = 0.5f
			};

			tM.CreateCustomTrail(new SwingTrail(Projectile, upswingTrailParam, swingFunc, SwingTrail.BasicSwingShaderParams));
		}

		tM.CreateCustomTrail(new SwingTrail(Projectile, parameters, swingFunc, s => SwingTrail.NoiseSwingShaderParams(s, "vnoise", new Vector2(0.5f, 0.5f)), TrailLayer.UnderProjectile));

		parameters.Color = Color.Pink;
		parameters.SecondaryColor = Ruby;
		parameters.Width /= 3;
		parameters.TrailLength += 0.05f * trailLengthMod;
		parameters.UseLightColor = false;

		tM.CreateCustomTrail(new SwingTrail(Projectile, parameters, swingFunc, s => SwingTrail.NoiseSwingShaderParams(s, "noiseCrystal", new Vector2(3f, 0.5f)), TrailLayer.UnderProjectile));

	}

	public override void OnSwingStart()
	{
		TrailManager.TryTrailKill(Projectile);
		TrailManager.ManualTrailSpawn(Projectile);
	}

	public override void Swinging(Player owner)
	{
		base.Swinging(owner);

		if (!Main.rand.NextBool(3) && GetSwingProgress < SwingShrinkThreshold)
		{
			Vector2 particleVel = Projectile.position.DirectionFrom(Projectile.oldPosition).RotatedByRandom(Pi / 6) * Main.rand.NextFloat(3, 5);
			int particleTime = Main.rand.Next(15, 25);
			float particleScale = Main.rand.NextFloat(0.4f, 0.6f);
			if (!FullCharge)
			{
				particleTime -= 4;
				particleScale /= 2;
			}

			static void ParticleDelegate(Particle p) => p.Velocity *= 0.85f;

			ParticleHandler.SpawnParticle(new GlowParticle(GetHeadPosition(12) + Main.rand.NextVector2Square(5, 5), particleVel, Ruby, particleScale, Main.rand.Next(15, 20), 4, ParticleDelegate));
		}

		if (owner.controlUseItem && GetSwingProgress < SwingShrinkThreshold)
			_inputHeld = true;

		if (GetSwingProgress > SwingShrinkThreshold && Direction == 1 && _inputHeld)
		{
			PrepareNextSwing();
			return;
		}
	}

	public override void OnSmash(Vector2 position)
	{
		TrailManager.TryTrailKill(Projectile);
		Collision.HitTiles(Projectile.position, Vector2.UnitY, Projectile.width, Projectile.height);

		DustClouds(12);

		if (FullCharge)
		{
			float angle = PiOver4 * 1.5f;
			if (Projectile.direction > 0)
				angle = -angle + Pi;

			DoShockwaveCircle(Vector2.Lerp(Projectile.Center, Owner.Center, 0.5f), 380, angle, 0.4f);
		}

		DoShockwaveCircle(Projectile.Bottom - Vector2.UnitY * 8, 240, PiOver2, 0.4f);
	}

	public override void AfterCollision()
	{
		const float shrinkThreshold = 0.5f;

		_lingerTimer--;
		float lingerProgress = _lingerTimer / (float)LingerTime;
		lingerProgress = 1 - lingerProgress;

		float shrinkProgress = (lingerProgress - shrinkThreshold) / (1 - shrinkThreshold);
		shrinkProgress = Clamp(shrinkProgress, 0, 1);

		if (Owner.controlUseItem && lingerProgress < shrinkThreshold)
			_inputHeld = true;

		if (_inputHeld && lingerProgress >= shrinkThreshold && Direction == 1)
		{
			PrepareNextSwing();
			return;
		}
		else
		{
			BaseScale = Lerp(1, 0, EaseQuadOut.Ease(shrinkProgress));

			if (_lingerTimer <= 0)
				Projectile.Kill();
		}

		BaseRotation = Lerp(BaseRotation, SwingAngle_Max, EaseCubicIn.Ease(lingerProgress) / 4f);
	}

	private void PrepareNextSwing()
	{
		SetAIState(AIStates.CHARGING);
		Direction = -1;
		ResetData();
		Projectile.ResetLocalNPCHitImmunity();
		//TrailManager.TryTrailKill(Projectile);
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (!Main.dedServ)
		{
			var direction = -Vector2.UnitY.RotatedByRandom(Pi / 8);

			var position = Vector2.Lerp(Projectile.Center, target.Center, 0.75f);
			SoundEngine.PlaySound(SoundID.Item70.WithVolumeScale(0.5f), position);

			float width = 220 * TotalScale;

			var pos = position + direction;
			float rotation = direction.ToRotation();
			float chargeLerp = Lerp(0.33f, 1, Charge);
			Color rubyParticleColor = Ruby.Additive(200) * chargeLerp;

			var p = new TexturedPulseCircle(pos, rubyParticleColor, Color.LightPink, 0.6f, width, Main.rand.Next(30, 35), "Star2", new Vector2(2, 1), EaseCircularOut, false, 0.2f).WithSkew(.5f, rotation);
			ParticleHandler.SpawnParticle(p);

			width *= Main.rand.NextFloat(0.9f, 1.1f);
			rotation += Main.rand.NextFloat(-0.3f, 0.3f);
			p = new TexturedPulseCircle(pos, LightGold, DarkGold, 1, width, Main.rand.Next(15, 20), "Star2", new Vector2(2, 1), EaseQuadOut, false, 0.3f).WithSkew(.75f, rotation);
			ParticleHandler.SpawnParticle(p.UsesLightColor());

			if(Direction == -1)
			{
				Vector2 velocity = -Vector2.UnitY.RotatedByRandom(PiOver4);
				velocity *= Main.rand.NextFloat(3, 5) * TotalScale * 2;

				var line = new ImpactLinePrim(position - velocity * 7, velocity, rubyParticleColor, new Vector2(EaseCircularOut.Ease(chargeLerp), 3) * TotalScale, 14, 1, target);
				line.UseLightColor = false;
				ParticleHandler.SpawnParticle(line);
			}

			float numLines = 16 * chargeLerp;
			for (int i = 0; i < numLines; i++)
			{
				Vector2 velocity = Vector2.UnitX.RotatedBy(TwoPi * i / numLines) * TotalScale;
				velocity = velocity.RotatedByRandom(PiOver4);
				velocity *= Main.rand.NextFloat(4, 7);

				var line = new ImpactLine(position, velocity, DarkGold.Additive() * 0.5f, new Vector2(0.2f, 0.6f) * TotalScale, Main.rand.Next(15, 20), 0.9f);
				line.UseLightColor = true;
				ParticleHandler.SpawnParticle(line);
			}
		}
	}

	internal override void SafeModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		if(Direction == -1)
		{
			modifiers.FinalDamage *= 1.33f;
			modifiers.Knockback *= 1.25f;
		}
	}

	internal override void SafeModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
	{
		if (Direction == -1)
		{
			modifiers.FinalDamage *= 1.33f;
			modifiers.Knockback *= 1.25f;
		}
	}

	public override void SafeDraw(SpriteBatch spriteBatch, Texture2D texture, Color lightColor, Vector2 handPosition, Vector2 drawPosition)
	{
		Texture2D starTex = AssetLoader.LoadedTextures["Star2"].Value;

		float maxSize = 0.6f * TotalScale;
		float starProgress = EaseQuadIn.Ease(Charge);

		Vector2 scale = new Vector2(Lerp(0.8f, 1.2f, EaseSine.Ease(Main.GlobalTimeWrappedHourly * 2f % 1)), 0.4f) * Lerp(0, maxSize, starProgress) * 0.4f;
		var starOrigin = starTex.Size() / 2;

		Color color = Projectile.GetAlpha(Ruby.Additive()) * EaseQuadIn.Ease(starProgress) * BaseScale * 0.5f;

		Main.spriteBatch.Draw(starTex, GetHeadPosition(12) - Main.screenPosition, null, color, 0, starOrigin, scale, SpriteEffects.None, 0);
		Main.spriteBatch.Draw(starTex, GetHeadPosition(12) - Main.screenPosition, null, color, 0, starOrigin, scale / 2, SpriteEffects.None, 0);
	}

	internal override void SendExtraDataSafe(BinaryWriter writer)
	{
		writer.Write((sbyte)Direction);
		writer.Write(_inputHeld);
	}

	internal override void ReceiveExtraDataSafe(BinaryReader reader)
	{
		Direction = reader.ReadSByte();
		_inputHeld = reader.ReadBoolean();
	}
}
