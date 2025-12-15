using SpiritReforged.Common.Easing;
using SpiritReforged.Common.MathHelpers;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Particles;
using System;
using System.IO;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Desert.Scarabeus.Boss;

[AutoloadBossHead]
[AutoloadGlowmask("255, 255, 255", false)]
public class ScarabeusBoss : ModNPC
{
	public float AITimer { get => NPC.ai[0]; set => NPC.ai[0] = value; }
	public float CurrentPattern { get => NPC.ai[1]; set => NPC.ai[1] = value; }

	private Point _curFrame;

	private bool _contactDmgEnabled = false;
	private bool _inGround = true;

	private int _jumpState = 0;

	private enum AIPatterns
	{
		SpawnAnimation,
		Walking,
		Leap,
		RollDash,
		GroundedSlam,
		Dig,
		BounceGroundPound,
		FlyingDash,
		ChainGroundPound,
		DigErupt,
		FlyingSlam,
		ScarabSwarm
	}

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[NPC.type] = 4;
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
		NPC.width = 64;
		NPC.height = 64;
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
		NPC.TargetClosest(true);
		Player player = Main.player[NPC.target];
		_contactDmgEnabled = false;

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

			case AIPatterns.GroundedSlam:
				GroundSlam(player);
				break;

			case AIPatterns.BounceGroundPound:
				BounceGroundPound(player);
				break;

			case AIPatterns.Dig:
				Dig(player);
				break;
		}
	}

	private void SpawnAnimation(Player player)
	{
		const int undergroundTime = 120;
		const int roarTime = 120;

		/*Todo: 
		 * foreground scarab particles fly across the screen from bottom left to top right
		 * screenshake
		 * ground beneath player starts emitting particles
		 * scarab bursts out of ground and roars
		*/

		AITimer++;

		if(AITimer == undergroundTime)
		{
			NPC.Center = player.Center;
			NPC.noTileCollide = false;
			NPC.noGravity = false;
			NPC.velocity.Y = -6;
			_curFrame.Y = 0;
		}

		if(AITimer >= undergroundTime + roarTime)
			NextAttack(player, AIPatterns.Walking);
	}

	private void Walking(Player player)
	{
		int maxWalkTime = 360;

		NPC.spriteDirection = NPC.direction;
		NPC.knockBackResist = 0.7f;
		AITimer++;
		_curFrame.Y = 0;
		CheckPlatform(player);

		//Check if grounded
		if (NPC.velocity.Y == 0 && NPC.oldVelocity.Y >= 0)
		{
			//Only move if too far from the player, try to move away a little bit if too close

			if(Math.Abs(NPC.position.X - player.position.X) > 100 && Math.Abs(NPC.velocity.X) < 12)
				NPC.velocity.X += Math.Sign(NPC.DirectionTo(player.position).X) * 0.1f;

			if(Math.Abs(NPC.position.X - player.position.X) < 100)
				NPC.velocity.X -= Math.Sign(NPC.DirectionTo(player.position).X) * 0.3f;
		}

		NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X, -12, 12);

		StepUp(player);

		/*
		 * Todo:
		 * Make it change to horn swipe if player sticks too close too long
		 * Leap if too far or can't traverse terrain and a leap would reach the player (Pits, height differences)
		 * Dig if too far or can't traverse terrain and a leap wouldn't reach player (Collision)
		 */

		if (AITimer > maxWalkTime)
			NextAttack(player);
	}

	private void Leap(Player player)
	{
		const int windupTime = 60;
		const int restTime = 45;

		bool HasJumped = _jumpState == 1;
		bool HasLanded = _jumpState == 2;

		NPC.spriteDirection = NPC.direction;
		NPC.knockBackResist = 0f;
		CheckPlatform(player);

		if (!HasJumped && !HasLanded)
		{
			_curFrame.Y = 0;

			//Check if grounded
			if (NPC.velocity.Y == 0 && NPC.oldVelocity.Y >= 0)
			{
				AITimer++;
				//Slow down for a bit, then calculate mortar velocity to jump towards player
				//Increase velocity if too far to reach player

				if (AITimer < windupTime)
					NPC.velocity.X *= 0.9f;

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

		else if (!HasLanded)
		{
			_curFrame.Y = 2;
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
			_curFrame.Y = 0;

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
		_curFrame.Y = 1;
		AITimer++;

		if(AITimer < windupTime)
		{
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
		const int windupTime = 80;
		const int restTime = 45;

		NPC.spriteDirection = NPC.direction;
		NPC.noTileCollide = false;
		NPC.noGravity = false;
		NPC.knockBackResist = 0f;
		CheckPlatform(player);
		AITimer++;

		if(AITimer < windupTime)
		{
			_curFrame.Y = 3;
			NPC.velocity *= 0.7f;
		}

		if(AITimer == windupTime)
		{
			_contactDmgEnabled = true;
			_curFrame.Y = 0;
			//projectiles and sfx here
		}

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
		NPC.noGravity = false;
		NPC.knockBackResist = 0.2f;
		CheckPlatform(player);
		_curFrame.Y = 1;

		if(_jumpState < maxBounces)
		{
			_contactDmgEnabled = true;

			//Check if grounded
			if (NPC.velocity.Y == 0 && NPC.oldVelocity.Y >= 0)
			{
				_jumpState++;
				NPC.velocity.Y = -16;
			}

			else
			{
				float desiredVel = (NPC.Center.X < player.Center.X) ? 16 : -16;
				NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, desiredVel, 0.01f);

				if(NPC.velocity.Y < 12)
					NPC.velocity.Y += 0.08f;

				NPC.rotation += NPC.velocity.X / 120;
			}
		}

		else if (_jumpState == maxBounces)
		{
			AITimer++;
			_curFrame.Y = 2;
			_contactDmgEnabled = true;

			if (AITimer < 40)
			{
				_curFrame.Y = 1;
				float desiredVel = (NPC.Center.X < player.Center.X) ? 16 : -16;
				NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, desiredVel, 0.01f);

				if (NPC.velocity.Y < -4)
					NPC.velocity.Y += 0.08f;

				NPC.rotation += NPC.velocity.X / 120;

				if (AITimer > 30)
					NPC.velocity.X *= 0.9f;
			}

			else if (AITimer < 70)
			{
				NPC.velocity.X = 0;
				NPC.velocity.Y = MathHelper.Lerp(NPC.velocity.Y, -3, 0.1f);
				NPC.rotation = -MathHelper.PiOver2;
			}

			else if (AITimer == 70)
			{
				NPC.velocity.Y = 16;
				NPC.rotation = MathHelper.PiOver2;
				//
			}

			if(AITimer > 70 && NPC.velocity.Y == 0 && NPC.oldVelocity.Y >= 0)
			{
				//vfx and sfx and projs here

				_jumpState++; //use the variable to track the final ground pound too
			}
		}

		else //rest before next attack
		{
			_curFrame.Y = 0;
			NPC.rotation = 0;
			AITimer++;

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
		_curFrame.Y = 2;

		if(AITimer < digStartTime)
		{
			//dig into ground anim here, placeholder rn
			NPC.velocity = Vector2.Zero;
			NPC.position.Y += 0.5f;
			NPC.alpha += (255 / digStartTime);
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
			NPC.velocity.X = (float)Math.Sin(AITimer * MathHelper.TwoPi / 120) * 5 + NPC.DirectionTo(player.Center).X;
			NPC.position.Y = FindGroundFromPosition(NPC.position).Y;

			if(Main.rand.NextBool(4) && !Main.dedServ)
			{
				ParticleHandler.SpawnParticle(new SmokeCloud(NPC.Center - Vector2.UnitY * 32, -Vector2.UnitY * 8, Color.LightGoldenrodYellow, Main.rand.NextFloat(0.1f, 0.25f), EaseFunction.EaseCubicOut, 30) 
				{ 
					Pixellate = true, 
					DissolveAmount = 1, 
					SecondaryColor = Color.SandyBrown,
					TertiaryColor = Color.SaddleBrown,
					PixelDivisor = 3,
					ColorLerpExponent = 0.5f
				});
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

	private void NextAttack(Player player, AIPatterns? pattern = null)
	{
		_inGround = false;
		_jumpState = 0;
		AITimer = 0;
		NPC.rotation = 0;

		if(pattern != null)
		{
			CurrentPattern = (float)pattern.Value;
			SyncNPC();
			return;
		}

		List<AIPatterns> availablePatterns = [];

		//phase check here to determine what attacks to add
		availablePatterns.AddRange([AIPatterns.Walking, AIPatterns.RollDash, AIPatterns.Dig, AIPatterns.Leap, AIPatterns.GroundedSlam]);

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
		const int horizontalFrames = 2;
		var frameSize = new Point(bossTex.Width / horizontalFrames, bossTex.Height / verticalFrames);

		var drawFrame = new Rectangle(_curFrame.X * frameSize.X, _curFrame.Y * frameSize.Y, frameSize.X, frameSize.Y);
		var flip = (NPC.direction > 0) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		Main.EntitySpriteDraw(bossTex, NPC.Center - Main.screenPosition, drawFrame, NPC.GetAlpha(drawColor), NPC.rotation, drawFrame.Size() / 2, NPC.scale, flip);

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