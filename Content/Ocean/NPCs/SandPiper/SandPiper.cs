using Terraria.GameContent.Bestiary;
using Terraria.DataStructures;
using SpiritReforged.Common.NPCCommon;

namespace SpiritReforged.Content.Ocean.NPCs.SandPiper;

[SpawnPack(3, 5)]
[AutoloadCritter]
public class SandPiper : ModNPC
{
	private const int STATE_WALK = 0;
	private const int STATE_FLY = 1;
	private const int STATE_PECK = 2;

	private ref float State => ref NPC.ai[0];
	private ref float Timer => ref NPC.ai[1];
	private ref float WalkState => ref NPC.ai[2];

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = 4;
		NPCID.Sets.ShimmerTransformToNPC[Type] = NPCID.Shimmerfly;
	}

	public override void SetDefaults()
	{
		NPC.dontCountMe = true;
		NPC.width = 16;
		NPC.height = 16;
		NPC.damage = 0;
		NPC.defense = 0;
		NPC.lifeMax = 5;
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath1;
		NPC.knockBackResist = 1f;
		NPC.aiStyle = -1;
		NPC.npcSlots = 0;
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
	{
		bestiaryEntry.AddInfo(this, "Ocean");
		bestiaryEntry.UIInfoProvider = new CritterUICollectionInfoProvider(ContentSamples.NpcBestiaryCreditIdsByNpcNetIds[Type]);
	}

	public override void OnSpawn(IEntitySource source)
	{
		NPC.direction = -1;
		NPC.netUpdate = true;
	}

	public override void AI()
	{
		NPC.noGravity = true;

		if (State == STATE_WALK) //Grounded behaviour
		{
			NPC.noGravity = NPC.wet;

			if (NPC.wet)
			{
				NPC.velocity.Y *= 0.7f;

				if (Collision.WetCollision(NPC.position, NPC.width, (int)(NPC.height / 1.25f)))
					NPC.velocity.Y -= 0.2f;
			}

			if (NPC.collideX)
			{
				Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);

				if (NPC.collideX)
					WalkState *= -1;
			}

			if (Main.netMode != NetmodeID.MultiplayerClient && --Timer <= 0) //Randomly select a direction and speed to walk
			{
				Timer = Main.rand.Next(80, 160);
				NPC.netUpdate = true;

				if (WalkState == 0)
					if (!NPC.wet && Main.rand.NextBool(3))
					{
						State = STATE_PECK;
						Timer = Main.rand.Next(10, 30);
					}
					else
						WalkState = Main.rand.NextFloat(-1.8f, 1.8f);
				else
					WalkState = 0;
			}

			for (int i = 0; i < Main.maxPlayers; ++i) //Scare check
			{
				Player player = Main.player[i];

				if (player.active && !player.dead && player.DistanceSQ(NPC.Center) < 300 * 300)
				{
					float dist = player.Distance(NPC.Center);

					if (dist > 150)
						WalkState = Math.Sign(NPC.Center.X - player.Center.X) * (1 - (dist - 150f) / 150f) * 5;
					else
					{
						State = STATE_FLY;
						Timer = 0;

						NPC.netUpdate = true;
						break;
					}
				}
			}

			if (WalkState != 0)
				NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, WalkState, .1f);
			else
				NPC.velocity.X *= 0.92f;
		}
		else if (State == STATE_FLY) //Flying behaviour //Almost entirely cleaned up vanilla AI
		{
			if (Main.player[NPC.target].dead)
				return;

			for (int i = 0; i < Main.maxPlayers; ++i) //Scare check
			{
				Player player = Main.player[i];

				if (player.active && !player.dead && player.DistanceSQ(NPC.Center) < 400 * 400)
				{
					Timer--;
					break;
				}
			}

			if (++Timer >= 130f) //Fall down and switch states when landing
			{
				if ((NPC.velocity.Y == 0f || NPC.collideY) && Collision.SolidCollision(NPC.BottomLeft, NPC.width, 6) || NPC.wet)
				{
					NPC.velocity = Vector2.Zero;
					State = STATE_WALK;
					Timer = 0f;
				}
				else
				{
					NPC.velocity.X *= 0.98f;
					NPC.velocity.Y += 0.15f;

					if (NPC.velocity.Y > 2.5f)
						NPC.velocity.Y = 2.5f;
				}

				return;
			}

			if (NPC.collideX)
			{
				NPC.direction *= -1;
				NPC.velocity.X = NPC.oldVelocity.X * -0.5f;
				NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X, -2, 2);
			}

			if (NPC.collideY)
			{
				NPC.velocity.Y = NPC.oldVelocity.Y * -0.5f;
				NPC.velocity.Y = MathHelper.Clamp(NPC.velocity.Y, -1, 1);
			}

			if (NPC.direction == -1 && NPC.velocity.X > -3f)
			{
				NPC.velocity.X -= 0.15f;

				if (NPC.velocity.X > 4f)
					NPC.velocity.X -= 0.1f;
				else if (NPC.velocity.X > 0f)
					NPC.velocity.X -= 0.05f;

				if (NPC.velocity.X < -4f)
					NPC.velocity.X = -4f;
			}
			else if (NPC.direction == 1 && NPC.velocity.X < 3f)
			{
				NPC.velocity.X += 0.15f;

				if (NPC.velocity.X < -4f)
					NPC.velocity.X += 0.1f;
				else if (NPC.velocity.X < 0f)
					NPC.velocity.X += 0.05f;

				if (NPC.velocity.X > 4f)
					NPC.velocity.X = 4f;
			}

			const int ScanCheck = 15;

			int tileX = (int)(NPC.Center.X / 16f) + NPC.direction;
			int tileY = (int)(NPC.Bottom.Y / 16f);
			bool closeGround = true;
			bool veryCloseGround = false;

			for (int y = tileY; y < tileY + ScanCheck; y++)
				if (Main.tile[tileX, y].HasUnactuatedTile && Main.tileSolid[Main.tile[tileX, y].TileType] || Main.tile[tileX, y].LiquidAmount > 0)
				{
					if (y < tileY + 5)
						veryCloseGround = true;

					closeGround = false;
					break;
				}

			if (closeGround)
				NPC.velocity.Y += 0.08f;
			else
				NPC.velocity.Y -= 0.08f;

			if (veryCloseGround)
				NPC.velocity.Y -= 0.15f;

			NPC.velocity.Y = MathHelper.Clamp(NPC.velocity.Y, -4.5f, 4);
		}
		else if (State == STATE_PECK) //Pecking behaviour
			if (--Timer <= 0)
			{
				State = STATE_WALK;
				Timer = 30;
				WalkState = 0;
			}

		if (WalkState != 0)
			NPC.spriteDirection = NPC.direction = NPC.velocity.X > 0 ? 1 : -1;
	}

	public override void FindFrame(int frameHeight)
	{
		if (NPC.IsABestiaryIconDummy && --Timer <= 0) //Remain stationary and randomly peck the ground in the bestiary window
		{
			Timer = 120;

			if (State == STATE_PECK)
				State = STATE_WALK;
			else if (Main.rand.NextBool(3))
			{
				State = STATE_PECK;
				Timer = Main.rand.Next(10, 30);
			}
		}

		int frameCount = Main.npcFrameCount[Type];
		switch (State)
		{
			case STATE_FLY:
				NPC.frame.X = 44;
				frameCount = 3;
				break;
			case STATE_PECK:
				NPC.frame.X = 22;
				frameCount = 2;
				break;
			case STATE_WALK:
				if (NPC.wet)
				{
					NPC.frame.X = 66;
					frameCount = 1;
				}
				else
				{
					NPC.frame.X = 0;
					frameCount = Main.npcFrameCount[Type];
				}

				break;
		} //Determine which column to use and the amount of frames inside

		NPC.frame.Width = 22;

		if (State != STATE_PECK)
			if (State == 0 && NPC.velocity.X == 0)
				NPC.frameCounter = 0;
			else
				NPC.frameCounter += MathHelper.Min(Math.Abs(NPC.velocity.X), 1) * 0.15f; //Move speed influences our animation speed slightly
		else
			NPC.frameCounter += 0.15f;

		NPC.frameCounter %= frameCount;
		int frame = (int)NPC.frameCounter;
		NPC.frame.Y = frame * frameHeight;
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		var effects = NPC.direction == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
		Color color = NPC.GetNPCColorTintedByBuffs(NPC.IsABestiaryIconDummy ? Color.White : NPC.GetAlpha(drawColor));

		spriteBatch.Draw(TextureAssets.Npc[Type].Value, NPC.Center - screenPos + new Vector2(0, NPC.gfxOffY + 4), NPC.frame, color, NPC.rotation, NPC.frame.Size() / 2, NPC.scale, effects, 0);

		return false;
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (NPC.life <= 0 && Main.netMode != NetmodeID.Server)
		{
			for (int i = 0; i < 10; i++)
			{
				var dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Blood);
				dust.velocity = dust.position.DirectionFrom(NPC.Center);
			}

			Gore.NewGore(Entity.GetSource_Death(), NPC.Center, NPC.velocity, Mod.Find<ModGore>("SandPiper1").Type);
		}
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo)
	{
		if (spawnInfo.Player.ZoneBeach && Main.dayTime && !spawnInfo.Water)
			return spawnInfo.PlayerInTown ? 2 : 1;

		return 0;
	}
}