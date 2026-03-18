using Microsoft.CodeAnalysis;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Content.Particles;
using System.Security.Cryptography;
using Terraria.GameContent.Drawing;
using Terraria.Graphics.CameraModifiers;
using static Terraria.GameContent.PlayerEyeHelper;

namespace SpiritReforged.Content.Desert.ScarabBoss.Boss;

public class BabyAntlionProjectile : ModProjectile
{
	private const int MAX_TIMELEFT = 460;

	public bool HasAScarabValid
	{
		get
		{
			if (Projectile.ai[0] < 0 || Projectile.ai[0] >= Main.maxNPCs)
				return false;
			NPC npc = Main.npc[(int)Projectile.ai[0]];
			if (!npc.active)
				return false;

			return npc.type == ModContent.NPCType<Scarabeus>();
		}
	}

	public ref float HopHeight => ref Projectile.ai[1];

	public NPC Scarab => Main.npc[(int)Projectile.ai[0]];

	public enum AIState
	{
		Hidden,
		Emerging,
		ChasingScarab,
		Burnt,
		FlyOff
	}

	public AIState CurrentState
	{
		get => (AIState)Projectile.ai[2];
		set => Projectile.ai[2] = (int)value;
	}

	public override void SetStaticDefaults() => ProjectileID.Sets.DontAttachHideToAlpha[Type] = true;

	public override void SetDefaults()
	{
		Projectile.Size = new(26, 26);
		Projectile.hostile = true;
		Projectile.tileCollide = false;
		Projectile.penetrate = -1;
		Projectile.timeLeft = MAX_TIMELEFT;
		Projectile.manualDirectionChange = true;
		Projectile.hide = true;

		Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
	}

	public override bool? CanDamage() => CurrentState != AIState.Hidden && Projectile.timeLeft > 30 ? null : false;

	public override void AI()
	{
		if (CurrentState == AIState.Hidden)
		{
			LieInWait();
			return;
		}

		//Spin around and up into the air before starting to fly
		if (CurrentState == AIState.Emerging)
		{
			SpinEmerge();
			return;
		}

		//Falling down when burnt
		if (CurrentState == AIState.Burnt)
		{
			BurnOffAndFall();
			return;
		}

		//Fly away if there's no valid scarabeus anymore
		if (!HasAScarabValid && CurrentState != AIState.FlyOff)
		{
			CurrentState = AIState.FlyOff;
			Projectile.timeLeft = Math.Min(Projectile.timeLeft, 40);
		}

		//Update the frame for it to flap its wings
		if (++Projectile.frameCounter > 4)
		{
			Projectile.frameCounter = 0;
			Projectile.frame = (Projectile.frame + 1) % 8;
		}

		//Flying off
		if (CurrentState == AIState.FlyOff)
		{
			Projectile.velocity.Y = MathHelper.Lerp(Projectile.velocity.Y, -6f, 0.2f);
			Projectile.velocity.X *= 0.91f;
			Projectile.rotation = Projectile.velocity.X * 0.02f;
			Projectile.direction = Projectile.velocity.X < 0 ? 1 : -1;
			return;
		}

		Vector2 towardsScarab = (Scarab.Center - Vector2.UnitY * 30f - Projectile.Center);
		float distanceToScarab = towardsScarab.Length();

		if (distanceToScarab < 40f)
		{
			//No burnt corpses in normal
			if (!Main.expertMode)
			{
				Projectile.Kill();
				return;
			}

			if (!Main.dedServ)
				ParticleHandler.SpawnParticle(new FireSploshion(Projectile.Center, Main.rand.Next(15, 25)));

			CurrentState = AIState.Burnt;
			Projectile.velocity *= 0.1f;
			Projectile.velocity -= Vector2.UnitY.RotatedByRandom(1.5f) * Main.rand.NextFloat(2f, 6f);
			if (Main.rand.NextBool(3))
				Projectile.velocity += Scarab.DirectionTo((Scarab.ModNPC as Scarabeus).Target.Center + new Vector2(0f, -16f)) * 3f;
			Projectile.timeLeft = 200;
			Projectile.frame = Main.rand.Next(3);
			Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
			return;
		}

		//Stop following scarab if its no longer burning and were too far
		if (Scarab.ai[0] != (int)Scarabeus.AIState.Swarm && distanceToScarab > 200)
		{
			CurrentState = AIState.FlyOff;
			Projectile.timeLeft = Math.Min(Projectile.timeLeft, 40);
		}

		towardsScarab.Normalize();
		towardsScarab = towardsScarab.RotatedBy(MathF.Sin(Projectile.timeLeft * 0.1f) * 0.16) * 13;

		float accelerationSpeed = Utils.GetLerpValue(400f, 100f, distanceToScarab, true);
		Projectile.velocity = Vector2.Lerp(Projectile.velocity, towardsScarab, 0.1f + accelerationSpeed * 0.2f);
		Projectile.rotation = Projectile.velocity.X * 0.02f;
	}

	public void LieInWait()
	{
		const int emerge_wait_time = 45;

		//Do dust and smoke on the floor
		if (Main.rand.NextBool(2))
		{
			Color[] palette = Scarabeus.GetTilePalette(Projectile.Center);

			ParticleHandler.SpawnParticle(new SmokeCloud(Projectile.Bottom, -Vector2.UnitY * Main.rand.NextFloat(1, 3), palette[0] * 0.7f, Main.rand.NextFloat(0.03f, 0.15f), EaseFunction.EaseQuadOut, Main.rand.Next(20, 40))
			{
				Pixellate = true,
				DissolveAmount = 1,
				SecondaryColor = palette[1] * 0.7f,
				TertiaryColor = palette[2] * 0.7f,
				PixelDivisor = 3,
				ColorLerpExponent = 0.25f,
				Layer = ParticleLayer.BelowSolid
			});
		}

		if (!Main.rand.NextBool(3))
		{
			Vector2 dustPosition = Projectile.Center + Vector2.UnitY * 4f;
			Point tilePosition = dustPosition.ToTileCoordinates();
			int dustIndex = WorldGen.KillTile_MakeTileDust(tilePosition.X, tilePosition.Y, Framing.GetTileSafely(tilePosition));

			Dust dust = Main.dust[dustIndex];
			dust.position = dustPosition + Vector2.UnitX * Main.rand.NextFloat(-16f, 16f);
			dust.velocity.Y -= Main.rand.NextFloat(1.5f, 3f);
			dust.velocity.X *= 0.5f;
			dust.noLightEmittence = true;
			dust.scale = Main.rand.NextFloat(0.5f, 1.2f);
		}

		//Just die if scarab dies lol
		if (!HasAScarabValid)
		{
			Projectile.Kill();
			return;
		}

		//Emerge outta the ground
		if (MAX_TIMELEFT - Projectile.timeLeft > emerge_wait_time)
		{
			Projectile.direction = (Projectile.Center.X - Scarab.Center.X) < 0 ? 1 : -1;
			CurrentState = AIState.Emerging;
			Projectile.velocity.Y = -HopHeight;
			Projectile.netUpdate = true;
		}
	}

	public void SpinEmerge()
	{
		Projectile.rotation += Projectile.direction * 0.36f;
		Projectile.velocity.Y += 0.2f;

		if (Projectile.velocity.Y >= 0)
		{
			if (HasAScarabValid)
			{
				Projectile.direction = (Projectile.Center.X - Scarab.Center.X) < 0 ? 1 : -1;
				CurrentState = AIState.ChasingScarab;
			}
			else
				CurrentState = AIState.FlyOff;
		}
	}

	public void BurnOffAndFall()
	{
		Projectile.velocity.X *= 0.99f;
		Projectile.velocity.Y += 0.18f;
		if (Projectile.velocity.Y > 0)
			Projectile.velocity.Y *= 1.02f;

		Projectile.rotation += Projectile.velocity.Y * 0.01f;

		//Sharticles
		if (!Main.dedServ & Main.rand.NextBool(3))
		{
			Dust d = Dust.NewDustPerfect(Main.rand.NextVector2FromRectangle(Projectile.Hitbox), DustID.Torch, Vector2.Zero, 0, Scale: Main.rand.NextFloat(0.7f, 1f));
			d.noLight = true;
		}

		if (!Main.dedServ && Main.rand.NextBool(4))
		{
			Gore g = Gore.NewGorePerfect(Projectile.GetSource_FromThis(), Projectile.Center + Main.rand.NextVector2Circular(8f, 8f), Vector2.Zero, 99, Main.rand.NextFloat(1f, 1.2f));
			g.alpha = 185;
			g.position -= Vector2.One * 10f;
			g.rotation = Main.rand.NextFloat(6.24f);
		}
	}

	public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) => modifiers.Knockback *= 0f;

	public override void OnHitPlayer(Player target, Player.HurtInfo info)
	{
		target.AddBuff(BuffID.OnFire, Scarabeus.STAT_ANTLION_ONFIRE_DURATION, false);
	}

	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
	{
		if (CurrentState == AIState.Emerging)
			behindNPCsAndTiles.Add(index);
		else if (CurrentState != AIState.Burnt)
			overPlayers.Add(index);
		else
			behindNPCs.Add(index);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		if (CurrentState == AIState.Hidden)
			return false;

		Texture2D texture = TextureAssets.Projectile[Type].Value;
		Vector2 position = Projectile.Center;
		float rotation = Projectile.rotation;
		float scale = Projectile.scale;
		Rectangle frame = texture.Frame(8, 3, Projectile.frame, CurrentState == AIState.Burnt ? 2 : CurrentState == AIState.Emerging ? 0 : 1);
		SpriteEffects effects = Projectile.direction < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

		lightColor *= Math.Min(1, Projectile.timeLeft / 40f) * Utils.GetLerpValue(MAX_TIMELEFT, MAX_TIMELEFT - 30, Projectile.timeLeft, true);

		Main.EntitySpriteDraw(texture, position - Main.screenPosition, frame, lightColor, rotation, frame.Size() / 2f, scale, effects, 0);
		return false;
	}
}