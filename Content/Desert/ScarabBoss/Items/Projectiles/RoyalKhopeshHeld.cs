using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.ProjectileCommon;
using System.IO;
using static SpiritReforged.Common.Easing.EaseFunction;
using static Microsoft.Xna.Framework.MathHelper;
using Terraria.Audio;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Easing;
using SpiritReforged.Content.Underground.Moss.MossFlasks;
using SpiritReforged.Content.Aether.Items;
using Microsoft.Xna.Framework.Graphics;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using Terraria;
using Terraria.Graphics.CameraModifiers;
using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items.Projectiles;
public class RoyalKhopeshHeld : ModProjectile
{
	public static readonly SoundStyle EmpoweredHit = SoundID.DD2_SonicBoomBladeSlash;
	public static readonly SoundStyle RegularSlash = new SoundStyle("SpiritReforged/Assets/SFX/Projectile/SwordSlash1");
	public static readonly SoundStyle EmpoweredSlash_01 = new SoundStyle("SpiritReforged/Assets/SFX/Item/BigSwing") with { Volume = 0.5f, Pitch = 0.5f, PitchVariance = 0.1f };
	public static readonly SoundStyle EmpoweredSlash_02 = new SoundStyle("SpiritReforged/Assets/SFX/Projectile/Explosion_Liquid") with { Volume = 0.5f, Pitch = 0.5f, PitchVariance = 0.1f };

	internal static readonly Color OutlineColor = new Color(129, 88, 53);
	internal static readonly Color DarkYellow = new Color(179, 148, 54);
	internal static readonly Color LightYellow = new Color(255, 220, 79);
	public float Combo => Projectile.ai[0];
	public ref float MaxTime => ref Projectile.ai[1];
	public ref float OriginalDirection => ref Projectile.ai[2];
	public Player Owner => Main.player[Projectile.owner];

	public bool _empoweredStrike;
	private float _originalScale;
	public override void SetDefaults()
	{
		Projectile.penetrate = -1;
		Projectile.friendly = false;
		Projectile.DamageType = DamageClass.Melee;

		Projectile.tileCollide = false;
		Projectile.Size = new Vector2(32);

		Projectile.ownerHitCheck = true;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = 25;

		Projectile.extraUpdates = 1;
	}

	public override void AI()
	{
		if (!Projectile.friendly)
			Initialize();

		UpdateHeldProjectile();

		if (_empoweredStrike)
			EmpoweredSlash();
		else
			Slash();

		UpdateVisuals();
	}

	// TODO: balance this probably
	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		if (_empoweredStrike)
		{
			modifiers.FinalDamage *= 2f;
			modifiers.SetCrit();
		}	
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		// gets the edge of the targets hitbox
		var position = target.Center - target.Size.RotatedBy(target.DirectionTo(Projectile.Center).ToRotation() - MathHelper.PiOver4) / 4f;
		
		if (_empoweredStrike)
		{
			SoundEngine.PlaySound(EmpoweredHit, Owner.Center);

			for (int i = 0; i < 8; i++)
			{
				var velocity = Projectile.Center.DirectionTo(target.Center).RotatedByRandom(0.5f) * Main.rand.NextFloat(4f);

				ParticleHandler.SpawnParticle(new ImpactLine(position, velocity, Color.MediumVioletRed.Additive(), new Vector2(0.2f, 0.4f), 15));
				ParticleHandler.SpawnParticle(new ImpactLine(position, velocity, new Color(255, 255, 255, 0).Additive(), new Vector2(0.2f, 0.4f), 15));
			}

			ParticleHandler.SpawnParticle(new ImpactLinePrim(position, Vector2.Zero, Color.MediumVioletRed.Additive(), new(0.66f, 2.25f), 10, 1));
			ParticleHandler.SpawnParticle(new ImpactLinePrim(position, Vector2.Zero, Color.White.Additive(), new(0.66f, 2.25f), 10, 1));
			ParticleHandler.SpawnParticle(new LightBurst(position, Main.rand.NextFloatDirection(), Color.MediumVioletRed.Additive(), 0.66f, 25));
		}
	}

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		int inflationSize = _empoweredStrike ? 50 : 20;
		inflationSize = (int)(inflationSize * _originalScale);

		Rectangle hitBox = Projectile.getRect();
		hitBox.Inflate(inflationSize, inflationSize);

		return hitBox.Intersects(targetHitbox);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Main.instance.LoadProjectile(985);

		var tex = TextureAssets.Projectile[Type].Value;
		var texWhite = TextureColorCache.ColorSolid(tex, Color.White);
		var star = AssetLoader.LoadedTextures["Star"].Value;
		var bloom = AssetLoader.LoadedTextures["Bloom"].Value;

		SpriteEffects flip = Owner.direction == -1 ? SpriteEffects.FlipHorizontally : 0;

		float rot = 0f;

		if (Combo == 1)
		{
			rot = MathHelper.PiOver2 * OriginalDirection;
			flip = OriginalDirection == -1 ? 0 : SpriteEffects.FlipHorizontally;
		}

		float fadeOut = 1f;

		if (Projectile.timeLeft < MaxTime / 3)
			fadeOut = Projectile.timeLeft / (MaxTime / 3f);

		float progress = Projectile.timeLeft / MaxTime;

		float lerp = 1f - progress;

		if (!_empoweredStrike)
			DrawSmear(progress);

		var drawPos = Projectile.Center + new Vector2(0f, Owner.gfxOffY);

		Main.spriteBatch.Draw(tex, drawPos - Main.screenPosition, null, lightColor * fadeOut,
			  Projectile.rotation + (Owner.direction == -1 ? MathHelper.TwoPi : 0f) + rot, tex.Size() / 2f, Projectile.scale, flip, 0f);
		
		if (_empoweredStrike)
			DrawEmpoweredSmear(progress);

		Vector2 handlePos = Projectile.Center + new Vector2(-10f * Owner.direction, 10f).RotatedBy(Projectile.rotation) * _originalScale + new Vector2(0f, Owner.gfxOffY);

		if (_empoweredStrike && lerp < 0.6f)
		{
			float prog = EaseBuilder.EaseCircularIn.Ease(1f - lerp / 0.6f);
			
			Main.spriteBatch.Draw(texWhite, drawPos - Main.screenPosition, null, Color.MediumVioletRed.Additive() * prog,
			  Projectile.rotation + (Owner.direction == -1 ? MathHelper.TwoPi : 0f) + rot, texWhite.Size() / 2f, Projectile.scale, flip, 0f);

			Main.spriteBatch.Draw(bloom, handlePos - Main.screenPosition, null, Color.MediumVioletRed.Additive() * prog,
			  0f, bloom.Size() / 2f, new Vector2(0.3f + 1f * prog, 0.3f), 0, 0f);

			Main.spriteBatch.Draw(star, handlePos - Main.screenPosition, null, Color.MediumVioletRed.Additive() * prog,
			  0f, star.Size() / 2f, new Vector2(0.3f + 1f * prog, 0.3f), 0, 0f);
			
			Main.spriteBatch.Draw(star, handlePos - Main.screenPosition, null, Color.Orange.Additive() * prog,
			  0f, star.Size() / 2f, new Vector2(0.2f + 1f * prog, 0.2f), 0, 0f);
		}

		return false;
	}
	private void DrawEmpoweredSmear(float progress)
	{
		float lerp = 1f - progress;

		var smear = TextureAssets.Projectile[985].Value;

		var source = smear.Frame(1, 4, 0, (int)MathHelper.Lerp(0, 5.5f, EaseBuilder.EaseQuinticOut.Ease(1f - progress)));
		var pos = Owner.Center + new Vector2(90f, -90f).RotatedBy(Projectile.rotation - (Owner.direction == -1 ? MathHelper.PiOver2 : 0f)) * _originalScale - Main.screenPosition;

		Main.EntitySpriteDraw(smear, pos, source, OutlineColor, Projectile.rotation - MathHelper.PiOver4 - (Owner.direction == -1 ? MathHelper.PiOver2 : 0f),
			new Vector2(source.Width, source.Height / 2), _originalScale * 1.2f, 0, 0);

		Main.EntitySpriteDraw(smear, pos, source, DarkYellow, Projectile.rotation - MathHelper.PiOver4 - (Owner.direction == -1 ? MathHelper.PiOver2 : 0f),
			new Vector2(source.Width, source.Height / 2), _originalScale * 1.15f, 0, 0);

		Main.EntitySpriteDraw(smear, pos, source, LightYellow, Projectile.rotation - MathHelper.PiOver4 - (Owner.direction == -1 ? MathHelper.PiOver2 : 0f),
			new Vector2(source.Width, source.Height / 2), _originalScale * 1.1f, 0, 0);
		
		Main.EntitySpriteDraw(smear, pos, source, Color.OrangeRed.Additive() * (lerp < 0.1f ? lerp / 0.1f : 0f), Projectile.rotation - MathHelper.PiOver4 - (Owner.direction == -1 ? MathHelper.PiOver2 : 0f),
			new Vector2(source.Width, source.Height / 2), _originalScale * 1.45f, 0, 0);

		Main.EntitySpriteDraw(smear, pos, source, Color.MediumVioletRed.Additive() * (lerp < 0.1f ? lerp / 0.1f : 0f), Projectile.rotation - MathHelper.PiOver4 - (Owner.direction == -1 ? MathHelper.PiOver2 : 0f),
			new Vector2(source.Width, source.Height / 2), _originalScale * 1.5f, 0, 0);
	}

	private void DrawSmear(float progress)
	{
		float lerp = 1f - progress;

		float fadeIn;
		if (lerp < 0.6f)
			fadeIn = lerp / 0.6f;
		else
			fadeIn = (lerp - 0.6f) / 0.4f;

		var smear = TextureAssets.Projectile[985].Value;

		var source = smear.Frame(1, 4, 0, (int)MathHelper.Lerp(0, 5f, EaseBuilder.EaseQuinticOut.Ease(1f - progress)));
		var pos = Owner.Center + new Vector2(75f, -70f).RotatedBy(Projectile.rotation - (Owner.direction == -1 ? MathHelper.PiOver2 : 0f)) * _originalScale - Main.screenPosition;

		Main.EntitySpriteDraw(smear, pos, source, OutlineColor * fadeIn, Projectile.rotation - MathHelper.PiOver4 - (Owner.direction == -1 ? MathHelper.PiOver2 : 0f),
			new Vector2(source.Width, source.Height / 2), _originalScale * 1.05f, 0, 0);

		Main.EntitySpriteDraw(smear, pos, source, DarkYellow * fadeIn, Projectile.rotation - MathHelper.PiOver4 - (Owner.direction == -1 ? MathHelper.PiOver2 : 0f),
			new Vector2(source.Width, source.Height / 2), _originalScale * 1f, 0, 0);

		Main.EntitySpriteDraw(smear, pos, source, LightYellow * fadeIn, Projectile.rotation - MathHelper.PiOver4 - (Owner.direction == -1 ? MathHelper.PiOver2 : 0f),
			new Vector2(source.Width, source.Height / 2), _originalScale * 0.9f, 0, 0);
	}

	private void EmpoweredSlash()
	{
		float swingStart = Combo == -1 ? -3.5f : 1.25f;
		float swingEnd = Combo == -1 ? 1.25f : -3.5f;

		float progress = 1f - Projectile.timeLeft / MaxTime;

		Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

		float armRotation = Projectile.velocity.ToRotation() + (OriginalDirection == -1 ? MathHelper.Pi : 0f);

		Projectile.rotation += MathHelper.Lerp(swingStart, swingEnd, EaseBuilder.EaseQuinticOut.Ease(progress)) * OriginalDirection;

		armRotation += MathHelper.Lerp(swingStart, swingEnd, EaseBuilder.EaseQuinticOut.Ease(progress)) * OriginalDirection;

		if (progress < 0.3f)
			Projectile.scale = _originalScale * MathHelper.Lerp(1.1f, 1.3f, progress / 0.3f);

		if (progress > 0.7f)
		{
			float lerp = (progress - 0.7f) / 0.3f;

			Projectile.scale = _originalScale * MathHelper.Lerp(1.3f, 1.1f, lerp);

			Projectile.rotation += MathHelper.Lerp(0f, -0.5f * Combo, EaseBuilder.EaseCircularIn.Ease(lerp)) * OriginalDirection;
		}

		Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);

		Projectile.Center = Owner.MountedCenter + new Vector2(40f * OriginalDirection, -25f).RotatedBy(Projectile.rotation);
	}

	private void Slash()
	{
		float swingStart = Combo == -1 ? -3f : 1f;
		float swingEnd = Combo == -1 ? 1f : -3f;

		float progress = 1f - Projectile.timeLeft / MaxTime;

		Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

		float armRotation = Projectile.velocity.ToRotation() + (OriginalDirection == -1 ? MathHelper.Pi : 0f);

		Projectile.rotation += MathHelper.Lerp(swingStart, swingEnd, EaseBuilder.EaseQuinticOut.Ease(progress)) * OriginalDirection;

		armRotation += MathHelper.Lerp(swingStart, swingEnd, EaseBuilder.EaseQuinticOut.Ease(progress)) * OriginalDirection;

		if (progress < 0.3f)
			Projectile.scale = _originalScale * MathHelper.Lerp(0.9f, 1.1f, progress / 0.3f);

		if (progress > 0.7f)
		{
			float lerp = (progress - 0.7f) / 0.3f;
			
			Projectile.scale = _originalScale * MathHelper.Lerp(1.1f, 0.9f, lerp);

			Projectile.rotation += MathHelper.Lerp(0f, -0.5f * Combo, EaseBuilder.EaseCircularIn.Ease(lerp)) * OriginalDirection;
		}

		Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);

		Projectile.Center = Owner.MountedCenter + new Vector2(40f * OriginalDirection, -25f).RotatedBy(Projectile.rotation);
	}

	private void UpdateVisuals()
	{
		float progress = Projectile.timeLeft / MaxTime;

		if (Projectile.timeLeft % 2 == 0 && _empoweredStrike)
		{
			var tipPosition = Projectile.Center + new Vector2(30f * OriginalDirection, -40f).RotatedBy(Projectile.rotation) * _originalScale;

			Dust.NewDustPerfect(tipPosition,
				DustID.Sand, Main.rand.NextVector2Circular(5f, 5f), 150, default, 1.5f * progress).noGravity = true;

			Color smokeColor = new Color(223, 219, 147) * 0.25f * progress;
			float scale = Main.rand.NextFloat(0.1f, 0.15f);
			var velSmoke = Vector2.UnitX * OriginalDirection * 1.5f;
			ParticleHandler.SpawnParticle(new SmokeCloud(tipPosition + Main.rand.NextVector2Circular(5f, 5f), velSmoke, smokeColor, scale, EaseQuadOut, Main.rand.Next(30, 40)));

			static void DecelerateAction(Particle p) => p.Velocity *= 0.925f;

			var velocity = Main.rand.NextVector2Circular(2f, 2f);

			ParticleHandler.SpawnParticle(new GlowParticle(tipPosition, velocity, Color.MediumVioletRed.Additive(), 1f * progress, 20, 1, DecelerateAction));
			ParticleHandler.SpawnParticle(new GlowParticle(tipPosition, velocity, Color.White, 0.5f * progress, 20, 1, DecelerateAction));
		}
	}

	private void UpdateHeldProjectile()
	{
		if (Owner is null || Owner.HeldItem.ModItem is not RoyalKhopesh)
			Projectile.Kill();

		Owner.heldProj = Projectile.whoAmI;

		Owner.ChangeDir(Projectile.direction);
	}

	internal void Initialize()
	{
		Owner.TryGetModPlayer<RoyalKhopeshPlayer>(out var kopeshPlayer);

		Projectile.friendly = true;

		_originalScale = Owner.HeldItem.scale;
		Owner.ApplyMeleeScale(ref _originalScale);

		if (kopeshPlayer.EmpoweredStrikeTimer > 0)
		{
			Projectile.timeLeft = Owner.itemTimeMax;
			_empoweredStrike = true;
			kopeshPlayer.EmpoweredStrikeTimer = 0;

			if (Main.myPlayer == Projectile.owner)
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(Projectile.Center, Projectile.velocity, 2f, 3, 15));

			SoundEngine.PlaySound(EmpoweredSlash_01, Owner.Center);
			SoundEngine.PlaySound(EmpoweredSlash_02, Owner.Center);
		}		
		else
		{
			Projectile.timeLeft = (int)(Owner.itemTimeMax * Main.rand.NextFloat(0.9f, 1.1f));
			if (kopeshPlayer.FastStrikeAmount > 0)
			{
				Projectile.timeLeft /= 2;
				kopeshPlayer.FastStrikeAmount--;
				SoundEngine.PlaySound(RegularSlash with { Volume = 0.5f, Pitch = 1.1f, PitchVariance = 0.1f}, Owner.Center);
			}
			else
				SoundEngine.PlaySound(RegularSlash with { Volume = 0.6f }, Owner.Center);
		}		

		MaxTime = Projectile.timeLeft;
		Projectile.rotation = Owner.DirectionTo(Projectile.Center + Projectile.velocity).ToRotation() + MathHelper.PiOver4;
		Projectile.netUpdate = true;
		Projectile.direction = Main.MouseWorld.X < Owner.Center.X ? -1 : 1;
		OriginalDirection = Projectile.direction;
		Owner.itemTime = Projectile.timeLeft / 2;
		Owner.itemAnimation = Owner.itemTime;
	}
}