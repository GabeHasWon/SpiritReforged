using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Content.SaltFlats.Biome;
using SpiritReforged.Content.Savanna.Biome;
using System.IO;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Savanna.NPCs;

[AutoloadCritter]
public class Jerboa : ModNPC
{
	public enum State : byte
	{
		Idle,
		Scratch,
		Sniff,
		Hop,
		Run
	}

	public ref float Counter => ref NPC.ai[0]; //Used to change behaviour at intervals
	public ref float TargetSpeed => ref NPC.ai[1]; //Stores a direction to lerp to over time

	private static readonly int[] endFrames = [1, 11, 13, 6, 10];

	public State AnimationState = State.Idle;
	private State _lastAnimationState;
	private bool _didHop = false;

	public override void SetStaticDefaults()
	{
		//Add critter item defaults
		ItemEvents.CreateItemDefaults(
			this.AutoItemType(),
			static item => item.value = Item.sellPrice(0, 0, 0, 45));

		Main.npcFrameCount[Type] = 13; //Rows

		Main.npcCatchable[Type] = true;
		NPCID.Sets.CountsAsCritter[Type] = true;
		NPCID.Sets.TakesDamageFromHostilesWithoutBeingFriendly[Type] = true;
		NPCID.Sets.ShimmerTransformToNPC[Type] = NPCID.Shimmerfly;
	}

	public override void SetDefaults()
	{
		NPC.Size = new Vector2(16);
		NPC.lifeMax = 5;
		NPC.chaseable = false;
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath1;
		NPC.knockBackResist = 1f;
		AIType = -1;
		SpawnModBiomes = [ModContent.GetInstance<SavannaBiome>().Type];
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "");

	public override void AI()
	{
		NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, TargetSpeed, 0.1f);

		if (AnimationState is State.Hop)
		{
			bool wasIdle = _lastAnimationState == State.Idle;
			if (NPC.velocity.Y == 0)
			{
				if (_didHop)
				{
					ChangeAnimationState(State.Idle);
					_didHop = false;

					if (wasIdle)
					{
						NPC.velocity.X = TargetSpeed = 0; //Stop upon landing
					}
				}
				else
				{
					NPC.velocity.X *= 0.9f;
				}

				if ((int)NPC.frameCounter >= 2 && !_didHop)
				{
					NPC.velocity.Y = -Main.rand.NextFloat(3, 4.5f);
					_didHop = true;
				}
			}
			else if (NPC.collideX && NPC.velocity.Y > 0) //Bounce off of walls when descending
			{
				TargetSpeed = -TargetSpeed;
				NPC.velocity.X = TargetSpeed * 0.5f;
			}
		}
		else if (AnimationState is State.Scratch or State.Sniff)
		{
			if (AnimationState != State.Hop || NPC.collideY)
				NPC.velocity.X = 0;

			if ((int)NPC.frameCounter >= endFrames[(int)AnimationState] - 1)
			{
				ChangeAnimationState(State.Idle);
				Counter = 150;
			}
		}
		else
		{
			if (AnimationState == State.Idle && PlayerInRange(200) && (int)Main.player[NPC.target].velocity.X != 0)
			{
				TargetSpeed = (Main.player[NPC.target].Center.X > NPC.Center.X ? -1 : 1) * 3f;
			}
			else if (Main.netMode != NetmodeID.MultiplayerClient && Counter % 80 == 0)
			{
				float oldTargetSpeed = TargetSpeed;
				int direction = Main.rand.NextFromList(-1, 0, 1);

				TargetSpeed = direction * Main.rand.NextFloat(2f, 3f);

				if (TargetSpeed != oldTargetSpeed)
					NPC.netUpdate = true;
			}

			var state = (Math.Abs(NPC.velocity.X) > 0.2f) ? State.Run : State.Idle;
			ChangeAnimationState(state);

			if (state is State.Run)
			{
				if (Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool(35))
				{
					ChangeAnimationState(State.Hop);
					NPC.netUpdate = true;
				}
				else
				{
					Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);
				}
			}
			else
			{
				if (Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool(20))
				{
					ChangeAnimationState(State.Hop);

					TargetSpeed = Main.rand.NextFromList(-1, 1) * Main.rand.NextFloat(2f, 3f);
					NPC.netUpdate = true;
				}

				if (Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool(60))
				{
					ChangeAnimationState(Main.rand.Next([State.Sniff, State.Scratch]));
					NPC.netUpdate = true;
				}
			}
		}

		//Set direction
		if (Math.Sign(NPC.velocity.X) is int value && value != 0)
			NPC.direction = NPC.spriteDirection = value;

		Counter++;
	}

	private void ChangeAnimationState(State toState)
	{
		if (AnimationState != toState)
		{
			_lastAnimationState = AnimationState;

			NPC.frameCounter = 0;
			Counter = 0;
			AnimationState = toState;
		}
	}

	private bool PlayerInRange(int distance)
	{
		NPC.TargetClosest();
		return NPC.HasPlayerTarget && Main.player[NPC.target].DistanceSQ(NPC.Center) < distance * distance;
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (!Main.dedServ)
		{
			for (int i = 0; i < ((NPC.life <= 0) ? 20 : 3); i++)
				Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Blood, Scale: Main.rand.NextFloat(0.8f, 2f)).velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f);
		}
	}

	public override void FindFrame(int frameHeight)
	{
		bool canLoop = AnimationState == State.Run;
		float frameRate = (AnimationState == State.Run) ? Math.Min(Math.Abs(NPC.velocity.X) / 5f, 0.3f) : 0.2f; //Rate depends on movement speed

		NPC.frame.Width = 36;
		NPC.frame.X = NPC.frame.Width * (int)AnimationState;
		NPC.frameCounter += frameRate;

		if (AnimationState == State.Hop && NPC.velocity.Y != 0)
			NPC.frameCounter = Math.Min(NPC.frameCounter, (NPC.velocity.Y > 0) ? 4 : 2);

		if (canLoop)
			NPC.frameCounter %= endFrames[(int)AnimationState];

		NPC.frame.Y = (int)Math.Min(endFrames[(int)AnimationState] - 1, NPC.frameCounter) * frameHeight;
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo)
	{
		if (!spawnInfo.Common() || spawnInfo.Water || !spawnInfo.Player.InModBiome<SavannaBiome>() || !SceneTileCounter.GetSurvey<SaltBiome>().tileTypes.Contains(spawnInfo.SpawnTileType))
			return 0;

		return 0.2f;
	}

	public override void SendExtraAI(BinaryWriter writer) => writer.Write((byte)AnimationState);
	public override void ReceiveExtraAI(BinaryReader reader)
	{
		int state = reader.ReadByte();
		ChangeAnimationState((State)state);
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		var texture = TextureAssets.Npc[Type].Value;
		var source = NPC.frame with { Width = NPC.frame.Width - 2, Height = NPC.frame.Height - 2 }; //Remove padding
		var position = NPC.Center - screenPos + new Vector2(0, NPC.gfxOffY - (source.Height - NPC.height) / 2 + 2);
		var effects = (NPC.spriteDirection == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		Main.EntitySpriteDraw(texture, position, source, NPC.DrawColor(drawColor), NPC.rotation, source.Size() / 2, NPC.scale, effects);
		return false;
	}
}