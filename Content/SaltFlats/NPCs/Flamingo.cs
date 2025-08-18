using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Content.SaltFlats.Biome;
using System.IO;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.SaltFlats.NPCs;

[SpawnPack(3, 4)]
public class Flamingo : ModNPC
{
	public enum State : byte
	{
		IdleReset,
		Walk,
		Lower,
		Muncha,
		Rise,
		Flamingosis
	}

	public State AnimationState
	{
		get => (State)NPC.ai[0];
		set => NPC.ai[0] = (int)value;
	} //What animation is currently being played
	public ref float Counter => ref NPC.ai[1]; //Used to change behaviour at intervals
	public ref float TargetSpeed => ref NPC.ai[2]; //Stores a direction to lerp to over time

	private static readonly int[] endFrames = [4, 8, 6, 12, 5, 5];

	private float _frameRate = 0.2f;
	private float _acceleration = 0.025f;
	private bool _pink;

	public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 12; //Rows
	public override void SetDefaults()
	{
		NPC.Size = new Vector2(20, 40);
		NPC.lifeMax = 50;
		NPC.value = 44f;
		NPC.chaseable = false;
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath1;
		NPC.knockBackResist = 0.5f;
		NPC.direction = 1; //Don't start at 0
		AIType = -1;
		SpawnModBiomes = [ModContent.GetInstance<SaltBiome>().Type];
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "");
	public override void OnSpawn(IEntitySource source)
	{
		const float distance = 100 * 100;
		_pink = Main.rand.NextBool(3);

		if (Main.rand.NextBool(15)) //Randomly spawn in a flying state
		{
			NPC.TargetClosest();
			if (NPC.HasPlayerTarget)
			{
				foreach (var npc in Main.ActiveNPCs)
				{
					if (npc.type == Type && npc.DistanceSQ(NPC.Center) < distance * distance && npc.ModNPC is Flamingo flamingo)
					{
						flamingo.ChangeAnimationState(State.Flamingosis);
						flamingo.TargetSpeed = ((NPC.Center.X < Main.player[NPC.target].Center.X) ? 1 : -1) * 5;

						npc.netUpdate = true;
					}
				}
			}
		}

		NPC.netUpdate = true;
	}

	public override void AI()
	{
		const int hoverDistance = 15;
		NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, TargetSpeed, _acceleration);

		_frameRate = 0.2f; //Defaults
		_acceleration = 0.015f;

		if (AnimationState is State.Flamingosis)
		{
			_frameRate = Math.Min(Math.Abs(NPC.velocity.X) / 5f, 0.2f);

			Point tileCoords = NPC.Bottom.ToTileCoordinates();
			bool inRange = false;

			for (int i = 0; i < hoverDistance; i++)
			{
				if (!WorldGen.InWorld(tileCoords.X, tileCoords.Y + i))
					break;

				var t = Main.tile[tileCoords.X, tileCoords.Y + i];
				if (t.HasUnactuatedTile && Main.tileSolid[t.TileType])
				{
					inRange = true;
					break;
				}
			}

			if (NPC.collideX)
				TargetSpeed = -TargetSpeed;

			NPC.velocity.Y = MathHelper.Lerp(NPC.velocity.Y, inRange ? -3 : 3, 0.02f);
			NPC.noGravity = true;

			NPC.EncourageDespawn(120);
		}
		else
		{
			if (AnimationState is State.Lower or State.Muncha or State.Rise)
			{
				NPC.velocity.X = 0;

				if ((int)NPC.frameCounter >= endFrames[(int)AnimationState] - 1)
				{
					var state = State.Muncha;

					if (AnimationState is State.Muncha)
					{
						bool playerInRange = PlayerInRange(200);
						if (playerInRange || Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool(5))
						{
							state = State.Rise;
							NPC.netUpdate |= !playerInRange;
						}
					}
					else if (AnimationState is State.Rise)
					{
						state = State.IdleReset;
					}

					ChangeAnimationState(state);

					if (state is State.IdleReset)
						Counter = 150;
				}
			}
			else
			{
				if (Counter % 200 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
				{
					float oldTargetSpeed = TargetSpeed;
					int direction = Main.rand.NextFromList(-1, 0, 1);

					TargetSpeed = (direction != 0 && GetNearest() is NPC flamingo) ? Math.Sign(flamingo.Center.X - NPC.Center.X) : direction * Main.rand.NextFloat(0.8f, 1.5f);

					if (TargetSpeed != oldTargetSpeed)
						NPC.netUpdate = true;
				}

				var state = (Math.Abs(NPC.velocity.X) > 0.2f) ? State.Walk : State.IdleReset;
				ChangeAnimationState(state);

				if (state is State.Walk)
				{
					Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);
					_frameRate = Math.Min(Math.Abs(NPC.velocity.X) / 5f, 0.2f); //Rate depends on movement speed
				}
				else
				{
					_acceleration = 0.1f;

					if (Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool(50) && Collision.WetCollision(NPC.position + Vector2.UnitY * 8, NPC.width, NPC.height) && !PlayerInRange(200))
					{
						ChangeAnimationState(State.Lower);
						NPC.netUpdate = true;
					}
				}
			}
		}

		//Set direction
		if (Math.Sign(NPC.velocity.X) is int value && value != 0)
			NPC.direction = NPC.spriteDirection = value;

		Counter++;
	}

	private NPC GetNearest()
	{
		float distance = 1000 * 1000;
		NPC value = null;

		foreach (var npc in Main.ActiveNPCs)
		{
			float distanceSQ = npc.DistanceSQ(NPC.Center);
			if (npc.whoAmI != NPC.whoAmI && npc.type == Type && distanceSQ < distance * distance)
			{
				distance = distanceSQ;
				value = npc;
			}
		}

		return value;
	}

	private bool PlayerInRange(int distance)
	{
		NPC.TargetClosest();
		return NPC.HasPlayerTarget && Main.player[NPC.target].DistanceSQ(NPC.Center) < distance * distance;
	}

	private void ChangeAnimationState(State toState)
	{
		if (AnimationState != toState)
		{
			AnimationState = toState;
			NPC.frameCounter = 0;
			Counter = 0;
		}
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		bool dead = NPC.life <= 0;
		if (!Main.dedServ)
		{
			for (int i = 0; i < (dead ? 20 : 3); i++)
				Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Blood, Scale: Main.rand.NextFloat(0.8f, 2f)).velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f);

			if (dead)
			{
				for (int i = 1; i < 4; i++)
				{
					int type = Mod.Find<ModGore>((_pink ? "FlamingoPink" : "FlamingoRed") + i).Type;
					Gore.NewGore(NPC.GetSource_Death(), Main.rand.NextVector2FromRectangle(NPC.getRect()), NPC.velocity * Main.rand.NextFloat(.3f), type);
				}
			}
		}

		if (!dead)
		{
			NPC.TargetClosest();
			if (NPC.HasPlayerTarget)
			{
				ChangeAnimationState(State.Flamingosis);
				TargetSpeed = ((NPC.Center.X < Main.player[NPC.target].Center.X) ? -1 : 1) * 5;
			}
		}
	}

	public override void FindFrame(int frameHeight)
	{
		bool canLoop = AnimationState is State.Walk or State.Flamingosis or State.Muncha;

		NPC.frame.Width = 84;
		NPC.frame.X = NPC.frame.Width * (int)AnimationState + (_pink ? 504 : 0);

		NPC.frameCounter += _frameRate;

		if (canLoop)
			NPC.frameCounter %= endFrames[(int)AnimationState];

		NPC.frame.Y = (int)Math.Min(endFrames[(int)AnimationState] - 1, NPC.frameCounter) * frameHeight;
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		var texture = TextureAssets.Npc[Type].Value;
		var source = NPC.frame with { Width = NPC.frame.Width - 2, Height = NPC.frame.Height - 2 }; //Remove padding
		var position = NPC.Center - screenPos + new Vector2(0, NPC.gfxOffY - (source.Height - NPC.height) / 2 + 2);

		var effects = (NPC.spriteDirection == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
		var color = NPC.GetAlpha(NPC.GetNPCColorTintedByBuffs(drawColor));

		Main.EntitySpriteDraw(texture, position, source, color, NPC.rotation, source.Size() / 2, NPC.scale, effects);
		return false;
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo)
	{
		if (spawnInfo.Invasion || spawnInfo.Water || !spawnInfo.Player.InModBiome<SaltBiome>())
			return 0;

		return 0.2f;
	}

	public override void SendExtraAI(BinaryWriter writer) => writer.Write(_pink);
	public override void ReceiveExtraAI(BinaryReader reader) => _pink = reader.ReadBoolean();
}