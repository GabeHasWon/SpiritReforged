using Terraria.Audio;
using Terraria.Graphics.Shaders;
using System.Linq;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Ocean.Items.Reefhunter.JellyfishStaff;

[Common.Misc.AutoloadMinionBuff()]
public class JellyfishMinion : Common.ProjectileCommon.BaseMinion
{
	public JellyfishMinion() : base(900, 1500, new Vector2(28, 28)) { }

	public bool IsPink { get => (int)Projectile.ai[1] != 0; set => Projectile.ai[1] = value ? 1 : 0; }

	public ref float AiTimer => ref Projectile.ai[0];

	public override void AbstractSetStaticDefaults() => Main.projFrames[Type] = 3;

	public override void AbstractSetDefaults()
	{
		Projectile.tileCollide = false;
		AIType = ProjectileID.Raven;
	}

	public override void OnSpawn(IEntitySource source) => IsPink = Main.rand.NextBool(2);

	public override void PostAI()
	{
		foreach (Projectile p in Main.projectile.Where(x => x.active && x != null && x.type == Projectile.type && x.owner == Projectile.owner && x != Projectile))
		{
			if (p.Hitbox.Intersects(Projectile.Hitbox))
				Projectile.velocity += Projectile.DirectionFrom(p.Center) / 5;
		}
	}

	public override void IdleMovement(Player player)
	{
		if (++Projectile.frameCounter >= 10f)
		{
			Projectile.frameCounter = 0;
			Projectile.frame = ++Projectile.frame % Main.projFrames[Type];
		}

		Projectile.friendly = true;
		float num535 = 8f;

		var vector38 = new Vector2(Projectile.position.X + (float)Projectile.width * 0.5f, Projectile.position.Y + (float)Projectile.height * 0.5f);
		float num536 = Main.player[Projectile.owner].Center.X - vector38.X;
		float num537 = Main.player[Projectile.owner].Center.Y - vector38.Y - 60f;
		float num538 = (float)Math.Sqrt((double)(num536 * num536 + num537 * num537));

		if (num538 > 2000f)
			Projectile.Center = player.Center;

		if (num538 > 70f)
		{
			num538 = num535 / num538;
			num536 *= num538;
			num537 *= num538;
			Projectile.velocity.X = (Projectile.velocity.X * 20f + num536) * (1f / 21f);
			Projectile.velocity.Y = (Projectile.velocity.Y * 20f + num537) * (1f / 21f);
		}
		else
		{
			if (Projectile.velocity.X == 0 && Projectile.velocity.Y == 0)
			{
				Projectile.velocity.X = -.05f;
				Projectile.velocity.Y = -.025f;
			}

			Projectile.velocity *= 1.005f;
		}

		Projectile.friendly = false;
		Projectile.rotation = Projectile.velocity.X * .05f;
	}

	public override void TargettingBehavior(Player player, NPC target)
	{
		if (++Projectile.frameCounter >= 6f)
		{
			Projectile.frameCounter = 0;
			Projectile.frame = ++Projectile.frame % Main.projFrames[Type] + Main.projFrames[Type];
		}

		//Move toward the target, but keep distance
		int closeRange = 100;

		Vector2 targetVel = (Projectile.Distance(target.Center) > closeRange) ? (Projectile.DirectionTo(target.Center) * 8f) : Vector2.Zero;
		Projectile.velocity = Vector2.Lerp(Projectile.velocity, targetVel, 0.1f);

		//Move even closer when inside tiles
		if (!Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height, target.position, target.width, target.height))
			Projectile.velocity = Projectile.DirectionTo(target.Center);

		if (++AiTimer >= Main.rand.Next(50, 90))
		{
			AiTimer = 0;

			float shootVelocity = 6.5f;
			Vector2 direction = Projectile.DirectionTo(target.Center) * shootVelocity;

			for (int i = 0; i < 10; i++)
			{
				var dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Electric, 0f, -2f, 0, default, .5f);
				dust.position += Main.rand.NextVector2Unit() * Main.rand.NextFloat(2.5f);
				dust.velocity = dust.position.DirectionFrom(Projectile.Center);
				dust.noGravity = true;
				dust.shader = GameShaders.Armor.GetSecondaryShader(IsPink ? 93 : 96, Main.LocalPlayer);
			}

			if (Main.netMode != NetmodeID.MultiplayerClient)
				Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center.X, Projectile.Center.Y, direction.X, direction.Y, ModContent.ProjectileType<JellyfishBolt>(), Projectile.damage, 0, Main.myPlayer, IsPink ? 1 : 0);

			SoundEngine.PlaySound(SoundID.Item12, Projectile.Center);
		}

		Projectile.rotation = Projectile.velocity.X * .05f;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D texture = TextureAssets.Projectile[Type].Value;
		var frame = texture.Frame(2, Main.projFrames[Type], (Projectile.frame >= Main.projFrames[Type]) ? 1 : 0, Projectile.frame % Main.projFrames[Type]);
		Color color = IsPink ? new Color(248, 148, 255) : new Color(133, 177, 255);

		for (int i = 0; i < 2; i++)
		{
			if (i == 1)
				color.A = 0;

			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Projectile.GetAlpha(color), Projectile.rotation, frame.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
		}

		Lighting.AddLight(Projectile.Center, color.ToVector3() * .25f);
		return false;
	}

	public override bool MinionContactDamage() => true;

	public override bool? CanCutTiles() => false;
}