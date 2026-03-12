using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Visuals.Glowmasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpiritReforged.Content.Desert.Scarabeus.Items.Projectiles;

[AutoloadGlowmask("255,255,255", false)]
public class SunriseScepterOrb : ModProjectile
{
	internal ref float Timer => ref Projectile.ai[0];
	internal ref float MaxTimer => ref Projectile.ai[1];
	internal ref float AttackCooldown => ref Projectile.ai[2];
	internal float Progress => Timer / MaxTimer;
	internal Player Owner => Main.player[Projectile.owner];

	public override void SetDefaults()
	{
		Projectile.Size = new Vector2(40);

		Projectile.tileCollide = false;

		Projectile.timeLeft = -1;
	}

	public override void AI()
	{
		if (Projectile.timeLeft == -1)
			Initialize();

		if (Timer < MaxTimer)
		{
			SpawningBehavior();
		}			
		else
		{
			ShootingBehavior();
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D texture = TextureAssets.Projectile[Type].Value;
		Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;
		Texture2D godray = AssetLoader.LoadedTextures["GodrayCircle"].Value;
		Texture2D star = AssetLoader.LoadedTextures["Star"].Value;

		float scale = Projectile.scale * Progress;

		float fade = 1f;

		if (Projectile.velocity.Length() < 0.2f && Progress >= 1f)
			fade = (Projectile.velocity.Length() - 0.1f) / 0.1f;

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor * fade, 0f, texture.Size() / 2f, scale, 0);
		
		Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, new Color(255, 205, 0, 0) * fade, 0f, bloom.Size() / 2f, scale * 0.5f, 0);
		
		Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, new Color(255, 55, 0, 0) * fade, 0f, bloom.Size() / 2f, scale * 0.35f, 0);

		Main.EntitySpriteDraw(godray, Projectile.Center - Main.screenPosition, null, new Color(255, 205, 0, 0) * fade, 0f, godray.Size() / 2f, scale * 0.2f, 0);

		return false;
	}

	internal void SpawningBehavior()
	{
		Timer++;
		Projectile.timeLeft = 2;

		Vector2 targetPosition = Owner.Center + new Vector2(-30f * Owner.direction, -MathHelper.Lerp(0f, 90f, Progress));

		Projectile.Center = Vector2.Lerp(Projectile.Center, targetPosition, 0.25f);

		if (Timer == MaxTimer)
			Projectile.velocity = new Vector2(Owner.velocity.X + Projectile.Center.DirectionTo(Owner.Center).X * 5, -2f);
	}

	internal void ShootingBehavior()
	{
		Projectile.timeLeft = 2;

		Projectile.velocity *= 0.98f;
		if (Projectile.velocity.Length() < 0.1f)
			Projectile.Kill();

		if (AttackCooldown <= 0)
		{
			NPC target = Main.npc.Where(n => n.CanBeStruck() && n.Distance(Projectile.Center) < 150f).OrderBy(n => n.Distance(Projectile.Center)).FirstOrDefault();

			if (target != default)
			{
				if (Main.myPlayer == Projectile.owner)
				{
					Projectile.NewProjectile(Projectile.GetSource_FromAI("SunriseScepterOrb_Shot"), target.Center, Vector2.Zero,
											ModContent.ProjectileType<SunriseScepterShot>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
				}

				AttackCooldown = 25;
			}			
		}
		else
			AttackCooldown--;
	}

	internal void Initialize()
	{
		Projectile.Center = Owner.Center + new Vector2(-30f * Owner.direction, 0f);

		Main.NewText("Spawned Orb");
	}
}
