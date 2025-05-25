using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using Terraria.Audio;

namespace SpiritReforged.Content.Forest.RoguesCrest;

public class RogueKnifeMinion() : BaseMinion(500, 900, new Vector2(12, 12))
{
	public static readonly SoundStyle BigSwing = new("SpiritReforged/Assets/SFX/Item/BigSwing");

	public static readonly SoundStyle Slash = new("SpiritReforged/Assets/SFX/Projectile/SwordSlash1")
	{
		Pitch = 1.25f
	};

	private bool Trailing => Projectile.velocity.Length() >= ProjectileID.Sets.TrailCacheLength[Type] && AiState == Attacking;

	private const int Returning = 0;
	private const int Attacking = 1;
	private const int LockedToPlayer = 2;

	private readonly int attackCooldown = 30;

	private ref float AiTimer => ref Projectile.ai[0];
	private ref float AiState => ref Projectile.ai[1];

	public override void AbstractSetStaticDefaults()
	{
		Main.projFrames[Type] = 6;
		ProjectileID.Sets.TrailCacheLength[Type] = 5;
		ProjectileID.Sets.TrailingMode[Type] = 0;
	}

	public override void AbstractSetDefaults()
	{
		Projectile.minionSlots = 0f;
		Projectile.localNPCHitCooldown = attackCooldown;
		Projectile.extraUpdates = 1;
	}

	public override bool PreAI()
	{
		Player mp = Main.player[Projectile.owner];

		if (mp.HasEquip<RogueCrest>())
			Projectile.timeLeft = 2;

		return true;
	}

	public override void AI()
	{
		base.AI();
		AiTimer = Math.Max(0, AiTimer - 1);
	}

	public override void IdleMovement(Player player)
	{
		var desiredPos = new Vector2((int)player.MountedCenter.X, (int)player.MountedCenter.Y - 60 + (float)Math.Sin(Main.GameUpdateCount / 30f) * 5 + player.gfxOffY);

		AiTimer = 10;
		Projectile.rotation = Projectile.rotation.AngleLerp(0, 0.07f);

		if (AiState != LockedToPlayer && Projectile.Distance(desiredPos) > 25)
		{
			Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(desiredPos) * 6, 0.1f);
			AiState = Returning;
		}
		else
		{
			AiState = LockedToPlayer;
			Projectile.extraUpdates = 0;
			Projectile.velocity = Vector2.Zero;
			Projectile.Center = desiredPos;
		}
	}

	public override void TargettingBehavior(Player player, NPC target)
	{
		AiState = Attacking;

		Projectile.extraUpdates = 1;
		Projectile.rotation = Utils.AngleLerp(Projectile.rotation, Projectile.velocity.ToRotation() + 1.57f, 0.2f);

		if (Projectile.Distance(target.Center) > 120) //Move closer to the target
			Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(target.Center) * 8, 0.08f);
		else
		{
			CanRetarget = AiTimer <= 1;
			Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionFrom(target.Center) * 5, 0.04f);

			if (AiTimer <= 0 && Projectile.Distance(target.Center) > 30) //Lunge, then go on cooldown
			{
				SoundEngine.PlaySound(Slash, Projectile.Center);

				AiTimer = attackCooldown;
				Projectile.velocity = (Projectile.DirectionTo(target.Center) * 24).RotatedByRandom(0.1f);
				Projectile.netUpdate = true;

				for (int i = 0; i < 10; i++)
				{
					var position = Projectile.Center + Projectile.velocity * Main.rand.NextFloat(0, 5f) + Main.rand.NextVector2Unit() * Main.rand.NextFloat(4f);

					var dust = Dust.NewDustPerfect(position, DustID.SilverFlame, Projectile.velocity * Main.rand.NextFloat(.1f, .5f), 100, Color.White, Main.rand.NextFloat(.5f, 1f));
					dust.noGravity = true;
					dust.noLightEmittence = true;
				}

				for (int i = 0; i < 3; i++)
				{
					var position = Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f);
					var scale = new Vector2(Main.rand.NextFloat(.2f, .5f), Main.rand.NextFloat(3, 6));
					var color = Lighting.GetColor(Projectile.Center.ToTileCoordinates()).MultiplyRGB(Color.Lerp(Color.White, Color.SaddleBrown, Main.rand.NextFloat(.5f))) * 2;

					ParticleHandler.SpawnParticle(new Particles.ImpactLine(position, Projectile.velocity * .1f, color, scale, 8, Projectile));
				}
			}
		}
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (Main.rand.NextBool(10))
		{
			target.AddBuff(ModContent.BuffType<OpenWounds>(), 60 * 10);

			ParticleHandler.SpawnParticle(new Particles.ImpactLine(Projectile.Center, Projectile.velocity * .1f, Color.Red.Additive(), new Vector2(.75f, 8), 8, Projectile) { NoLight = true });
			ParticleHandler.SpawnParticle(new Particles.ImpactLine(Projectile.Center, Projectile.velocity * .1f, Color.White.Additive(), new Vector2(.3f, 5), 8, Projectile) { NoLight = true });

			ParticleHandler.SpawnParticle(new Particles.LightBurst(target.Center, 0, Color.Red, .5f, 10) { noLight = true });
			ParticleHandler.SpawnParticle(new Particles.LightBurst(target.Center, 0, Color.White, .3f, 10) { noLight = true });

			SoundEngine.PlaySound(BigSwing, Projectile.Center);
			SoundEngine.PlaySound(SoundID.NPCDeath12 with { Volume = .1f, Pitch = .25f }, Projectile.Center);
		}

		MoRHelper.Decapitation(target, ref damageDone, ref hit.Crit);
	}

	public override bool DoAutoFrameUpdate(ref int framesPerSecond, ref int startFrame, ref int endFrame)
	{
		framesPerSecond = 28;
		return true;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		var texture = TextureAssets.Projectile[Projectile.type].Value;
		var source = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame, 0, -2);
		var origin = new Vector2(source.Width / 2, Projectile.height / 2);

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, source, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);

		if (Trailing)
			for (int k = 0; k < Projectile.oldPos.Length; k++)
			{
				var drawPos = Projectile.oldPos[k] - Main.screenPosition + Projectile.Size / 2 + new Vector2(0f, Projectile.gfxOffY);
				var color = Projectile.GetAlpha(lightColor) * ((Projectile.oldPos.Length - k) / (float)Projectile.oldPos.Length);

				Main.EntitySpriteDraw(texture, drawPos, source, color * .75f, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
			}

		return false;
	}
}