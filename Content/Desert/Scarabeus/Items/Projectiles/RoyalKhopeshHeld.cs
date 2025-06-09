using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.ProjectileCommon;
using System.IO;
using static Terraria.Player;
using static SpiritReforged.Common.Easing.EaseFunction;
using static Microsoft.Xna.Framework.MathHelper;
using Terraria.Audio;
using SpiritReforged.Common.Visuals.Glowmasks;
using Terraria;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Ocean.Items.Reefhunter.Particles;
using Microsoft.Xna.Framework.Graphics;
using SpiritReforged.Common.ModCompat;

namespace SpiritReforged.Content.Desert.Scarabeus.Items.Projectiles;

[AutoloadGlowmask("255,255,255", false)]
public class RoyalKhopeshHeld : ModProjectile
{
	private static Color SAND_LIGHT = new Color(223, 219, 147);
	private static Color SAND_DARK = new Color(150, 111, 44);
	private static Color SAND_PARTICLE = new Color(58, 49, 18);

	private static Color RUBY_LIGHT = new Color(255, 105, 155);
	private static Color RUBY_DARK = new Color(217, 2, 20);
	private static Color RUBY_PARTICLE = new Color(252, 124, 158);

	public const int EXTRA_UPDATES = 3;

	private const float WINDUP_TIME = 0.35f;

	public int SwingTime { get; set; }
	public int Combo { get; set; }
	public float SwingRadians { get; set; }
	public float SizeModifier { get; set; }

	public int SwingDirection { get; set; }

	public Vector2 BaseDirection { get; set; }

	private float AiTimer { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }

	private float[] oldScale = { 0 };

	public override bool IsLoadingEnabled(Mod mod) => false;

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = 15;
		ProjectileID.Sets.TrailingMode[Type] = 2;

		MoRHelper.AddElement(Projectile, MoRHelper.Earth);
	}

	public override void SetDefaults()
	{
		Projectile.width = 46;
		Projectile.height = 46;
		Projectile.DamageType = DamageClass.Melee;
		Projectile.friendly = true;
		Projectile.aiStyle = -1;
		Projectile.ignoreWater = true;
		Projectile.tileCollide = false;
		Projectile.penetrate = -1;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
		Projectile.extraUpdates = EXTRA_UPDATES;
		oldScale = new float[ProjectileID.Sets.TrailCacheLength[Type]]; //Match the rest of the oldX arrays
	}

	public override void AI()
	{
		if (!Projectile.TryGetOwner(out Player owner) || owner.dead)
		{
			Projectile.Kill();
			return;
		}

		//owner.heldProj = Projectile.whoAmI; Causes trail to break, looks fine enough without it imo
		owner.itemAnimation = 2;
		owner.itemTime = 2;
		if (Combo == 2)
			owner.reuseDelay = 10;

		Projectile.timeLeft = 2;

		int tempDirection = owner.direction;
		owner.direction = BaseDirection.X >= 0 ? 1 : -1;
		if (tempDirection != owner.direction && Main.netMode != NetmodeID.SinglePlayer)
			NetMessage.SendData(MessageID.SyncPlayer, -1, -1, null, owner.whoAmI);

		int direction = SwingDirection * owner.direction;
		Projectile.spriteDirection = direction;

		float progress = AiTimer / SwingTime;
		switch (Combo)
		{
			case 0:
				FirstSwing(progress, direction);
				break;
			case 1:
				DoubleSwing(progress, ref direction);
				break;
			default:
				FinalSwing(progress, direction);
				break;
		}

		float armRot = Projectile.rotation - 3 * PiOver4;
		if (direction < 0)
			armRot -= PiOver2;

		owner.SetCompositeArmFront(true, CompositeArmStretchAmount.Full, armRot);
		Projectile.Center = owner.GetFrontHandPosition(CompositeArmStretchAmount.Full, armRot);

		AiTimer++;
		if (AiTimer > SwingTime)
			Projectile.Kill();

		Projectile.scale *= SizeModifier;
		oldScale[0] = Projectile.scale;
		for (int i = oldScale.Length - 1; i > 0; i--)
			oldScale[i] = oldScale[i - 1];
	}

	public void DoSwingNoise()
	{
		if (Main.netMode != NetmodeID.Server)
			SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing with { MaxInstances = -1 }, Projectile.Center);
	}

	private void FirstSwing(float progress, int direction)
	{
		float swingProgress = GetSwingProgress();
		Projectile.scale = EaseCircularIn.Ease(EaseSine.Ease(GetSwingProgress())) * 0.3f + 0.7f;

		if (direction < 0)
			swingProgress = 1 - swingProgress;

		Projectile.rotation = Lerp(-SwingRadians * 0.6f, SwingRadians * 0.6f - PiOver4, swingProgress);

		Projectile.rotation += BaseDirection.ToRotation() + PiOver4;
		if (direction < 0)
			Projectile.rotation += PiOver4 + PiOver2;
	}

	private void DoubleSwing(float progress, ref int direction)
	{
		float swingProgress = GetSwingProgress();
		Projectile.scale = EaseQuadIn.Ease(EaseSine.Ease(swingProgress)) * 0.3f + 0.7f;

		if (progress >= 0.5f)
			swingProgress = 1 - swingProgress;

		if (AiTimer == SwingTime / 2)
		{
			Projectile.ResetLocalNPCHitImmunity();
			DoSwingNoise();
			Projectile.netUpdate = true;
		}

		Projectile.rotation = Lerp(-SwingRadians * 0.6f, SwingRadians * 0.6f - PiOver4, swingProgress);

		Projectile.rotation += BaseDirection.ToRotation() + PiOver4;
		if (direction < 0)
			Projectile.rotation += PiOver4 + PiOver2;
	}

	private void FinalSwing(float progress, int direction)
	{
		if (progress < WINDUP_TIME)
		{
			float windupProgress = progress / WINDUP_TIME;
			windupProgress = EaseCircularOut.Ease(windupProgress);

			Projectile.rotation = Lerp(-SwingRadians * 0.2f, -SwingRadians * 0.6f, windupProgress) * direction;
			Projectile.rotation += BaseDirection.ToRotation() + PiOver4;
			if (direction < 0)
				Projectile.rotation += PiOver4;

			Projectile.scale = windupProgress * 0.2f + 0.7f;
		}
		else
		{
			if ((int)(progress * SwingTime) == (int)(WINDUP_TIME * SwingTime) + 1)
				DoSwingNoise();

			float swingProgress = GetSwingProgress();
			Projectile.scale = EaseQuadIn.Ease(EaseSine.Ease(swingProgress)) * 0.1f + 0.9f - EaseCubicIn.Ease(swingProgress) * 0.1f;

			if (direction < 0)
				swingProgress = 1 - swingProgress;

			Projectile.rotation = Lerp(-SwingRadians * 0.6f, SwingRadians * 0.6f - PiOver4, swingProgress);

			Projectile.rotation += BaseDirection.ToRotation() + PiOver4;
			if (direction < 0)
				Projectile.rotation += PiOver4 + PiOver2;
		}
	}

	private float GetSwingProgress()
	{
		float progress = AiTimer / SwingTime;
		switch (Combo)
		{
			case 0:
				progress = EaseQuarticOut.Ease(progress);
				break;
			case 1:
				float halfProgress = (progress % 0.5f) * 2;
				progress = EaseCircularOut.Ease(halfProgress);
				break;
			case 2:
				float swingProgress = Max((progress - WINDUP_TIME) / (1 - WINDUP_TIME), 0);
				progress = EaseCircularOut.Ease(EaseQuadOut.Ease(swingProgress));
				break;
		}

		return progress;
	}

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		if (Combo == 2 && (AiTimer / SwingTime) < WINDUP_TIME)
			return false;

		Projectile.TryGetOwner(out Player Owner);
		Vector2 directionUnit = Vector2.UnitX.RotatedBy(Projectile.rotation - PiOver4);
		float _ = 0;
		float hitboxLengthMod = Lerp(0.8f, 1.5f, EaseSine.Ease(GetSwingProgress())) * SizeModifier;
		return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Owner.MountedCenter, Owner.MountedCenter + directionUnit * Projectile.Size.Length() * Projectile.scale * hitboxLengthMod, 10f, ref _);
	}

	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		if (Combo == 2)
		{
			modifiers.FinalDamage *= 1.5f;
			modifiers.FlatBonusDamage += Min(target.defense / 2, 20);
			modifiers.SetCrit();
		}
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		MoRHelper.Decapitation(target, ref damageDone, ref hit.Crit);

		if (Main.dedServ)
			return;

		Vector2 particleDirection = Vector2.Lerp(Vector2.UnitX.RotatedBy(Projectile.rotation - PiOver4).RotatedByRandom(Pi / 8), BaseDirection, 0.75f).SafeNormalize(BaseDirection);
		float sineProgress = Max(EaseSine.Ease(GetSwingProgress()), 0.3f);
		if (Combo < 2)
		{
			//Sand dust (icky!) and smoke clouds on normal swings
			int numSmoke = 8;
			for (int i = 0; i < numSmoke; i++)
			{
				Color smokeColor = SAND_LIGHT * 0.5f;
				float progress = i / (float)numSmoke;
				float scale = Main.rand.NextFloat(0.04f, 0.08f) * EaseCubicOut.Ease(sineProgress) * SizeModifier;
				var velSmoke = particleDirection.RotatedByRandom(Pi / 8) * Main.rand.NextFloat(3, 5) * sineProgress * progress * SizeModifier;
				ParticleHandler.SpawnParticle(new SmokeCloud(target.Center, velSmoke, smokeColor, scale, EaseQuadOut, Main.rand.Next(30, 40)));
			}

			for (int i = 0; i < (int)(Main.rand.Next(7, 10) * sineProgress); i++)
			{
				Vector2 velDust = particleDirection.RotatedByRandom(PiOver4) * Main.rand.NextFloat(3, 7) * sineProgress;
				float scale = Main.rand.NextFloat(0.9f, 1.3f) * sineProgress * SizeModifier * SizeModifier;
				Dust d = Dust.NewDustDirect(target.Center, 3, 8, DustID.Sand, velDust.X, velDust.Y, Scale: scale);
				d.noGravity = true;
			}
		}
		else
		{
			//Red impact slash and glowy particles on empowered swing
			int lifeTime = 20;
			Vector2 slashVel = particleDirection * 3;
			Vector2 slashImpactOffset = -slashVel * lifeTime / 2;
			ParticleHandler.SpawnParticle(new ImpactLine(target.Center + slashImpactOffset, slashVel, RUBY_LIGHT.Additive(150), new Vector2(0.75f, 2.5f) * SizeModifier, lifeTime, target));

			for (int i = 0; i < (int)(Main.rand.Next(7, 10) * sineProgress); i++)
			{
				Vector2 velParticle = particleDirection.RotatedByRandom(PiOver4) * Main.rand.NextFloat(2, 6) * sineProgress * SizeModifier;
				float scale = Main.rand.NextFloat(0.6f, 1f) * sineProgress * SizeModifier;
				static void DelegateAction(Particle p)
				{
					p.Velocity *= 0.93f;
				}

				ParticleHandler.SpawnParticle(new GlowParticle(target.Center + Main.rand.NextVector2Square(8, 8), velParticle, RUBY_LIGHT, RUBY_DARK, scale, Main.rand.Next(20, 40), 5, DelegateAction));
			}
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D swordTex = TextureAssets.Projectile[Type].Value;
		float diagonalOrigin = 0.2f;
		diagonalOrigin = Lerp(diagonalOrigin, 0, EaseQuadIn.Ease(EaseSine.Ease(GetSwingProgress())));
		Vector2 origin = new Vector2(diagonalOrigin, 1 - diagonalOrigin) * swordTex.Size();
		if (Projectile.spriteDirection < 0)
			origin = new Vector2(1 - diagonalOrigin) * swordTex.Size();

		DrawTrail();

		if (Combo != 2 || (AiTimer / SwingTime) >= WINDUP_TIME)
			DrawAfterimages(lightColor, origin);

		Projectile.QuickDraw(origin: origin);
		if (Combo == 2)
			EmpoweredGlow(origin);

		return false;
	}

	private void DrawAfterimages(Color lightColor, Vector2 origin)
	{
		Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
		float baseOpacity = 0.4f;

		for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Type]; i++)
		{
			if (oldScale[i] == 0)
				return;

			float progress = i / (float)ProjectileID.Sets.TrailCacheLength[Type];
			float opacity = baseOpacity * EaseQuadIn.Ease(1 - progress);
			SpriteEffects effect = Projectile.oldSpriteDirection[i] == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor) * opacity, Projectile.oldRot[i], origin, oldScale[i], effect, 0);
		}
	}

	private void DrawTrail()
	{
		Projectile.TryGetOwner(out Player Owner);
		float progress = AiTimer / SwingTime;
		float swingProgress = GetSwingProgress();
		float trailDirection = SwingDirection;
		float maxDist = 1f;
		float minDist = 0.6f;
		bool useLightColor = true;
		float opacityMod = 1f;
		float minWidth = 0.55f;
		float maxWidth = 0.75f;
		float angleRangeStretch = 1.1f;
		Effect effect;

		//Switch trail direction during the double hit swing
		if (Combo == 1)
		{
			if (progress < 0.5f)
				trailDirection *= -1;

			trailDirection *= Owner.direction;
			opacityMod = 0.8f;
		}

		//Fix trail drawing to account for windup swing, set different params on windup swing
		if (Combo == 2)
		{
			if (progress < WINDUP_TIME)
				return;

			progress = (progress - WINDUP_TIME) / (1 - WINDUP_TIME);
			SetRedtrailParams(out effect, progress, swingProgress);
			useLightColor = false;
			minDist = 0.8f;
		}
		else
			SetSandtrailParams(out effect, progress, swingProgress);

		Vector2 pos = Owner.MountedCenter - Main.screenPosition;
		var slash = new PrimitiveSlashArc
		{
			BasePosition = pos,
			MinDistance = Projectile.Size.Length() * minDist * SizeModifier,
			MaxDistance = Projectile.Size.Length() * maxDist * SizeModifier,
			Width = Projectile.Size.Length() * minWidth * SizeModifier,
			MaxWidth = Projectile.Size.Length() * maxWidth * SizeModifier,
			AngleRange = new Vector2(SwingRadians / 2 * trailDirection, -SwingRadians / 2 * trailDirection) * (Owner.direction * -1) * angleRangeStretch,
			DirectionUnit = Vector2.Normalize(BaseDirection),
			Color = Color.White * opacityMod,
			UseLightColor = useLightColor,
			DistanceEase = EaseQuinticIn,
			SlashProgress = swingProgress,
			RectangleCount = 30
		};
		PrimitiveRenderer.DrawPrimitiveShape(slash, effect);
	}

	private static void SetSandtrailParams(out Effect effect, float progress, float swingProgress)
	{
		effect = AssetLoader.LoadedShaders["NoiseParticleTrail"];
		effect.Parameters["baseTexture"].SetValue(AssetLoader.LoadedTextures["noise"].Value);
		effect.Parameters["baseColorDark"].SetValue(SAND_DARK.ToVector4());
		effect.Parameters["baseColorLight"].SetValue(SAND_LIGHT.ToVector4());
		effect.Parameters["overlayTexture"].SetValue(AssetLoader.LoadedTextures["particlenoise"].Value);
		effect.Parameters["overlayColor"].SetValue(SAND_PARTICLE.ToVector4());

		effect.Parameters["coordMods"].SetValue(new Vector2(1.8f, 0.3f));
		effect.Parameters["overlayCoordMods"].SetValue(new Vector2(1f, 0.1f));
		effect.Parameters["overlayScrollMod"].SetValue(new Vector2(0.4f, -1f));
		effect.Parameters["overlayExponentRange"].SetValue(new Vector2(5f, 0.5f));

		effect.Parameters["timer"].SetValue(-swingProgress * 0.5f - progress * 0.2f);
		effect.Parameters["progress"].SetValue(swingProgress);
		effect.Parameters["intensity"].SetValue(2.5f);
		effect.Parameters["opacity"].SetValue(EaseCircularOut.Ease(EaseSine.Ease(EaseQuadOut.Ease(swingProgress))));
	}

	private static void SetRedtrailParams(out Effect effect, float progress, float swingProgress)
	{
		effect = AssetLoader.LoadedShaders["NoiseParticleTrail"];
		effect.Parameters["baseTexture"].SetValue(AssetLoader.LoadedTextures["noiseCrystal"].Value);
		effect.Parameters["baseColorDark"].SetValue(RUBY_DARK.ToVector4());
		effect.Parameters["baseColorLight"].SetValue(RUBY_LIGHT.ToVector4());
		effect.Parameters["overlayTexture"].SetValue(AssetLoader.LoadedTextures["vnoise"].Value);
		effect.Parameters["overlayColor"].SetValue(RUBY_PARTICLE.ToVector4());

		effect.Parameters["coordMods"].SetValue(new Vector2(4f, 1f));
		effect.Parameters["overlayCoordMods"].SetValue(new Vector2(1f, 0.1f) * 3f);
		effect.Parameters["overlayScrollMod"].SetValue(new Vector2(0.4f, -1f));
		effect.Parameters["overlayExponentRange"].SetValue(new Vector2(20f, 5f));

		effect.Parameters["timer"].SetValue(-swingProgress * 0.5f - progress * 0.2f);
		effect.Parameters["progress"].SetValue(swingProgress);
		effect.Parameters["intensity"].SetValue(3f);
		effect.Parameters["opacity"].SetValue(EaseCircularOut.Ease(EaseSine.Ease(EaseQuadOut.Ease(swingProgress))));
	}

	private void EmpoweredGlow(Vector2 origin)
	{
		float progress = AiTimer / SwingTime;
		float windupProgress = (progress) / WINDUP_TIME;
		windupProgress = Min(windupProgress, 1);

		Texture2D starTex = AssetLoader.LoadedTextures["Star"].Value;
		GlowmaskProjectile.ProjIdToGlowmask.TryGetValue(Type, out GlowmaskInfo glowmaskInfo);
		Texture2D glowmaskTex = glowmaskInfo.Glowmask.Value;

		//Draw a star at the hilt
		var center = Projectile.Center - Main.screenPosition;
		float maxSize = 0.6f * Projectile.scale;
		float starProgress = EaseSine.Ease(windupProgress);

		Vector2 scale = new Vector2(1f, 1f) * Lerp(0, maxSize, starProgress) * 0.7f;
		var starOrigin = starTex.Size() / 2;
		Color color = Projectile.GetAlpha(Color.Lerp(RUBY_LIGHT.Additive(), RUBY_DARK.Additive(), starProgress)) * EaseQuadOut.Ease(starProgress);
		Main.spriteBatch.Draw(starTex, center, null, color, Projectile.rotation, starOrigin, scale, SpriteEffects.None, 0);

		//Glowmask drawing
		float glowProgress = EaseCircularIn.Ease(windupProgress);
		color = RUBY_DARK.Additive() * glowProgress * EaseCircularOut.Ease(1 - progress);
		SpriteEffects effects = (Projectile.spriteDirection < 0) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
		for (int i = 0; i < 6; i++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy((TwoPi * i / 6) + Projectile.rotation);

			Main.spriteBatch.Draw(glowmaskTex, center + offset, null, color * 0.33f, Projectile.rotation, origin, Projectile.scale, effects, 0);
		}
	}

	public override void SendExtraAI(BinaryWriter writer)
	{
		writer.WriteVector2(BaseDirection);
		writer.Write(SwingDirection);
		writer.Write(SwingRadians);
		writer.Write(SizeModifier);
		writer.Write(SwingTime);
		writer.Write(Combo);
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
		BaseDirection = reader.ReadVector2();
		SwingDirection = reader.ReadInt32();
		SwingRadians = reader.ReadSingle();
		SizeModifier = reader.ReadSingle();
		SwingTime = reader.ReadInt32();
		Combo = reader.ReadInt32();
	}
}