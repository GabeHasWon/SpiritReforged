using SpiritReforged.Common.Easing;
using SpiritReforged.Common.MathHelpers;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Particles;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.Graphics.CameraModifiers;

namespace SpiritReforged.Content.Desert.Scarabeus.Boss;

[AutoloadBossHead]
[AutoloadGlowmask("255, 255, 255", false)]
public class ScarabeusBoss : ModNPC
{
	public float AITimer { get => NPC.ai[0]; set => NPC.ai[0] = value; }
	public float CurrentPattern { get => NPC.ai[1]; set => NPC.ai[1] = value; }

	private Vector3 _curFrame;

	private bool _contactDmgEnabled = false;
	private bool _inGround = true;
	private bool _hasPhaseChanged = false;

	private int _jumpState = 0;

	private enum AIPatterns
	{
		SpawnAnimation,
		Walking,
		Skitter,
		HornSwipe,
		Leap,
		RollDash,
		GroundedSlam,
		Dig,
		BounceGroundPound,
		FlyHover,
		FlyingDash,
		ChainGroundPound,
		DigErupt,
		ScarabSwarm
	}

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[NPC.type] = 14;
		NPCID.Sets.TrailCacheLength[NPC.type] = 4;
		NPCID.Sets.TrailingMode[NPC.type] = 0;

		var drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers()
		{
			Position = new Vector2(8f, 12f),
			PortraitPositionXOverride = 0f
		};
		NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, drawModifiers);
	}

	public override void SetDefaults()
	{
		NPC.width = 110;
		NPC.height = 110;
		NPC.value = 30000;
		NPC.damage = 40;
		NPC.defense = 10;
		NPC.lifeMax = 1750;
		NPC.aiStyle = -1;
		Music = MusicLoader.GetMusicSlot(Mod, "Assets/Music/Scarabeus");
		NPC.boss = true;
		NPC.npcSlots = 15f;
		NPC.HitSound = SoundID.NPCHit31;
		NPC.DeathSound = SoundID.NPCDeath5;
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "Desert");

	public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
	{
		NPC.lifeMax = (int)(NPC.lifeMax * (Main.masterMode ? 0.85f : 1.0f) * 0.7143f * balance);
		NPC.damage = (int)(NPC.damage * 0.626f);
	}

	public override bool CheckActive()
	{
		Player player = Main.player[NPC.target];
		if (!player.active || player.dead)
			return false;

		return true;
	}

	public override void AI()
	{
		NPC.TargetClosest(CurrentPattern != (float)AIPatterns.GroundedSlam);
		Player player = Main.player[NPC.target];
		_contactDmgEnabled = false;
		NPC.behindTiles = true;

		NPCID.Sets.TrailingMode[NPC.type] = 3;

		if (NPC.life < NPC.lifeMax / 2)
		{
			if(!_hasPhaseChanged)
			{
				NextAttack(player, AIPatterns.FlyHover);
				_hasPhaseChanged = true;
			}
		}

		PatternSelect(player);
	}

	private void PatternSelect(Player player)
	{
		switch ((AIPatterns)CurrentPattern)
		{
			case AIPatterns.SpawnAnimation:
				SpawnAnimation(player);
				break;

			case AIPatterns.Walking:
				Walking(player);
				break;

			case AIPatterns.Leap:
				Leap(player);
				break;

			case AIPatterns.RollDash:
				RollDash(player);
				break;

			case AIPatterns.HornSwipe:
				HornSwipe(player);
				break;

			case AIPatterns.Skitter:
				Skitter(player);
				break;

			case AIPatterns.GroundedSlam:
				GroundSlam(player);
				break;

			case AIPatterns.BounceGroundPound:
				BounceGroundPound(player);
				break;

			case AIPatterns.Dig:
				Dig(player);
				break;

			case AIPatterns.FlyHover:
				FlyHover(player);
				break;

			case AIPatterns.FlyingDash:
				FlyDash(player);
				break;

			case AIPatterns.ChainGroundPound:
				ChainGroundPound(player);
				break;

			case AIPatterns.DigErupt:
				DigErupt(player);
				break;

			case AIPatterns.ScarabSwarm:
				ScarabSwarm(player);
				break;
		}
	}

	private void SpawnAnimation(Player player)
	{
		const int swarmTime = 120;
		const int roarTime = 120;

		/*Todo: 
		 * foreground scarab particles fly across the screen from bottom left to top right
		 * screenshake
		 * ground beneath player starts emitting particles
		 * scarab bursts out of ground and roars
		*/

		AITimer++;

		if(AITimer == 1 && !Main.dedServ)
		{
			if(Main.LocalPlayer.Distance(player.Center) < 800)
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(player.Center, Vector2.UnitX, 0.5f, 3, swarmTime * 2));

			for(int i = 0; i < 48; i++)
			{
				Vector2 scarabPos = player.Center;
				bool backgroundScarab = !Main.rand.NextBool(3);
				int spawnDelayRange = (int)(swarmTime * (backgroundScarab ? 0.25f : 0.66f));
				int spawnDelayStatic = backgroundScarab ? 0 : swarmTime / 3;
				scarabPos += new Vector2(-Main.rand.NextFloat(900, 1400), Main.rand.NextFloat(200, 800)) * (backgroundScarab ? 1f : 1.2f);
				ParticleHandler.SpawnQueuedParticle(new ScarabParticle(scarabPos, Main.rand.NextFloat(0.3f, 0.7f), 1, backgroundScarab), Main.rand.Next(spawnDelayRange) + spawnDelayStatic);
			}
		}

		if(AITimer == swarmTime)
		{
			NPC.Center = FindGroundFromPosition(player.Center);
			NPC.noTileCollide = false;
			NPC.noGravity = false;
			NPC.velocity.Y = -12;
			_curFrame.Y = 0;
			_inGround = false;
		}

		if(AITimer >= swarmTime + roarTime)
			NextAttack(player, AIPatterns.Walking);
	}

	private int boredomTimer;
	private void Walking(Player player)
	{
		int maxWalkTime = 360;
		int maxBoredom = 60;

		NPC.spriteDirection = NPC.direction;
		NPC.knockBackResist = 0.7f;
		AITimer++;
		CheckPlatform(player);

		//Check if grounded
		if (NPC.velocity.Y == 0 && NPC.oldVelocity.Y >= 0)
		{
			//Only move if too far from the player, try to move away a little bit if too close

			float horizontalDist = Math.Abs(NPC.position.X - player.position.X);
			if (horizontalDist > 200 || AITimer < 30)
			{
				NPC.velocity.X += NPC.direction * 0.3f;
				boredomTimer = Math.Max(boredomTimer - 1, 0);
			}

			else
			{
				if (Math.Sign(NPC.velocity.X) == NPC.direction && Math.Abs(NPC.velocity.X) > 2)
					NPC.velocity.X -= NPC.direction * 0.1f;

				if (horizontalDist < 140)
				{
					boredomTimer++;

					if (boredomTimer > 2 * maxBoredom / 3)
						NPC.velocity.X -= NPC.direction * 0.1f;
				}
			}
		}

		NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X, -5, 5);

		float fps = NPC.direction * NPC.velocity.X * 2;
		if(Math.Abs(fps) < 1)
		{
			if (fps < 0)
				fps = -1;

			else if (fps > 0)
				fps = 1;
		}

		AnimateFrame(1, (int)fps);
		StepUp(player);

		if(boredomTimer >= maxBoredom)
			NextAttack(player, Main.rand.NextBool() ? AIPatterns.Skitter : AIPatterns.HornSwipe);

		/*
		 * Todo:
		 * Make it change to horn swipe if player sticks too close too long
		 * Leap if too far or can't traverse terrain and a leap would reach the player (Pits, height differences)
		 * Dig if too far or can't traverse terrain and a leap wouldn't reach player (Collision)
		 */

		if (AITimer > maxWalkTime)
			NextAttack(player);
	}

	private void HornSwipe(Player player)
	{
		const int windupTime = 30;
		const int attackTime = 35;

		NPC.spriteDirection = NPC.direction;
		NPC.knockBackResist = 0f;
		NPC.noGravity = false;
		CheckPlatform(player);

		NPC.velocity.X *= 0.5f;

		if (AITimer < windupTime)
			AnimateFrame(2, (int)(3 * 60f / windupTime), false);
		else
			AnimateFrame(2, (int)(7 * 60f / attackTime), false);

		if (_curFrame.Y is >= 3 and < 7)
			_contactDmgEnabled = true;

		if (AITimer++ >= attackTime + windupTime)
			NextAttack(player);
	}

	private void Skitter(Player player)
	{
		const int skitterTime = 40;

		NPC.spriteDirection = NPC.direction;
		NPC.knockBackResist = 0f;
		NPC.noGravity = false;
		CheckPlatform(player);

		NPC.velocity.X = -NPC.direction * MathHelper.Lerp(12, 4, EaseFunction.EaseQuadOut.Ease(AITimer / skitterTime));
		AITimer++;
		AnimateFrame(1, (int)(NPC.direction * NPC.velocity.X) * 2);

		if (AITimer > skitterTime)
			NextAttack(player);
	}

	private void Leap(Player player)
	{
		const int windupTime = 40;
		const int restTime = 45;

		bool HasJumped = _jumpState == 1;
		bool HasLanded = _jumpState == 2;

		NPC.spriteDirection = NPC.direction;
		NPC.knockBackResist = 0f;
		NPC.noGravity = false;
		CheckPlatform(player);

		if (!HasJumped && !HasLanded)
		{
			//Check if grounded
			if (NPC.velocity.Y == 0 && NPC.oldVelocity.Y >= 0)
			{
				AITimer++;
				//Slow down for a bit, then calculate mortar velocity to jump towards player
				//Increase velocity if too far to reach player

				if (AITimer <= windupTime)
				{
					NPC.velocity.X *= 0.8f;
					AnimateFrame(3, (int)(6 * windupTime / 60f), false);

					if (AITimer == windupTime)
					{
						Vector2 desiredPos = player.Center + player.velocity * 6 + (NPC.direction * 112 * Vector2.UnitX);
						NPC.velocity = NPC.GetArcVel(desiredPos, 0.38f, 12, true);
						NPC.noTileCollide = true;
						_jumpState++;
						SyncNPC();
					}
				}
			}
		}

		else if (!HasLanded)
		{
			_curFrame = new Vector3(4, 5, 0);
			_contactDmgEnabled = true;

			if (NPC.velocity.Y < 0)
				NPC.noTileCollide = true;
			else
				NPC.noTileCollide = false;

			if (NPC.velocity.Y == 0 && NPC.oldVelocity.Y > 0)
			{
				_jumpState++;
				//vfx and sfx and shockwaves here
				SyncNPC();
				AITimer = 0;
			}
		}

		else
		{
			NPC.velocity.X = 0;
			AnimateFrame(4, (int)(10 * restTime / 60f), false);

			AITimer++;

			if (AITimer > restTime)
				NextAttack(player);
		}

		/*
		 * Todo:
		 * Leap towards player's current position with some prediction, phase through some tiles but avoid phasing through a wall, create shockwave on impact
		 */
	}

	private void RollDash(Player player)
	{
		const int windupTime = 80;
		const int dashTime = 50;

		NPC.spriteDirection = NPC.direction;
		NPC.noTileCollide = false;
		NPC.noGravity = false;
		NPC.knockBackResist = 0f;
		CheckPlatform(player);
		AITimer++;

		if(AITimer < windupTime)
		{
			_curFrame = new Vector3(5, 0, 0);
			float windupProgress = EaseFunction.EaseCubicOut.Ease(AITimer / windupTime);
			NPC.velocity.X = NPC.direction * (1 - windupProgress) * -8;
			NPC.rotation += windupProgress * 0.3f;
		}

		if(AITimer == windupTime)
		{
			NPC.velocity.X = NPC.direction * 36;
			//sfx and vfx here
		}

		if(AITimer > windupTime)
		{
			_contactDmgEnabled = true;
			NPC.rotation += 0.08f;
			NPC.velocity.X *= 0.96f;
			//sfx here

			if (NPC.collideX)
				NextAttack(player, AIPatterns.BounceGroundPound);
		}

		if(AITimer >= windupTime + dashTime)
		{
			//end attack
			NPC.velocity.X /= 2;
			NextAttack(player, AIPatterns.Walking);
			NPC.rotation = 0;
		}
	}

	private void GroundSlam(Player player)
	{
		const int windupTime = 60;
		const int restTime = 45;

		NPC.spriteDirection = NPC.direction;
		NPC.noTileCollide = false;
		NPC.noGravity = false;
		NPC.knockBackResist = 0f;
		CheckPlatform(player);
		AITimer++;

		if(AITimer < windupTime)
		{
			AnimateFrame(4, 60 * 6 / windupTime);
			NPC.velocity *= 0.7f;
		}

		if(AITimer == windupTime)
		{
			_contactDmgEnabled = true;
			//projectiles and sfx here

			if(Main.netMode != NetmodeID.MultiplayerClient)
			{
				Vector2 center = FindGroundFromPosition(NPC.Center + NPC.direction * Vector2.UnitX * 320);
				Projectile.NewProjectile(NPC.GetSource_FromThis(), center - Vector2.UnitY * 160, Vector2.Zero, ModContent.ProjectileType<SlamShockwave>(), NPC.damage / 2, 16, Main.myPlayer, NPC.direction);
			}
		}

		if(AITimer > windupTime)
			AnimateFrame(4, 8, false);

		if(AITimer > windupTime + restTime)
		{
			NextAttack(player);
			//end attack
		}
	}

	private void BounceGroundPound(Player player)
	{
		const int maxBounces = 3;

		NPC.spriteDirection = NPC.direction;
		NPC.noTileCollide = false;
		NPC.noGravity = true;
		NPC.knockBackResist = 0f;
		CheckPlatform(player);

		if(_jumpState < maxBounces)
		{
			_curFrame = new Vector3(5, 0, 0);
			_contactDmgEnabled = true;

			//Check if grounded
			if (NPC.velocity.Y == 0 && NPC.oldVelocity.Y >= 0)
			{
				_jumpState++;
				NPC.velocity.Y = -16;

				if (Main.netMode != NetmodeID.Server && Main.LocalPlayer.Distance(NPC.Center) < 800)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(Main.screenPosition, Vector2.UnitY, 2, 3, 15));
			}

			else
			{
				float desiredVel = (NPC.Center.X < player.Center.X) ? 16 : -16;
				NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, desiredVel, 0.006f);

				if(NPC.velocity.Y < 12)
					NPC.velocity.Y += 0.08f;

				NPC.rotation += NPC.direction * 0.1f + NPC.velocity.X / 120;
			}

			NPC.velocity.Y += 0.38f;
		}

		else if (_jumpState == maxBounces)
		{
			AITimer++;
			_contactDmgEnabled = true;

			if (AITimer < 40)
			{
				float desiredVel = (NPC.Center.X < player.Center.X) ? 16 : -16;
				NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, desiredVel, 0.006f);

				if (NPC.velocity.Y < -4)
					NPC.velocity.Y += 0.08f;

				NPC.rotation += NPC.direction * 0.1f + NPC.velocity.X / 120;

				if (AITimer > 30)
					NPC.velocity.X *= 0.9f;
			}

			else if (AITimer < 60)
			{
				NPC.velocity.X = 0;
				NPC.velocity.Y = MathHelper.Lerp(NPC.velocity.Y, -1.25f, 0.3f);
			}

			else if (AITimer == 60)
				NPC.velocity.Y = 16;

			if(AITimer > 70 && NPC.velocity.Y == 0 && NPC.oldVelocity.Y >= 0)
			{
				//vfx and sfx and projs here

				_jumpState++; //use the variable to track the final ground pound too
				NPC.velocity.Y = -3;

				for(int i = -3; i <= 3; i++)
				{
					if (i == 0)
						continue;

					float distStep = Main.rand.NextFloat(11, 13) * i * 16;
					Vector2 projPosition = FindGroundFromPosition(NPC.Bottom + Vector2.UnitX * distStep) - Vector2.UnitY * 80;

					Projectile.NewProjectile(NPC.GetSource_FromThis(), projPosition, Vector2.Zero, ModContent.ProjectileType<SandPillar>(), NPC.damage / 4, 3, Main.myPlayer, Math.Abs(i) * 40);
				}

				if (Main.netMode != NetmodeID.Server && Main.LocalPlayer.Distance(NPC.Center) < 800)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(Main.screenPosition, Vector2.UnitY, 2, 3, 15));
			}
			
			if(AITimer is > 60 or < 40)
				NPC.velocity.Y += 0.38f;

			if(AITimer > 40)
				NPC.rotation += NPC.direction * 0.3f;
		}

		else //rest before next attack
		{
			AnimateFrame(4, (int)(8 * 80 / 60f), false);
			if (_curFrame.Y < 7)
				_curFrame.Y = 7;

			NPC.rotation = 0;
			AITimer++;
			NPC.noGravity = false;

			if (AITimer > 150)
				NextAttack(player);
		}
	}

	private void Dig(Player player)
	{
		const int digStartTime = 60;
		const int undergroundTime = 180;
		const int airTime = 40;

		NPC.spriteDirection = NPC.direction;
		NPC.noTileCollide = false;
		NPC.noGravity = false;
		NPC.knockBackResist = 0f;
		AITimer++;

		if(AITimer < digStartTime)
		{
			//dig into ground anim here, placeholder rn
			NPC.velocity = Vector2.Zero;
			NPC.position.Y += 0.5f;
			AnimateFrame(3, 6, false);
		}

		else if(AITimer == digStartTime)
		{
			//temp for hiding boss
			_inGround = true;
			NPC.alpha = 0;
			NPC.Center = FindGroundFromPosition(player.Center);
		}

		else if(AITimer < undergroundTime + digStartTime)
		{
			//set npc's position to tiles under player, moving around left and right, before settling on a position
			//particles spawn from the tile where the npc is located

			NPC.noGravity = true;
			NPC.noTileCollide = true;

			float desiredVel = (NPC.Center.X < player.Center.X) ? 16 : -16;
			NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, desiredVel, 0.01f);
			NPC.position.Y = FindGroundFromPosition(NPC.position).Y;

			if(!Main.dedServ)
			{
				for (int i = 0; i < Main.rand.Next(4); i++)
				{
					Vector2 particlePos = NPC.Center - Vector2.UnitY * 48;
					particlePos += Main.rand.NextFloat(-64, 64) * Vector2.UnitX;

					Vector2 particleVel = -Vector2.UnitY * Main.rand.NextFloat(4, 7);

					ParticleHandler.SpawnParticle(new SmokeCloud(particlePos, particleVel, new Color(223, 219, 147) * 2f, Main.rand.NextFloat(0.08f, 0.12f), EaseFunction.EaseCircularOut, Main.rand.Next(30, 40))
					{
						Pixellate = true,
						DissolveAmount = 1,
						Intensity = 0.9f,
						SecondaryColor = new Color(188, 170, 86) * 1.33f,
						TertiaryColor = new Color(58, 49, 18) * 0.5f,
						PixelDivisor = 3,
						Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
						ColorLerpExponent = 0.5f,
						Layer = ParticleLayer.BelowSolid
					});
				}

				if(Main.rand.NextBool(3))
					Dust.NewDust(NPC.position - Vector2.UnitY * 8, NPC.width, 0, DustID.Sand, 0, -4, 0, default, Main.rand.NextFloat(0.7f, 1.2f));

				if(AITimer % 20 == 0)
					BouncingTileWave(5, Main.rand.NextFloat(4, 10), Main.rand.Next(30, 40), Main.rand.NextFloat(-NPC.width / 4, NPC.width / 4) * Vector2.UnitX + NPC.velocity / 2);
			}
		}

		else if(AITimer == undergroundTime + digStartTime)
		{
			//pop out of ground here
			_inGround = false;
			NPC.rotation = MathHelper.PiOver4;
			NPC.velocity.Y = -16;
		}

		else if (AITimer < undergroundTime + digStartTime + airTime)
		{
			NPC.noGravity = false;
			NPC.noTileCollide = false;
			_contactDmgEnabled = true;

			//curl anim here
		}

		else
		{
			NextAttack(player, AIPatterns.BounceGroundPound);
		}
	}

	private void FlyHover(Player player)
	{
		const int hoverTime = 180;

		NPC.noTileCollide = true;
		NPC.noGravity = true;
		NPC.knockBackResist = 0.7f;
		AITimer++;

		float heightAboveGround = FindGroundFromPosition(NPC.Center).Y - NPC.Center.Y;

		//Vertical movement
		if (heightAboveGround < 128)
			NPC.velocity.Y -= 0.1f;

		else if (Math.Abs(NPC.position.Y - player.position.Y) > 160)
			NPC.velocity.Y -= 0.175f * Math.Sign(NPC.Center.Y - player.Center.Y);

		else
			NPC.velocity.Y *= 0.9f;

		NPC.velocity.Y += (float)Math.Sin(MathHelper.TwoPi * AITimer / hoverTime) / 10;

		//Horizontal movement

		if (NPC.Center.X < player.Center.X)
		{
			if (NPC.velocity.X < 0)
			{
				NPC.velocity.X *= 0.975f;
				NPC.velocity.X += 0.025f;
			}

			else
			{
				NPC.velocity.X += 0.1f;
			}
		}

		else
		{
			if (NPC.velocity.X > 0)
			{
				NPC.velocity.X *= 0.975f;
				NPC.velocity.X -= 0.025f;
			}

			else
			{
				NPC.velocity.X -= 0.1f;
			}
		}

		NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X, -12, 12);
		NPC.velocity.Y = MathHelper.Clamp(NPC.velocity.Y, -8, 8);

		if (AITimer > hoverTime)
			NextAttack(player);
	}

	private void FlyDash(Player player)
	{
		const int prepTime = 90;
		const int dashTime = 70;

		NPC.noTileCollide = true;
		NPC.noGravity = true;
		NPC.knockBackResist = 0f;
		AITimer++;

		if(AITimer < prepTime)
		{
			//vertical

			if (Math.Abs(NPC.position.Y - player.position.Y) > 32)
				NPC.velocity.Y -= 0.25f * Math.Sign(NPC.Center.Y - player.Center.Y);

			else
				NPC.velocity.Y *= 0.9f;

			NPC.velocity.Y += (float)Math.Sin(MathHelper.TwoPi * AITimer / prepTime) / 10;

			//horizontal

			float desiredPos = player.Center.X - 132 * (NPC.Center.X < player.Center.X ? 1 : -1);

			if (Math.Abs(NPC.Center.X - desiredPos) > 48)
			{
				if (NPC.Center.X < desiredPos)
					NPC.velocity.X += 0.2f;

				else
					NPC.velocity.X -= 0.2f;
			}

			else
				NPC.velocity.X *= 0.9f;

			float windupThreshold = 0.66f;
			if (AITimer > prepTime * windupThreshold)
				NPC.velocity.X -= NPC.direction * MathHelper.Lerp(0.25f, 1f, EaseFunction.EaseQuadOut.Ease((AITimer - prepTime * windupThreshold) / (prepTime * (1 - windupThreshold))));

			NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X, -12, 12);
			NPC.velocity.Y = MathHelper.Clamp(NPC.velocity.Y, -8, 8);
		}

		if(AITimer == prepTime)
		{
			NPC.velocity.X = (NPC.Center.X > player.Center.X ? -1 : 1) * 34;
			NPC.velocity.Y /= 3;
			// fx here
		}

		if(AITimer > prepTime)
		{
			NPC.direction = NPC.spriteDirection = Math.Sign(NPC.velocity.X);
			NPC.velocity.X = MathHelper.Lerp(34 * NPC.direction, 0, EaseFunction.EaseCubicOut.Ease((AITimer - prepTime) / dashTime));
		}

		if (AITimer > prepTime + dashTime)
			NextAttack(player);
	}

	private void ChainGroundPound(Player player)
	{
		const int maxBounces = 2;
		const int maxPounds = 3;

		NPC.spriteDirection = NPC.direction;
		NPC.noTileCollide = false;
		NPC.noGravity = true; //for some godforsaken reason this also force caps an npc's downwards velocity to 10
		NPC.knockBackResist = 0f;
		CheckPlatform(player);
		_curFrame.Y = 1;
		_curFrame.X = 0;

		if (_jumpState < maxBounces)
		{
			_contactDmgEnabled = true;

			//Check if grounded
			if (NPC.velocity.Y == 0 && NPC.oldVelocity.Y >= 0)
			{
				_jumpState++;
				NPC.velocity.Y = -16;

				if (Main.netMode != NetmodeID.Server && Main.LocalPlayer.Distance(NPC.Center) < 800)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(Main.screenPosition, Vector2.UnitY, 2, 3, 15));
			}

			else
			{
				float desiredVel = (NPC.Center.X < player.Center.X) ? 16 : -16;
				NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, desiredVel, 0.01f);

				if (NPC.velocity.Y < 12)
					NPC.velocity.Y += 0.08f;

				NPC.rotation += NPC.velocity.X / 120;
			}

			NPC.velocity.Y += 0.38f;
		}

		else if (_jumpState is >= maxBounces and < (maxBounces + maxPounds))
		{
			NPC.knockBackResist = 0f;
			AITimer++;
			_curFrame.Y = 2;
			_contactDmgEnabled = true;

			if (AITimer < 25)
			{
				float desiredVel = (NPC.Center.X < player.Center.X) ? 24 : -24;
				NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, desiredVel, 0.01f);

				if(_jumpState == maxBounces)
				{
					_curFrame.Y = 1;
					NPC.rotation += NPC.velocity.X / 120;
				}

				if (NPC.velocity.Y < -4)
					NPC.velocity.Y += 0.08f;

				if (AITimer > 20)
					NPC.velocity.X *= 0.9f;
			}

			else if (AITimer < 40)
			{
				NPC.velocity.X = 0;
				NPC.velocity.Y = MathHelper.Lerp(NPC.velocity.Y, -6, 0.3f);
				NPC.rotation = -MathHelper.PiOver2 * NPC.direction;
			}

			else if (AITimer == 40)
			{
				NPC.velocity.Y = 16;
				NPC.rotation = MathHelper.PiOver2 * NPC.direction;
				//
			}

			if (AITimer > 40 && NPC.velocity.Y == 0 && NPC.oldVelocity.Y >= 0)
			{
				//vfx and sfx and projs here

				_jumpState++; //use the variable to track the final ground pound too

				for (int i = -1; i <= 1; i++)
				{
					if (i == 0)
						continue;

					float distStep = Main.rand.NextFloat(16, 32) * i * 16;
					Vector2 projPosition = FindGroundFromPosition(NPC.Bottom + Vector2.UnitX * distStep) - Vector2.UnitY * 80;

					Projectile.NewProjectile(NPC.GetSource_FromThis(), projPosition, Vector2.Zero, ModContent.ProjectileType<SandPillar>(), NPC.damage / 4, 3, Main.myPlayer, Math.Abs(i) * 20);
				}

				if (_jumpState < maxBounces + maxPounds)
					NPC.velocity.Y = -16;
				else
					NPC.velocity.Y = -5;

				AITimer = 0;

				if(Main.netMode != NetmodeID.Server && Main.LocalPlayer.Distance(NPC.Center) < 800)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(Main.screenPosition, Vector2.UnitY, 4, 3, 20));
			}

			NPC.velocity.Y += 0.38f;
		}

		else //rest before next attack
		{
			NPC.noGravity = false;
			NPC.knockBackResist = 0.3f;
			_curFrame.Y = 0;
			NPC.rotation = 0;
			AITimer++;

			if (AITimer > 120)
				NextAttack(player);
		}
	}

	private void DigErupt(Player player)
	{
		const int undergroundTime = 180;
		const int numEruptions = 3;
		const int restTime = 40;

		NPC.noTileCollide = true;
		NPC.noGravity = true;
		NPC.knockBackResist = 0f;

		if(_jumpState == 0)
		{
			_jumpState++;
			NPC.velocity = new Vector2(6 * NPC.direction, -12);
			SyncNPC();
		}

		if(!_inGround && _jumpState == 1)
		{
			NPC.velocity.Y += 0.4f;
			NPC.velocity.Y = Math.Min(NPC.velocity.Y, 24);

			if(Collision.SolidTiles(NPC.position, NPC.width, NPC.height))
			{
				_inGround = true;
				NPC.velocity = Vector2.Zero;

				if (Main.netMode != NetmodeID.Server && Main.LocalPlayer.Distance(NPC.Center) < 800)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(Main.screenPosition, Vector2.UnitY, 4, 3, 20));

				//fx here
			}
		}

		if(_inGround)
		{
			AITimer++;

			NPC.velocity.X = (float)Math.Sin(AITimer * MathHelper.TwoPi / 120) * 5 + NPC.DirectionTo(player.Center).X;
			NPC.position.Y = FindGroundFromPosition(NPC.position).Y;

			if (Main.rand.NextBool(4) && !Main.dedServ)
			{
				ParticleHandler.SpawnParticle(new SmokeCloud(NPC.Center - Vector2.UnitY * 32, -Vector2.UnitY * 8, Color.LightGoldenrodYellow, Main.rand.NextFloat(0.1f, 0.25f), EaseFunction.EaseCubicOut, 30)
				{
					Pixellate = true,
					DissolveAmount = 1,
					SecondaryColor = Color.SandyBrown,
					TertiaryColor = Color.SaddleBrown,
					PixelDivisor = 3,
					ColorLerpExponent = 0.5f,
					Layer = ParticleLayer.BelowSolid
				});
			}

			if (AITimer % (undergroundTime / (numEruptions + 1)) == 0 && AITimer != undergroundTime)
			{
				//projectile here					
				Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center - Vector2.UnitY * 80, Vector2.Zero, ModContent.ProjectileType<SandPillar>(), NPC.damage / 4, 3);

				if (Main.netMode != NetmodeID.Server && Main.LocalPlayer.Distance(NPC.Center) < 800)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(Main.screenPosition, Vector2.UnitY, 2, 3, 20));
			}

			if(AITimer > undergroundTime)
			{
				_inGround = false;
				NPC.velocity.Y = -15;
				AITimer = 0;
				_jumpState++;

				//fx here
				if (Main.netMode != NetmodeID.Server && Main.LocalPlayer.Distance(NPC.Center) < 800)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(Main.screenPosition, Vector2.UnitY, 4, 3, 20));
			}
		}

		if(_jumpState == 2)
		{
			AITimer++;
			NPC.velocity.Y *= 0.95f;

			if(AITimer > restTime)
				NextAttack(player);
		}
	}

	private void ScarabSwarm(Player player)
	{
		//durations of each segment
		const int attackStartTime = 40;
		const int swarmLength = 120;
		const int flashBoomChargeup = 60;
		const int restTime = 30;

		//short form calculations using durations of each segment
		int flashChargeStart = attackStartTime + swarmLength;
		int flashExplostionTime = attackStartTime + swarmLength + flashBoomChargeup;
		int attackEndTime = attackStartTime + swarmLength + flashBoomChargeup + restTime;

		NPC.noGravity = true;
		NPC.noTileCollide = true;
		NPC.knockBackResist = 0f;

		//attack start
		if(AITimer++ == 0)
		{
			NPC.velocity.Y = -6;
			NPC.velocity.X /= 2;
			//fx here?

		}

		NPC.velocity *= 0.95f;
		NPC.velocity.Y += (float)Math.Sin(MathHelper.TwoPi * 3 * AITimer / attackEndTime) / 10;

		if(AITimer == attackStartTime)
		{
			//proj here

			if (Main.netMode != NetmodeID.Server)
			{
				ParticleHandler.SpawnParticle(new TexturedPulseCircle(NPC.Center, Color.LightGoldenrodYellow, 1, 2400, 30, "GlowTrail", new Vector2(1, 1), EaseFunction.EaseCircularOut, true, 0.33f));
			}
		}

		if(AITimer > flashChargeStart && AITimer < flashExplostionTime)
		{
			//vfx here
		}

		if(AITimer == flashExplostionTime)
		{

			if(Main.netMode != NetmodeID.Server)
			{
				for (int i = 0; i < 3; i++)
					ParticleHandler.SpawnParticle(new DissipatingImage(NPC.Center, Color.Lerp(Color.LightGoldenrodYellow, Color.Goldenrod, 0.5f).Additive(), Main.rand.NextFloatDirection(), 0.66f, 0, "GodrayCircle", Vector2.Zero, new Vector2(3, 1.4f), 15));

			}
		}

		if(AITimer > attackEndTime)
			NextAttack(player);
	}

	private void NextAttack(Player player, AIPatterns? pattern = null)
	{
		_inGround = false;
		_jumpState = 0;
		AITimer = 0;
		NPC.rotation = 0;
		_curFrame.Y = 0;
		boredomTimer = 0;

		if(pattern != null)
		{
			CurrentPattern = (float)pattern.Value;
			SyncNPC();
			return;
		}

		List<AIPatterns> availablePatterns = [];

		AIPatterns[] phase1standard = [AIPatterns.Walking, AIPatterns.Leap];
		AIPatterns[] phase1strong = [AIPatterns.Dig, AIPatterns.GroundedSlam, AIPatterns.RollDash];

		AIPatterns[] phase2standard = [AIPatterns.FlyHover, AIPatterns.FlyingDash];
		AIPatterns[] phase2strong = [AIPatterns.ChainGroundPound, AIPatterns.DigErupt, AIPatterns.ScarabSwarm];

		if(!_hasPhaseChanged)
		{
			availablePatterns.AddRange(phase1standard);
			availablePatterns.AddRange(phase1strong);
		}

		else
		{
			availablePatterns.AddRange(phase2standard);
			availablePatterns.AddRange(phase2strong);
		}

		//Prune the current attack and attacks that shouldn't be used
		List<AIPatterns> temp = [];

		for(int i = 0; i < availablePatterns.ToArray().Length; i++)
		{
			if (availablePatterns[i] == (AIPatterns)CurrentPattern)
				continue;

			else if (!IsAttackValid(player, availablePatterns[i]))
				continue;

			temp.Add(availablePatterns[i]);
		}

		availablePatterns = temp;

		//Set a random attack from the remainders
		CurrentPattern = (float)availablePatterns[Main.rand.Next(0, availablePatterns.Count)];
		SyncNPC();
	}

	/// <summary>
	/// Checks if the given attack is viable for random selection, given the current position of the boss and terrain around it
	/// </summary>
	/// <param name="pattern"></param>
	/// <returns></returns>
	private bool IsAttackValid(Player player, AIPatterns pattern)
	{
		bool isValid = true;
		switch(pattern)
		{
			case AIPatterns.Walking:
				isValid = (AIPatterns)CurrentPattern != AIPatterns.Skitter;
				break;

			case AIPatterns.Leap:
				isValid = NPC.Distance(player.Center) > 160;
				break;

			case AIPatterns.RollDash:
				isValid = Math.Abs(NPC.Center.Y - player.Center.Y) < 64 && Math.Abs(NPC.Center.X - player.Center.X) > 48;
				break;

			case AIPatterns.GroundedSlam:
				isValid = Collision.SolidTiles(NPC.BottomLeft, NPC.width / 16, 3, false);
				break;

			case AIPatterns.Dig:
				isValid =  Collision.SolidTiles(NPC.BottomLeft, NPC.width / 16, 3, false);
				if ((AIPatterns)CurrentPattern == AIPatterns.BounceGroundPound)
					isValid = false;

				break;

			case AIPatterns.ChainGroundPound:
			case AIPatterns.DigErupt:
				isValid = !Collision.SolidTiles(NPC.position, NPC.width, NPC.height);
				break;
		}

		return isValid;
	}

	/// <summary>
	/// From a given input, translates the input to the surfacemost tile on the ground <br/>
	/// If the given input is inside the ground, instead moves upwards until reaching the surface
	/// </summary>
	/// <param name="input"></param>
	/// <returns></returns>
	private static Vector2 FindGroundFromPosition(Vector2 input)
	{
		Point tile = input.ToTileCoordinates();

		while (!Collision.SolidTiles(tile.ToWorldCoordinates(), 1, 1))
		{
			tile.Y += 1;
		}

		while (Collision.SolidTiles(tile.ToWorldCoordinates(), 1, 1))
		{
			tile.Y -= 1;
		}

		tile.Y += 1;

		return tile.ToWorldCoordinates();
	}

	private void BouncingTileWave(int numTiles, float maxHeight, int totalTime = 60, Vector2? offset = null)
	{
		for (int j = -1; j <= 1; j += 2)
		{
			BouncingTileWave(j, numTiles, maxHeight, totalTime, offset);
		}

		ParticleHandler.SpawnParticle(new MovingBlockParticle(FindGroundFromPosition(NPC.Center + (offset ?? Vector2.Zero)), totalTime / 2, maxHeight));
	}

	private void BouncingTileWave(int direction, int numTiles, float maxHeight, int totalTime = 60, Vector2? offset = null)
	{
		for (float i = 0; i < numTiles; i++)
		{
			float height = MathHelper.Lerp(maxHeight, 0, EaseFunction.EaseQuadIn.Ease(i / numTiles));
			int delay = (int)MathHelper.Lerp(0, totalTime / 2, (i + 1) / numTiles);
			ParticleHandler.SpawnQueuedParticle(new MovingBlockParticle(FindGroundFromPosition(NPC.Center + (offset ?? Vector2.Zero) + direction * Vector2.UnitX * 16 * (i + 1)), totalTime / 2, height), delay);
		}
	}

	private void CheckPlatform(Player player)
	{
		bool onplatform = true;
		for (int i = (int)NPC.position.X; i < NPC.position.X + NPC.width; i += NPC.width / 4)
		{ //check tiles beneath the boss to see if they are all platforms
			Tile tile = Framing.GetTileSafely(new Point((int)NPC.position.X / 16, (int)(NPC.position.Y + NPC.height + 8) / 16));
			if (!TileID.Sets.Platforms[tile.TileType])
				onplatform = false;
		}

		if (onplatform && NPC.Center.Y < player.position.Y - 20) //if they are and the player is lower than the boss, temporarily let the boss ignore tiles to go through them
			NPC.noTileCollide = true;
		else
			NPC.noTileCollide = false;
	}

	private void StepUp(Player player)
	{
		bool flag15 = true; //copy pasted collision step code from zombies
		if (player.Center.Y * 16 - 32 > NPC.position.Y)
			flag15 = false;

		if (!flag15 && NPC.velocity.Y == 0f)
			Collision.StepDown(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);

		if (NPC.velocity.Y >= 0f)
			Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY, 1, flag15, 1);
	}

	private void AnimateFrame(int horizontalFrame, int framesPerSecond, bool loop = true)
	{
		if(_curFrame.X != horizontalFrame)
		{
			_curFrame.X = horizontalFrame;
			_curFrame.Y = 0;
		}

		NPC.frameCounter++;

		int numFrames = _curFrame.X switch
		{
			0 => 1, //base
			1 => 8, //walk
			2 => 10, //horn swipe
			3 => 6, //roll windup
			4 => 14, //slam
			5 => 1, //ball
			_ => 1
		};

		if (NPC.frameCounter > 60.0 / Math.Abs(framesPerSecond))
		{
			NPC.frameCounter = 0;
			bool reversed = framesPerSecond < 0;

			if (reversed)
			{
				if (_curFrame.Y > 0)
					_curFrame.Y--;

				else if (loop)
					_curFrame.Y = numFrames - 1;
			}

			else
			{
				if (_curFrame.Y < numFrames - 1)
					_curFrame.Y++;

				else if (loop)
					_curFrame.Y = 0;
			}
		}
	}

	private void SyncNPC()
	{
		if (Main.netMode != NetmodeID.SinglePlayer)
			NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
	}

	public override bool CanHitPlayer(Player target, ref int cooldownSlot) => _contactDmgEnabled;

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		if (_inGround)
			return false;

		Texture2D bossTex = TextureAssets.Npc[NPC.type].Value;
		int verticalFrames = Main.npcFrameCount[NPC.type];
		const int horizontalFrames = 6;
		var frameSize = new Point(bossTex.Width / horizontalFrames, bossTex.Height / verticalFrames);

		var drawFrame = new Rectangle((int)_curFrame.X * frameSize.X + 2, (int)_curFrame.Y * frameSize.Y + 2, frameSize.X - 4, frameSize.Y - 2);
		var frameOrigin = _curFrame.X switch
		{
			5 => new Vector2(80, 102),
			_ => new Vector2(68, 100),
		};

		var flip = (NPC.direction > 0) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
		if (NPC.direction < 0)
			frameOrigin.X = drawFrame.Width - frameOrigin.X;

		for(int i = NPCID.Sets.TrailCacheLength[NPC.type] - 1; i >= 0; i--)
		{
			if (!_contactDmgEnabled && CurrentPattern != (float)AIPatterns.Skitter)
				break;

			float progress = 1 - (i / (float)NPCID.Sets.TrailCacheLength[NPC.type]);
			Vector2 oldCenter = NPC.oldPos[i] - Main.screenPosition + NPC.Size / 2;

			Main.EntitySpriteDraw(bossTex, oldCenter, drawFrame, NPC.GetAlpha(drawColor) * progress * 0.5f, NPC.oldRot[i], frameOrigin, NPC.scale, flip);
		}

		Main.EntitySpriteDraw(bossTex, NPC.Center - Main.screenPosition, drawFrame, NPC.GetAlpha(drawColor), NPC.rotation, frameOrigin, NPC.scale, flip);

		return false;
	}

	public override void SendExtraAI(BinaryWriter writer)
	{
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
	}

	public override int SpawnNPC(int tileX, int tileY)
	{
		NPC.velocity.Y = 1;
		return base.SpawnNPC(tileX, tileY);
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		for (int k = 0; k < 5; k++)
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, hit.HitDirection, -1f, 0, default, 1f);

		if (NPC.life <= 0 && Main.netMode != NetmodeID.Server)
		{
			//SoundEngine.TryGetActiveSound(wingSoundSlot, out ActiveSound sound);

			//if (sound is not null && sound.IsPlaying)
			//{
			//	sound.Stop();
				//wingSoundSlot = SlotId.Invalid;
			//}

			SpawnGores();
		}
	}

	public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
	{
		modifiers.Knockback *= 0.7f;

		if (!Main.player[projectile.owner].ZoneDesert)
			modifiers.FinalDamage /= 3;
	}

	public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
	{
		if (!player.ZoneDesert)
			modifiers.FinalDamage /= 3;
	}

	public override bool PreKill()
	{
		if (Main.netMode != NetmodeID.SinglePlayer)
			NetMessage.SendData(MessageID.WorldData);

		//NPC.PlayDeathSound("ScarabDeathSound");
		return true;
	}

	public override void ModifyNPCLoot(NPCLoot npcLoot)
	{
		/*npcLoot.AddMasterModeRelicAndPet<ScarabeusRelicItem, ScarabPetItem>();
		npcLoot.AddBossBag<BagOScarabs>();

		var notExpertRule = new LeadingConditionRule(new Conditions.NotExpert());
		notExpertRule.AddCommon<ScarabMask>(7);
		notExpertRule.AddCommon<Trophy1>(10);
		notExpertRule.AddCommon<SandsOfTime>(15);
		notExpertRule.AddCommon<Chitin>(1, 25, 36);
		notExpertRule.AddOneFromOptions<ScarabBow, LocustCrook, RoyalKhopesh, RadiantCane>();

		npcLoot.Add(notExpertRule);*/
	}

	private void SpawnGores()
	{
		for (int i = 1; i <= 7; i++)
			Gore.NewGoreDirect(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Scarab" + i.ToString()).Type, 1f);

		NPC.position += NPC.Size / 2;
		NPC.Size = new Vector2(100, 60);
		NPC.position -= NPC.Size / 2;

		static int randomDustType() => Main.rand.Next(3) switch
		{
			0 => 5,
			1 => 36,
			_ => 32,
		};

		for (int i = 0; i < 30; i++)
			Dust.NewDustDirect(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, randomDustType(), 0f, 0f, 100, default, Main.rand.NextBool() ? 2f : 0.5f).velocity *= 3f;

		for (int j = 0; j < 50; j++)
		{
			var dust = Dust.NewDustDirect(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, randomDustType(), 0f, 0f, 100, default, 1f);
			dust.velocity *= 5f;
			dust.noGravity = true;

			Dust.NewDustDirect(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, randomDustType(), 0f, 0f, 100, default, .82f).velocity *= 2f;
		}
	}
}