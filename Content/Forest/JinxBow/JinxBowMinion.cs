using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Forest.JinxBow;

public class JinxBowMinion() : BaseMinion(500, 900, new Vector2(12, 12))
{
	private readonly int attackCooldown = 60;

	private ref float AiTimer => ref Projectile.ai[0];

	public override void AbstractSetStaticDefaults()
	{
		Main.projFrames[Type] = 4;
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
	}

	public override bool? CanDamage() => false;

	public override void IdleMovement(Player player)
	{
		var desiredPos = new Vector2((int)player.MountedCenter.X - player.direction * 50, (int)player.MountedCenter.Y - 40 + (float)Math.Sin(Main.GameUpdateCount / 30f) * 5 + player.gfxOffY);

		AiTimer = 10;
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

		CanRetarget = AiTimer <= 1;
		Projectile.frame = EaseFunction.EaseCircularOut.Ease(AiTimer / attackCooldown) switch
		{
			< 0.25f => 3,
			< 0.5f => 2,
			< 0.75f => 1,
			_ => 0
		};

		if (AiTimer <= 0)
		{
			SoundEngine.PlaySound(SoundID.DD2_PhantomPhoenixShot with { Pitch = 1.25f }, Projectile.Center);

			AiTimer = attackCooldown;

			FindAmmo(player, AmmoID.Arrow, out int? projToFire, out int? ammoDamage, out float? ammoKB, out float? ammoVel);

			int type = projToFire ?? ProjectileID.WoodenArrowFriendly;

			float speed = 10 + ammoVel ?? 0;
			float ticksFromTarget = Projectile.Distance(target.Center) / speed;
			Vector2 arrowVelocity = Projectile.DirectionTo(target.Center + target.velocity * ticksFromTarget / 2) * speed;
			float knockBack = Projectile.knockBack + (ammoKB ?? 0);
			int damage = Projectile.damage + (ammoDamage ?? 0);

			PreNewProjectile.New(Projectile.GetSource_FromThis(), Projectile.Center, arrowVelocity, type, damage, knockBack, Projectile.owner, preSpawnAction: p => 
			{
				p.DamageType = DamageClass.Summon;
				p.GetGlobalProjectile<JinxBowShot>().IsJinxbowShot = true;
			});
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
		var texture = TextureAssets.Projectile[Projectile.type].Value;
		var source = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
		var origin = new Vector2(source.Width / 2, source.Height / 2);
		SpriteEffects flip = Projectile.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, source, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, flip, 0);

		return false;
	}
}