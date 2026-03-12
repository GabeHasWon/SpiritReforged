using Microsoft.Xna.Framework.Graphics;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Particles;
using System.IO;
using System.Linq;
using static Terraria.Player;

namespace SpiritReforged.Content.Desert.Scarabeus.Items.Projectiles;

[AutoloadGlowmask("255,255,255", false)]
public class SunriseScepterHeld : ModProjectile
{
	internal ref float Timer => ref Projectile.ai[0];
	internal ref float MaxTimer => ref Projectile.ai[1];
	internal ref float AttackCooldown => ref Projectile.ai[2];
	internal float Progress => Timer / MaxTimer;
	internal Player Owner => Main.player[Projectile.owner];

	internal bool _dying;
	internal int _deathTimer;

	internal Vector2 OrbPosition;
	public override void SetDefaults() 
	{
		Projectile.width = 32;
		Projectile.height = 32;
		Projectile.friendly = false;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
	}

	public override void AI()
	{
		if (MaxTimer == 0)
			Initialize();

		UpdateHeldProjectile();

		float armRotation;

		if (Progress < 0.2f)
		{
			float easedProgress = EaseBuilder.EaseCircularIn.Ease(Progress / 0.2f);

			Projectile.Center += new Vector2(0f, -MathHelper.Lerp(0f, 15f, easedProgress));

			Projectile.rotation = -MathHelper.Lerp(0f, 0.2f, easedProgress) * Projectile.direction;

			armRotation = -MathHelper.Lerp(0f, 2.5f, easedProgress) * Projectile.direction;

			Vector2 targetPosition = Owner.Center + new Vector2(-30f * Owner.direction, -MathHelper.Lerp(0f, 90f, EaseBuilder.EaseCircularIn.Ease(Progress / 0.2f)));

			OrbPosition = Vector2.Lerp(OrbPosition, targetPosition, 0.25f);
		}
		else if (Progress < 0.5f)
		{
			float progress = (Progress - 0.2f) / 0.3f;

			Projectile.Center += new Vector2(0f, -MathHelper.Lerp(15f, 40f, EaseBuilder.EaseCircularOut.Ease(progress)));
			Projectile.Center += Main.rand.NextVector2Circular(progress * 2f, progress * 2f);

			Projectile.rotation = -MathHelper.Lerp(0.2f, 0.1f, progress) * Projectile.direction;

			armRotation = -2.5f * Projectile.direction;

			Vector2 targetPosition = Owner.Center + new Vector2(-30f * Owner.direction, -MathHelper.Lerp(90f, 150f, EaseBuilder.EaseQuarticOut.Ease(progress)));

			OrbPosition = Vector2.Lerp(OrbPosition, targetPosition, 0.3f);
		}
		else
		{
			float progress = (Progress - 0.5f) / 0.5f;

			Projectile.Center += new Vector2(0f, -MathHelper.Lerp(40f, 15f, EaseBuilder.EaseCircularIn.Ease(progress)));
			Projectile.Center += Main.rand.NextVector2Circular((1f - progress) * 2f, (1f - progress) * 2f);

			Projectile.rotation = -MathHelper.Lerp(0.1f, -0.1f, progress) * Projectile.direction;

			armRotation = -2.5f * Projectile.direction;

			Vector2 targetPosition = Owner.Center + new Vector2(-30f * Owner.direction, -MathHelper.Lerp(150f, 90f, EaseBuilder.EaseQuarticIn.Ease(progress)));

			OrbPosition = Vector2.Lerp(OrbPosition, targetPosition, 0.3f);

			if (Progress >= 1f)
				Projectile.Kill();
		}

		if (Progress > 0.2f && Progress < 0.9f)
		{
			if (AttackCooldown <= 0)
			{
				NPC target = Main.npc.Where(n => n.CanBeChasedBy() && n.Distance(OrbPosition) < 250f).OrderBy(n => n.Distance(OrbPosition)).FirstOrDefault();

				if (target != default)
				{
					if (Main.myPlayer == Projectile.owner)
					{
						Projectile.NewProjectile(Projectile.GetSource_FromAI("SunriseScepterOrb_Shot"), target.Center, Vector2.Zero,
												ModContent.ProjectileType<SunriseScepterShot>(), Projectile.damage, Projectile.knockBack, Projectile.owner, Projectile.whoAmI);
					}

					AttackCooldown = 15;
				}
			}
			else
				AttackCooldown--;
		}

		Owner.SetCompositeArmFront(true, CompositeArmStretchAmount.Full, armRotation);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		DrawOrb();

		Texture2D texture = TextureAssets.Projectile[Type].Value;
		Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;
		Texture2D godray = AssetLoader.LoadedTextures["GodrayCircle"].Value;
		Texture2D star = AssetLoader.LoadedTextures["Star"].Value;

		SpriteEffects flip = Owner.direction == -1 ? SpriteEffects.FlipHorizontally : 0f;

		float rot = Projectile.rotation - MathHelper.PiOver4 + (flip == SpriteEffects.FlipHorizontally ? MathHelper.PiOver2 : 0f);

		float fade = 1f;

		if (Progress < 0.1f)
			fade = EaseBuilder.EaseCircularIn.Ease(Progress / 0.1f);

		if (Progress > 0.8f)
			fade = 1f - EaseBuilder.EaseCircularIn.Ease((Progress - 0.8f) / 0.2f);

		Vector2 tipPosition = Projectile.Center + new Vector2(-1 * Owner.direction, -18).RotatedBy(Projectile.rotation);
		
		Main.EntitySpriteDraw(godray, tipPosition - Main.screenPosition, null, new Color(255, 255, 0, 0) * fade * 0.25f, 0, godray.Size() / 2f, 0.1f, flip);

		Main.EntitySpriteDraw(bloom, tipPosition - Main.screenPosition, null, new Color(255, 255, 0, 0) * fade * 0.25f, 0, bloom.Size() / 2f, 0.35f, flip);

		Main.EntitySpriteDraw(godray, tipPosition - Main.screenPosition, null, new Color(255, 50, 0, 0) * fade * 0.15f, 0, godray.Size() / 2f, 0.1f, flip);

		Main.EntitySpriteDraw(bloom, tipPosition - Main.screenPosition, null, new Color(255, 50, 0, 0) * fade * 0.15f, 0, bloom.Size() / 2f, 0.35f, flip);

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor * fade, rot, texture.Size() / 2f, Projectile.scale, flip);

		return false;
	}

	internal void DrawOrb()
	{
		// TODO: change this LMFAO
		Texture2D texture = TextureAssets.Projectile[ModContent.ProjectileType<SunriseScepterOrb>()].Value;

		Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;
		Texture2D godray = AssetLoader.LoadedTextures["GodrayCircle"].Value;
		Texture2D star = AssetLoader.LoadedTextures["Star"].Value;

		float fade = 1f;

		if (Progress < 0.4f)
			fade = EaseBuilder.EaseCircularInOut.Ease(Progress / 0.4f);

		if (Progress > 0.8f)
			fade = 1f - EaseBuilder.EaseCircularIn.Ease((Progress - 0.8f) / 0.2f);
		
		float scale = Projectile.scale;

		Main.EntitySpriteDraw(texture, OrbPosition - Main.screenPosition, null, Color.White * fade, 0f, texture.Size() / 2f, scale, 0);

		Main.EntitySpriteDraw(bloom, OrbPosition - Main.screenPosition, null, new Color(255, 205, 0, 0) * fade, 0f, bloom.Size() / 2f, scale * 0.5f, 0);

		Main.EntitySpriteDraw(bloom, OrbPosition - Main.screenPosition, null, new Color(255, 55, 0, 0) * fade, 0f, bloom.Size() / 2f, scale * 0.35f, 0);

		Main.EntitySpriteDraw(godray, OrbPosition - Main.screenPosition, null, new Color(255, 205, 0, 0) * fade, 0f, godray.Size() / 2f, scale * 0.2f, 0);
	}

	internal void Initialize()
	{
		OrbPosition = Owner.Center + new Vector2(-30f * Owner.direction, 0);

		MaxTimer = Owner.itemTimeMax * 4;

		/*if (Main.myPlayer == Projectile.owner)
		{
			PreNewProjectile.New(Projectile.GetSource_FromAI("SunriseScepter_SpawnOrb"), Owner.Center, Vector2.Zero,
				ModContent.ProjectileType<SunriseScepterOrb>(), Projectile.damage, 2f, Projectile.owner, 0, MaxTimer);
		}*/
	}

	internal void UpdateHeldProjectile()
	{
		if (Timer < MaxTimer)
			Timer++;
		else if (!_dying)
		{
			_deathTimer = 20;
			_dying = true;
		}
		
		Owner.heldProj = Projectile.whoAmI;

		Projectile.timeLeft = 2;
		Owner.itemTime = 2;
		Owner.itemAnimation = 2;

		Projectile.Center = Owner.Center;
		Projectile.direction = Owner.direction;
	}
}