﻿using Terraria.Audio;
using Terraria.Graphics.Shaders;

namespace SpiritReforged.Content.Ocean.Items.Reefhunter.JellyfishStaff;

public class JellyfishBolt : ModProjectile
{
	public bool IsPink
	{
		get => (int)Projectile.ai[0] != 0;
		set => Projectile.ai[0] = value ? 1 : 0;
	}

	private int ShaderID => IsPink ? 93 : 96;

	public override string Texture => "Terraria/Images/Projectile_1"; //Use a basic texture because this projectile is hidden

	// public override void SetStaticDefaults() => DisplayName.SetDefault("Electric Bolt");

	public override void SetStaticDefaults() => ProjectileID.Sets.MinionShot[Type] = true;

	public override void SetDefaults()
	{
		Projectile.friendly = true;
		Projectile.hostile = false;
		Projectile.penetrate = 1;
		Projectile.timeLeft = 240;
		Projectile.height = 4;
		Projectile.width = 4;
		Projectile.hide = true;
		Projectile.extraUpdates = 3;
		AIType = ProjectileID.Bullet;
	}

	public override void AI()
	{
		for (int i = 0; i < 5; i++)
		{
			Vector2 position = Projectile.Center;
			Dust dust = Main.dust[Dust.NewDust(position, 0, 0, DustID.Electric, 0f, 0f, 0, new Color(255, 255, 255), .46f)];
			dust.noLight = true;
			dust.noGravity = true;
			dust.velocity = Vector2.Zero;
			dust.shader = GameShaders.Armor.GetSecondaryShader(ShaderID, Main.LocalPlayer);
		}

		if (Main.rand.NextBool(3))
		{
			Vector2 position = Projectile.Center;
			Dust dust = Main.dust[Dust.NewDust(position, (int)Projectile.velocity.X, (int)Projectile.velocity.Y, DustID.Electric, 0f, 0f, 0, new Color(255, 255, 255), 0.464947368f)];
			dust.noLight = true;
			dust.noGravity = true;
			dust.velocity *= .6f;
			dust.shader = GameShaders.Armor.GetSecondaryShader(ShaderID, Main.LocalPlayer);
		}
	}

	public override void OnKill(int timeLeft) => SoundEngine.PlaySound(SoundID.NPCHit3, Projectile.Center);

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		for (int i = 0; i < 10; i++)
		{
			int num = Dust.NewDust(target.position, target.width, target.height, DustID.Electric, 0f, -2f, 0, default, 2f);
			Main.dust[num].noGravity = true;
			Main.dust[num].shader = GameShaders.Armor.GetSecondaryShader(ShaderID, Main.LocalPlayer);
			Main.dust[num].position.X += Main.rand.Next(-50, 51) * .05f - 1.5f;
			Main.dust[num].position.Y += Main.rand.Next(-50, 51) * .05f - 1.5f;
			Main.dust[num].scale *= .25f;
			if (Main.dust[num].position != target.Center)
				Main.dust[num].velocity = target.DirectionTo(Main.dust[num].position) * 3f;
		}
	}
}
