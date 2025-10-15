using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Ocean.Items.Reefhunter.Particles;
using SpiritReforged.Content.Particles;
using System.IO;
using Terraria.Audio;
using static SpiritReforged.Common.Easing.EaseFunction;
using static Microsoft.Xna.Framework.MathHelper;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.PrimitiveRendering.Trails;

namespace SpiritReforged.Content.Ocean.Items.BassClub;

class BassSlapperProj : BaseClubProj
{
	private const int MAX_SLAMS = 3;

	private int _numSlams = 0;

	public BassSlapperProj() : base(new Vector2(76, 84)) { }

	public override float HoldAngle_Intial => base.HoldAngle_Intial * 1.25f;

	public override float WindupTimeRatio => 0.5f;
	public override float PullbackWindupRatio => 0.5f;
	public override float HoldPointRatio => 0.15f;
	public override float SwingPhaseThreshold => 0.3f;
	public override float SwingShrinkThreshold => 0.6f;

	public void CreateTrail(ProjectileTrailRenderer renderer)
	{
		float trailDist = 64 * MeleeSizeModifier;
		float trailWidth = 50 * MeleeSizeModifier;
		float angleRangeMod = 1f;
		float rotOffset = -PiOver4 / 2;

		if (FullCharge)
		{
			trailDist *= 1.1f;
			trailWidth *= 1.1f;
			angleRangeMod = 1.125f;
		}

		SwingTrailParameters parameters = new(AngleRange * angleRangeMod, -HoldAngle_Final + rotOffset, trailDist, trailWidth)
		{
			Color = Color.White,
			SecondaryColor = new(139, 140, 70),
			TrailLength = 0.5f,
			Intensity = 0.75f,
		};

		renderer.CreateTrail(Projectile, new SwingTrail(Projectile, parameters, GetSwingProgressStatic, SwingTrail.BasicSwingShaderParams));

		parameters = new(AngleRange * angleRangeMod, -HoldAngle_Final + rotOffset, trailDist * 0.8f, trailWidth)
		{
			Color = Color.White,
			SecondaryColor = Color.Red,
			TrailLength = 0.3f,
			Intensity = 1f,
		};

		renderer.CreateTrail(Projectile, new SwingTrail(Projectile, parameters, GetSwingProgressStatic, SwingTrail.BasicSwingShaderParams));
	}

	public override void OnSwingStart()
	{
		if (!Main.dedServ)
			CreateTrail(TrailSystem.ProjectileRenderer);

		if (_numSlams == 0)
			for (int i = 0; i < 6; i++)
				Projectile.oldRot[i] = Projectile.rotation;
	}

	public override void AfterCollision()
	{
		if (_numSlams < MAX_SLAMS && FullCharge)
		{
			_lingerTimer--;
			float lingerProgress = _lingerTimer / (float)LingerTime;
			lingerProgress = 1 - lingerProgress;
			BaseRotation = Lerp(BaseRotation, HoldAngle_Final, EaseCubicOut.Ease(lingerProgress) / 6f);

			if (lingerProgress >= 0.66f)
			{
				SetAIState(AIStates.SWINGING);
				ResetData();
				Charge = 1;
				Projectile.ResetLocalNPCHitImmunity();
				OnSwingStart();
			}

			return;
		}

		base.AfterCollision();
	}

	public override void OnSmash(Vector2 position)
	{
		++_numSlams;

		TrailSystem.ProjectileRenderer.DissolveTrail(Projectile);
		Collision.HitTiles(Projectile.position, Vector2.UnitY, Projectile.width, Projectile.height);

		DustClouds(6);

		//placeholder water dust
		for (int i = 0; i < 10; i++)
		{
			float maxOffset = 35 * TotalScale;
			float offset = Main.rand.NextFloat(-maxOffset, maxOffset);
			Vector2 dustPos = Projectile.Bottom + Vector2.UnitX * offset;
			float velocity = Lerp(3, 0, EaseCircularIn.Ease(Math.Abs(offset) / maxOffset));
			if (FullCharge)
				velocity *= 1.33f;

			ParticleHandler.SpawnParticle(new BubbleParticle(dustPos, velocity * -Vector2.UnitY, Main.rand.NextFloat(0.2f, 0.4f), Main.rand.Next(20, 40)));

			for (int j = 0; j < 2; j++)
				Dust.NewDustPerfect(dustPos + Main.rand.NextVector2Circular(4, 4), DustID.Water, velocity * -Vector2.UnitY * Main.rand.NextFloat(), Scale: Main.rand.NextFloat(2));
		}

		DoShockwaveCircle(Projectile.Bottom - Vector2.UnitY * 8, 220, PiOver2, 0.4f);

		SoundEngine.PlaySound(SoundID.NPCHit9.WithPitchOffset(-0.25f).WithVolumeScale(0.5f), Projectile.Center);
		SoundEngine.PlaySound(SoundID.NPCHit1.WithPitchOffset(-0.25f), Projectile.Center);
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (Main.dedServ)
			return;

		SoundEngine.PlaySound(SoundID.NPCHit1.WithPitchOffset(0.25f), Projectile.Center);

		var basePosition = Vector2.Lerp(Projectile.Center, target.Center, 0.6f);
		Vector2 directionUnit = basePosition.DirectionFrom(Owner.MountedCenter) * TotalScale;

		ParticleHandler.SpawnParticle(new SlapperHit(basePosition, FullCharge ? 1 : 0.75f, hit.Crit));

		int numParticles = FullCharge ? 12 : 6;
		Color particleColor = hit.Crit ? Color.Red : Color.Goldenrod;
		for (int i = 0; i < numParticles; i++)
		{
			float maxOffset = 15;
			float offset = Main.rand.NextFloat(-maxOffset, maxOffset);
			Vector2 position = basePosition + directionUnit.RotatedBy(PiOver2) * offset;
			float velocity = Lerp(12, 2, Math.Abs(offset) / maxOffset) * Main.rand.NextFloat(0.9f, 1.1f);
			if (FullCharge)
				velocity *= 1.5f;

			float rotationOffset = PiOver4 * offset / maxOffset;
			rotationOffset *= Main.rand.NextFloat(0.9f, 1.1f);

			Vector2 particleVel = directionUnit.RotatedBy(rotationOffset) * velocity;
			var p = new ImpactLine(position, particleVel, particleColor.Additive(160) * 0.75f, new Vector2(0.4f, 0.8f) * TotalScale, Main.rand.Next(12, 16), 0.8f);
			p.UseLightColor = false;
			ParticleHandler.SpawnParticle(p);
		}
	}

	public override bool OverrideDraw(SpriteBatch spriteBatch, Texture2D texture, Color lightColor, Vector2 handPosition, Vector2 drawPosition)
	{
		float lerpRotation(float lerpFactor)
		{
			if (CheckAIState(AIStates.CHARGING))
				return Projectile.rotation;

			return Lerp(Projectile.rotation, Projectile.oldRot[5], lerpFactor);
		}

		Vector2 GetSegmentPosition(Vector2 lastPos, Vector2 lastOrigin, Vector2 curOrigin, float rotation) =>
			lastPos + TotalScale * new Vector2((curOrigin.X - lastOrigin.X) * Owner.direction, -(curOrigin.Y - lastOrigin.Y)).RotatedBy(rotation);

		Vector2 getHoldPoint(Vector2 input) => Effects == SpriteEffects.FlipHorizontally ? Size - input : new Vector2(input.X, Size.Y - input.Y);

		Vector2 tailOffset = new(14);
		Vector2 bodyOffset1 = new(22);
		Vector2 bodyOffset2 = new(40, 48);
		Vector2 headOffset = new(54, 62);

		float bodyRotation1 = lerpRotation(0.3f);
		float bodyRotation2 = lerpRotation(0.6f);
		float headRotation = lerpRotation(0.9f);

		Vector2 TailPos = drawPosition;
		Vector2 BodyPos1 = GetSegmentPosition(TailPos, tailOffset, bodyOffset1, Projectile.rotation);
		Vector2 BodyPos2 = GetSegmentPosition(BodyPos1, bodyOffset1, bodyOffset2, bodyRotation1);
		Vector2 HeadPos = GetSegmentPosition(BodyPos2, bodyOffset2, headOffset, bodyRotation2);

		int frameHeight = texture.Height / 4;
		var TailFrame = new Rectangle(0, 0, texture.Width, frameHeight);
		var BodyFrame1 = new Rectangle(0, frameHeight, texture.Width, frameHeight);
		var BodyFrame2 = new Rectangle(0, frameHeight * 2, texture.Width, frameHeight);
		var HeadFrame = new Rectangle(0, frameHeight * 3, texture.Width, frameHeight);

		Color drawColor = Projectile.GetAlpha(lightColor);
		Main.EntitySpriteDraw(texture, TailPos, TailFrame, drawColor, Projectile.rotation, getHoldPoint(tailOffset), TotalScale, Effects, 0);
		Main.EntitySpriteDraw(texture, BodyPos1, BodyFrame1, drawColor, bodyRotation1, getHoldPoint(bodyOffset1), TotalScale, Effects, 0);
		Main.EntitySpriteDraw(texture, BodyPos2, BodyFrame2, drawColor, bodyRotation2, getHoldPoint(bodyOffset2), TotalScale, Effects, 0);
		Main.EntitySpriteDraw(texture, HeadPos, HeadFrame, drawColor, headRotation, getHoldPoint(headOffset), TotalScale, Effects, 0);

		//Flash when fully charged
		if (CheckAIState(AIStates.CHARGING) && _flickerTime > 0)
		{
			Texture2D flash = TextureColorCache.ColorSolid(TextureAssets.Item[ModContent.ItemType<BassSlapper>()].Value, Color.White);
			float alpha = EaseQuadIn.Ease(EaseSine.Ease(_flickerTime / (float)MAX_FLICKERTIME));

			Main.EntitySpriteDraw(flash, drawPosition, null, Color.White * alpha, Projectile.rotation, getHoldPoint(tailOffset), TotalScale, Effects, 0);
		}

		return true;
	}

	internal override void SafeModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		if (FullCharge && _numSlams < MAX_SLAMS - 1)
			modifiers.Knockback *= 0.3f;
	}

	internal override void SafeModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
	{
		if (FullCharge && _numSlams < MAX_SLAMS - 1)
			modifiers.Knockback *= 0.3f;
	}

	internal override void SendExtraDataSafe(BinaryWriter writer) => writer.Write((short)_numSlams);
	internal override void ReceiveExtraDataSafe(BinaryReader reader) => _numSlams = reader.ReadInt16();
}