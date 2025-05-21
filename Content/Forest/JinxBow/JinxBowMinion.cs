using Microsoft.Xna.Framework.Graphics;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.MathHelpers;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria;
using Terraria.Audio;

namespace SpiritReforged.Content.Forest.JinxBow;

public class JinxBowMinion() : BaseMinion(500, 900, new Vector2(12, 12))
{
	private readonly int attackCooldown = 60;
	private readonly int bounceTime = 30;

	private int _bounceTimer = 0;

	private ref float AiTimer => ref Projectile.ai[0];

	public override void AbstractSetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = 5;
		ProjectileID.Sets.TrailingMode[Type] = 0;
	}

	public override void AbstractSetDefaults() => Projectile.minionSlots = 0f;

	public override bool PreAI()
	{
		Player mp = Main.player[Projectile.owner];

		if (mp.HasAccessory<JinxBow>())
			Projectile.timeLeft = 2;

		return true;
	}

	public override void AI()
	{
		base.AI();
		AiTimer = Math.Max(0, AiTimer - 1);
		_bounceTimer = Math.Max(0, _bounceTimer - 1);
	}

	public override bool? CanDamage() => false;

	public override void IdleMovement(Player player)
	{
		var desiredPos = new Vector2((int)player.MountedCenter.X - player.direction * 40, (int)player.MountedCenter.Y - 20 + (float)Math.Sin(Main.GameUpdateCount / 30f) * 5 + player.gfxOffY);

		AiTimer = attackCooldown;
		_bounceTimer = 0;
		Projectile.frame = 0;
		Projectile.spriteDirection = Projectile.direction = player.direction;
		Projectile.velocity = Vector2.Zero;

		Projectile.position = Vector2.Lerp(Projectile.position + Projectile.Size / 2, desiredPos, 0.06f) - Projectile.Size / 2;
		Projectile.rotation = Projectile.rotation.AngleLerp((Projectile.position - Projectile.oldPosition).X * 0.1f, 0.07f);
	}

	public override void TargettingBehavior(Player player, NPC target)
	{
		Projectile.rotation = Utils.AngleLerp(Projectile.rotation, Projectile.AngleTo(target.Center), 0.2f); 
		
		var desiredPos = new Vector2((int)player.MountedCenter.X, (int)player.MountedCenter.Y - 40 + (float)Math.Sin(Main.GameUpdateCount / 30f) * 5 + player.gfxOffY);
		desiredPos += desiredPos.DirectionTo(target.Center) * 20;

		Projectile.spriteDirection = Projectile.direction = 1;
		Projectile.position = Vector2.Lerp(Projectile.position + Projectile.Size / 2, desiredPos, 0.1f) - Projectile.Size / 2;

		if (AiTimer <= 0)
		{
			AiTimer = attackCooldown;

			FindAmmo(player, AmmoID.Arrow, out int? projToFire, out int? ammoDamage, out float? ammoKB, out float? ammoVel);

			int type = projToFire ?? ProjectileID.WoodenArrowFriendly;

			float speed = 10 + ammoVel ?? 0;
			float ticksFromTarget = Projectile.Distance(target.Center) / speed;
			Vector2 arrowVelocity = Projectile.DirectionTo(target.Center + target.velocity * ticksFromTarget / 2) * speed;
			float knockBack = Projectile.knockBack + (ammoKB ?? 0);
			int damage = Projectile.damage + (ammoDamage ?? 0);

			_bounceTimer = bounceTime;

			PreNewProjectile.New(Projectile.GetSource_FromThis(), Projectile.Center, arrowVelocity, type, damage, knockBack, Projectile.owner, preSpawnAction: p => 
			{
				p.DamageType = DamageClass.Summon; 

				//Can't really change the static projectile id set of minion shots, so this is the only way to make whip effects work (why is this still the case with summon damage class existing)
				p.minion = true;
				p.GetGlobalProjectile<JinxBowShot>().IsJinxbowShot = true;
			});

			if(!Main.dedServ)
			{
				SoundEngine.PlaySound(SoundID.DD2_PhantomPhoenixShot with { Pitch = 1.25f }, Projectile.Center);

				Main.instance.LoadProjectile(type);
				Texture2D arrowTex = TextureAssets.Projectile[type].Value;
				Color color = TextureColorCache.GetBrightestColor(arrowTex);

				ParticleHandler.SpawnParticle(new ImpactLinePrim(Projectile.Center + arrowVelocity * 2f, arrowVelocity * 0.3f, color.Additive() * 0.66f, new(0.66f, 3f), 10, 1));
			}
		}
	}

	private static void FindAmmo(Player owner, int ammoID, out int? projToFire, out int? ammoDamage, out float? ammoKB, out float? ammoVel)
	{
		const int ammoInventoryStart = 54;
		const int ammoInventoryEnd = 58;

		projToFire = null;
		ammoDamage = null;
		ammoKB = null;
		ammoVel = null;

		for(int i = ammoInventoryStart; i < ammoInventoryEnd; i++)
		{
			Item selectedItem = owner.inventory[i];
			if (selectedItem.ammo == ammoID && selectedItem.stack > 0)
			{
				projToFire = selectedItem.shoot;
				ammoDamage = selectedItem.damage;
				ammoKB = selectedItem.knockBack;
				ammoVel = selectedItem.shootSpeed;
				return;
			}
		}

		for(int i = 0; i < ammoInventoryStart; i++)
		{
			Item selectedItem = owner.inventory[i];
			if (selectedItem.ammo == ammoID && selectedItem.stack > 0)
			{
				projToFire = selectedItem.shoot;
				ammoDamage = selectedItem.damage;
				ammoKB = selectedItem.knockBack;
				ammoVel = selectedItem.shootSpeed;
				return;
			}
		}
	}

	public override bool DoAutoFrameUpdate(ref int framesPerSecond, ref int startFrame, ref int endFrame) => false;

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D projTex = TextureAssets.Projectile[Type].Value;
		FindAmmo(Main.player[Projectile.owner], AmmoID.Arrow, out int? projToFire, out int? _, out float? _, out float? _);
		int arrowProj = projToFire ?? ProjectileID.WoodenArrowFriendly;
		float shootProgress = _targetNPC != null ? (1 - AiTimer / attackCooldown) : 0;

		//Draw string
		float stringLength = 16;
		float maxDrawback = 12;
		Vector2 stringOrigin = new(4, 19);
		Color stringColor = Color.LightGray;

		float stringHalfLength = stringLength / 2;
		const float stringScale = 2;
		stringColor = Projectile.GetAlpha(stringColor.MultiplyRGB(lightColor));

		float timeLeftProgress = 1 - (float)_bounceTimer / bounceTime;
		float easedCharge = EaseFunction.EaseCircularOut.Ease(shootProgress);
		float curDrawback = easedCharge;
		curDrawback *= maxDrawback;

		var pointTop = new Vector2(stringOrigin.X, stringOrigin.Y - stringHalfLength);
		var pointMiddle = new Vector2(stringOrigin.X - curDrawback, stringOrigin.Y);
		var pointBottom = new Vector2(stringOrigin.X, stringOrigin.Y + stringHalfLength);
		int splineIterations = 30;
		Vector2[] spline = Spline.CreateSpline([pointTop, pointMiddle, pointBottom], splineIterations);
		for (int i = 0; i < splineIterations; i++)
		{
			var pixelPos = spline[i];

			pixelPos = pixelPos.RotatedBy(Projectile.rotation);
			pixelPos -= (projTex.Size() / 2).RotatedBy(Projectile.rotation);
			pixelPos += Projectile.Center - Main.screenPosition;

			Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, pixelPos, new Rectangle(0, 0, 1, 1), stringColor, Projectile.rotation, Vector2.Zero, stringScale, SpriteEffects.None, 0);
		}

		//Draw arrow
		if (_targetNPC != null)
		{
			Main.instance.LoadProjectile(arrowProj);
			Texture2D arrowTex = TextureAssets.Projectile[arrowProj].Value;
			Texture2D arrowSolid = TextureColorCache.ColorSolid(arrowTex, Color.Lavender);

			Vector2 arrowPos = pointMiddle.RotatedBy(Projectile.rotation);
			arrowPos -= (projTex.Size() / 2).RotatedBy(Projectile.rotation);
			arrowPos += Projectile.Center - Main.screenPosition;
			var arrowOrigin = new Vector2(arrowTex.Width / 2, arrowTex.Height);

			Color glowColor = TextureColorCache.GetBrightestColor(arrowTex);
			glowColor = Color.Lerp(glowColor, Color.Lavender, 0.33f).Additive();

			for(int i = 0; i < 12; i++)
			{
				Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / 12f) * 2;
				Main.EntitySpriteDraw(arrowSolid, arrowPos + offset, null, glowColor * EaseFunction.EaseQuadIn.Ease(shootProgress) * 0.15f * (timeLeftProgress), Projectile.rotation + MathHelper.PiOver2, arrowOrigin, Projectile.scale, SpriteEffects.None);
			}

			Main.EntitySpriteDraw(arrowTex, arrowPos, null, Projectile.GetAlpha(lightColor).Additive(200) * (timeLeftProgress), Projectile.rotation + MathHelper.PiOver2, arrowOrigin, Projectile.scale, SpriteEffects.None, 0);
		}

		//Draw proj
		Projectile.QuickDraw();

		return false;
	}
}