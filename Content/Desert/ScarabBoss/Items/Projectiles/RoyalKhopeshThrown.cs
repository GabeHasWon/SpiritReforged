using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using System.IO;
using Terraria.Audio;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items.Projectiles;

public class RoyalKhopeshThrown : ModProjectile
{
	internal class KhopeshTugPacketData : PacketData
	{
		private readonly short _who;
		private readonly Vector2 _target;

		public KhopeshTugPacketData() { }

		public KhopeshTugPacketData(short who, Vector2 target)
		{
			_who = who;
			_target = target;
		}

		public override void OnReceive(BinaryReader reader, int whoAmI)
		{
			short who = reader.ReadInt16();
			Vector2 target = reader.ReadVector2();
			Main.npc[who].GetGlobalNPC<RoyalKhopeshGlobalNPC>().SetTug(target);
			Main.npc[who].netUpdate = true;
		}

		public override void OnSend(ModPacket modPacket)
		{
			modPacket.Write(_who);
			modPacket.WriteVector2(_target);
		}
	}

	// Maximum distance in which enemies will recalled with the sword
	public const int MAX_RECALL_DISTANCE = 400;
	public const int MAX_TIMELEFT = 240;
	public const int FADEOUT_TIME = 60;

	private readonly ParticleRenderer _twirlParticleRenderer = new();
	private VertexTrail[] _trails;

	bool hitTile;
	Vector2 offset = Vector2.Zero;
	Vector2 oldVelo = Vector2.Zero;

	public int EnemyID
	{
		get => (int)Projectile.ai[0];
		set => Projectile.ai[0] = value;
	}

	public bool Stuck
	{
		get => (int)Projectile.ai[1] == 1;
		set => Projectile.ai[1] = value ? 1 : 0;
	}

	public bool Dying
	{
		get => (int)Projectile.ai[2] == 1;
		set => Projectile.ai[2] = value ? 1 : 0;
	}

	public override string Texture => AssetLoader.EmptyTexture;

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailingMode[Type] = 0;
		ProjectileID.Sets.TrailCacheLength[Type] = 7;
	}

	public override void SetDefaults()
	{
		Projectile.width = Projectile.height = 24;
		Projectile.friendly = true;

		Projectile.penetrate = 3;
		Projectile.stopsDealingDamageAfterPenetrateHits = true;

		Projectile.timeLeft = 240;
		Projectile.hide = true;

		Projectile.extraUpdates = 1;
	}
	public override bool ShouldUpdatePosition() => !hitTile;

	public override bool PreAI()
	{
		if (!Main.dedServ)
		{
			if (_trails == null)
				CreateTrail();

			foreach (VertexTrail trail in _trails)
				trail.Update();
		}

		if (hitTile)
		{
			Projectile.velocity = oldVelo;

			if (Projectile.timeLeft > FADEOUT_TIME)
				Projectile.rotation = Projectile.velocity.ToRotation() + (Main.rand.NextBool() ? -1 : 1) * (Main.rand.NextFloat(0.1f, 0.3f) * EaseBuilder.EaseCircularIn.Ease((Projectile.timeLeft - FADEOUT_TIME) / (float)FADEOUT_TIME)) + MathHelper.PiOver2;
			else
				Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

			return false;
		}

		if (Stuck)
		{
			Player owner = Main.player[Projectile.owner];
			NPC target = Main.npc[EnemyID];
			Projectile.position = target.position + offset;

			if (!target.active)
				Projectile.Kill();

			if (Projectile.timeLeft > MAX_TIMELEFT - 10)
			{
				Projectile.rotation = MathHelper.Lerp(Projectile.rotation, (Projectile.Center - Projectile.velocity).DirectionTo(target.Center).ToRotation() + MathHelper.PiOver2, 0.35f);
			}
			else
			{
				Projectile.rotation = (Projectile.Center - Projectile.velocity * 5f).DirectionTo(target.Center).ToRotation() + MathHelper.PiOver2;
			}

			if (Main.myPlayer == Projectile.owner && (Main.mouseLeft || Main.mouseRight) && Projectile.timeLeft < MAX_TIMELEFT - 40 && !Dying)
			{
				Projectile.friendly = true;

				if (target.knockBackResist > 0)
				{
					owner = Main.player[Projectile.owner];

					float dist = target.Distance(owner.Center);

					if (dist < MAX_RECALL_DISTANCE)
					{
						float lerp = dist / MAX_RECALL_DISTANCE;

						target.velocity += target.DirectionTo(Main.player[Projectile.owner].Center + new Vector2(0f, -100f)) * MathHelper.Lerp(5f, 24f, lerp);

						if (Main.netMode == NetmodeID.MultiplayerClient)
						{
							new NPCVelocityPacketData((short)target.whoAmI, target.velocity).Send();
							new KhopeshTugPacketData((short)target.whoAmI, Main.player[Projectile.owner].Center + new Vector2(0f, -100f)).Send();
						}
						else
							target.GetGlobalNPC<RoyalKhopeshGlobalNPC>().SetTug(Main.player[Projectile.owner].Center + new Vector2(0f, -100f));
					}
				}

				SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing, Projectile.Center);

				owner.GetModPlayer<RoyalKhopeshPlayer>().EmpoweredStrikeTimer = 90;
				owner.SetItemAnimation(15);
				owner.SetItemTime(15);
				
				owner.GetModPlayer<RoyalKhopeshPlayer>().HandPosition = owner.HandPosition ?? owner.Center;

				Projectile.friendly = false;
				Stuck = false;
				Projectile.velocity = Projectile.DirectionTo(owner.Center) * 10f;
				Projectile.timeLeft = FADEOUT_TIME;
				Dying = true;
			}

			return false;
		}

		return base.PreAI();
	}

	public override void AI()
	{
		if (Main.rand.NextBool(5) && !Main.dedServ)
		{
			float progress = Projectile.timeLeft / (float)(MAX_TIMELEFT / 2);

			Dust.NewDustPerfect(Projectile.Center,
				DustID.Sand, Main.rand.NextVector2Circular(5f, 5f), 150, default, 1f * progress).noGravity = true;

			Color smokeColor = new Color(223, 219, 147) * 0.35f * progress;
			float scale = Main.rand.NextFloat(0.05f, 0.1f) * progress;
			var velSmoke = Projectile.velocity.RotatedByRandom(0.5f);
			ParticleHandler.SpawnParticle(new SmokeCloud(Projectile.Center + Main.rand.NextVector2Circular(5f, 5f), velSmoke, smokeColor, scale, EaseFunction.EaseQuadOut, Main.rand.Next(30, 40)));
		}

		if (Dying)
		{
			if (Main.dedServ)
				return;

			Projectile.velocity *= 0.96f;
			Projectile.rotation += Projectile.velocity.Length() * 0.025f;

			float progress = Projectile.timeLeft / (float)FADEOUT_TIME;
			
			float lerp = 1f - progress;

			Vector2 adjustedPos = Projectile.Center + Projectile.velocity +
				new Vector2(0f, (float)Math.Sin(Projectile.timeLeft / 2) * MathHelper.Lerp(20f, 70f, lerp)).RotatedBy(Projectile.velocity.ToRotation());

			Dust.NewDustPerfect(adjustedPos,
				DustID.Sand, Projectile.velocity.RotatedByRandom(0.1f) * 0.5f, 150, default, 1.5f).noGravity = true;

			Color smokeColor = new Color(223, 219, 147) * 0.35f;
			float scale = Main.rand.NextFloat(0.1f, 0.15f);
			var velSmoke = Projectile.velocity.RotatedByRandom(0.1f) * 0.5f;
			ParticleHandler.SpawnParticle(new SmokeCloud(adjustedPos, velSmoke, smokeColor, scale, EaseFunction.EaseQuadOut, Main.rand.Next(30, 40)));
		}
		else
		{
			Projectile.velocity *= 0.97f;
			Projectile.rotation += Projectile.velocity.Length() * 0.02f;

			if (Projectile.timeLeft < 180)
				if (Projectile.velocity.Y < 16f)
				{
					Projectile.velocity.Y += 0.1f;
					Projectile.velocity.Y *= 1.05f;
				}			
				else
					Projectile.velocity.Y = 16f;
		}
	}
	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (!Stuck && target.life > 0)
		{
			Projectile.friendly = false;
			Projectile.timeLeft = MAX_TIMELEFT;

			Stuck = true;
			Projectile.tileCollide = false;
			EnemyID = target.whoAmI;
			offset = Projectile.position - target.position;
			offset -= Projectile.velocity * 0.5f;
			Projectile.netUpdate = true;

			if (Main.dedServ)
				return;

			for (int i = 0; i < 5; i++)
			{
				var velocity = -Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.2f, 0.5f);

				ParticleHandler.SpawnParticle(new ImpactLine(Projectile.Center, velocity, new Color(255, 255, 0, 0), new Vector2(0.2f, 0.4f), 15));
				ParticleHandler.SpawnParticle(new ImpactLine(Projectile.Center, velocity, new Color(255, 255, 255, 0).Additive(), new Vector2(0.2f, 0.4f), 10));
			}
		}
	}

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		if (!hitTile)
		{
			// This helps it stick cleany when hitting tiles at more oblique angles to prevent it from sticking out of the ground weirdly
			// TLDR: rotates it towards tiles slightly
			if (Math.Abs(oldVelocity.X) > 5f)
				oldVelocity = oldVelocity.RotatedBy(0.5f * Projectile.direction) * 1.2f;
				
			hitTile = true;
			Projectile.timeLeft = MAX_TIMELEFT / 2;
			
			oldVelo = oldVelocity;
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
			Stuck = false;
			Projectile.friendly = false;

			if (Main.dedServ)
				return false;

			SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundMiss with { Volume = 0.5f, PitchVariance = 0.2f}, Projectile.Center);

			for (int i = 0; i < 5; i++)
			{
				Dust.NewDustPerfect(Projectile.Center,
				DustID.Sand, Main.rand.NextVector2Circular(5f, 5f), 150, default, 1f);

				Color smokeColor = new Color(223, 219, 147) * 0.35f;
				float scale = Main.rand.NextFloat(0.05f, 0.1f);
				var velSmoke = -Projectile.velocity.RotatedByRandom(0.1f) * Main.rand.NextFloat();
				Vector2 spawnPos = Projectile.Center + Main.rand.NextVector2Circular(5f, 5f);
				ParticleHandler.SpawnParticle(new SmokeCloud(spawnPos, velSmoke, smokeColor, scale, EaseFunction.EaseQuadOut, Main.rand.Next(30, 40)));

				Dust.NewDustPerfect(Projectile.Center, DustID.Sand, velSmoke * Main.rand.NextFloat(2), 150, default, 1f).noGravity = true;
			}
		}

		return false;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Main.instance.LoadProjectile(985);

		var tex = TextureAssets.Projectile[ModContent.ProjectileType<RoyalKhopeshHeld>()].Value;
		var texWhite = TextureColorCache.ColorSolid(tex, Color.White);

		float fadeOut = 1f;

		if (Projectile.timeLeft < FADEOUT_TIME)
			fadeOut = Projectile.timeLeft / (float)FADEOUT_TIME;

		_twirlParticleRenderer.Draw(Main.spriteBatch);

		if (_trails != null)
		{
			foreach (VertexTrail trail in _trails)
			{
				trail.Opacity = fadeOut;
				if (Stuck)
					trail.Opacity = Projectile.timeLeft > MAX_TIMELEFT - 30 ? (Projectile.timeLeft - MAX_TIMELEFT - 30) / 30f : 0f;
				trail?.Draw(TrailSystem.TrailShaders, AssetLoader.BasicShaderEffect, Main.spriteBatch.GraphicsDevice);
			}
		}

		if (!Stuck)
		{
			for (int i = 0; i < Projectile.oldPos.Length; i++)
			{
				Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f;
				float lerp = 1f - i / (float)Projectile.oldPos.Length;

				Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, lightColor * fadeOut * lerp,
				  Projectile.rotation - MathHelper.PiOver4, tex.Size() / 2f, Projectile.scale, 0, 0f);
			}
		}	

		Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, lightColor * fadeOut,
			  Projectile.rotation - MathHelper.PiOver4, tex.Size() / 2f, Projectile.scale, 0, 0f);

		if (Stuck)
		{
			Main.spriteBatch.Draw(texWhite, Projectile.Center - Main.screenPosition, null, Color.MediumVioletRed.Additive() * (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.05f)) * 0.5f * fadeOut,
			  Projectile.rotation - MathHelper.PiOver4, texWhite.Size() / 2f, Projectile.scale, 0, 0f);

			if (!Projectile.friendly && Projectile.timeLeft > 210)
			{
				float flashTime = (Projectile.timeLeft - 210) / 30f;

				Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, lightColor * flashTime,
				  Projectile.rotation - MathHelper.PiOver4, tex.Size() / 2f, Projectile.scale * MathHelper.Lerp(2f, 1f, flashTime), 0, 0f);
			}			
		}

		return false;
	}

	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) 
		=> behindNPCsAndTiles.Add(index);

	// Someone more well versed than me in multiplayer compat may need to double check this
	public override void SendExtraAI(BinaryWriter writer)
	{
		writer.WriteVector2(offset);
		writer.WriteVector2(oldVelo);
		writer.Write(hitTile);
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{	
		offset = reader.ReadVector2();
		oldVelo = reader.ReadVector2();
		hitTile = reader.ReadBoolean();
	}

	private void CreateTrail()
	{
		ITrailCap tCap = new RoundCap();
		ITrailPosition tPos = new ProjectileOffsetTrailPosition(Projectile, new Vector2(0, -30));
		ITrailShader tShader = new ImageShader(AssetLoader.LoadedTextures["GlowTrail"].Value, Vector2.One);

		_trails =
		[
			new VertexTrail(new GradientTrail(new Color(129, 88, 53), Color.Transparent, EaseFunction.EaseQuarticOut), tCap, tPos, tShader, 40, 150, -2),
			new VertexTrail(new StandardColorTrail(new Color(179, 148, 54) * 0.25f), tCap, tPos, tShader, 20, 150, -2),
		];
	}
}