using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Content.Savanna.Biome;
using SpiritReforged.Content.Vanilla.Food;
using System.IO;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Savanna.NPCs.Gar;

[AutoloadCritter]
[AutoloadBanner]
public class Gar : ModNPC
{
	private ref float YMovement => ref NPC.ai[0]; // Y Movement (adapted from vanilla)
	private ref float Proximity => ref NPC.ai[1]; // Player proximity 
	private ref float Resting => ref NPC.ai[2]; // Resting check;
	private ref float RestTimer => ref NPC.ai[3]; // Loop through resting phase

	public bool hasPicked = false;

	float frameTimer;
	int pickedType;

	public override void SetStaticDefaults()
	{
		CreateItemDefaults();

		Main.npcFrameCount[NPC.type] = 12;
		Main.npcCatchable[NPC.type] = true;
		NPCID.Sets.CountsAsCritter[Type] = true;
		NPCID.Sets.ShimmerTransformToNPC[NPC.type] = NPCID.Shimmerfly;
	}

	public virtual void CreateItemDefaults()
	{
		ItemEvents.CreateItemDefaults(this.AutoItemType(), item => item.value = Item.sellPrice(0, 0, 5, 37));
		ItemEvents.CreateItemDefaults(this.AutoItemType("Banner"), item => item.value = Item.sellPrice(0, 0, 2, 0));
	}

	public override void SetDefaults()
	{
		NPC.width = 40;
		NPC.height = 22;
		NPC.damage = 0;
		NPC.defense = 0;
		NPC.lifeMax = 5;
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath1;
		NPC.knockBackResist = .35f;
		NPC.aiStyle = -1;
		NPC.noGravity = true;
		NPC.npcSlots = 0;
		NPC.dontCountMe = true;
		SpawnModBiomes = [ModContent.GetInstance<SavannaBiome>().Type];
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "");

	public override void AI()
	{
		Player target = Main.player[NPC.target];
		RestTimer++;

		if (NPC.wet) //swimming AI (adapted from vanilla)
		{
			if (NPC.rotation != 0f)
				NPC.rotation *= .9f;

			if (NPC.direction == 0)
				NPC.TargetClosest();

			int tileX = (int)NPC.Center.X / 16;
			int tileY = (int)(NPC.Bottom.Y / 16f);

			// what to do if sloped tiles
			if (Main.tile[tileX, tileY].TopSlope)
			{
				if (Main.tile[tileX, tileY].LeftSlope)
				{
					NPC.direction = -1;
					NPC.velocity.X = Math.Abs(NPC.velocity.X) * -1f;
				}
				else
				{
					NPC.direction = 1;
					NPC.velocity.X = Math.Abs(NPC.velocity.X);
				}
			}
			else if (Main.tile[tileX, tileY + 1].TopSlope)
			{
				if (Main.tile[tileX, tileY + 1].LeftSlope)
				{
					NPC.direction = -1;
					NPC.velocity.X = Math.Abs(NPC.velocity.X) * -1f;
				}
				else
				{
					NPC.direction = 1;
					NPC.velocity.X = Math.Abs(NPC.velocity.X);
				}
			}

			//Predation: seeks out Killifish to kill
			foreach (var otherNPC in Main.ActiveNPCs)
			{
				if (otherNPC.type == ModContent.NPCType<Killifish.Killifish>() || otherNPC.type == ModContent.NPCType<Killifish.GoldKillifish>())
				{
					if (NPC.DistanceSQ(otherNPC.Center) < 100 * 65 && otherNPC.wet)
					{
						Vector2 vel = NPC.DirectionTo(otherNPC.Center) * 3f;
						NPC.velocity = vel;
						NPC.rotation = MathHelper.WrapAngle((float)Math.Atan2(NPC.velocity.Y, NPC.velocity.X) + (NPC.velocity.X < 0 ? MathHelper.Pi : 0));
						NPC.friendly = false;
						NPC.damage = 1;

						if (NPC.velocity.X <= 0)
						{
							NPC.spriteDirection = -1;
							NPC.direction = -1;
							NPC.netUpdate = true;
						}
						else if (NPC.velocity.X > 0)
						{
							NPC.spriteDirection = 1;
							NPC.direction = 1;
							NPC.netUpdate = true;
						}

						Resting = 0;
						break;
					}
					else
					{
						//reset friendliness otherwise
						NPC.damage = 0;
					}
				}
			}

			// switch directions if colliding
			if (NPC.collideX)
			{
				NPC.velocity.X *= -1f;
				NPC.direction *= -1;
				NPC.netUpdate = true;
			}

			// I don't know how often this happens, but if fish bonks head or hits floor, ease it down/up, respectively
			if (NPC.collideY)
			{
				NPC.netUpdate = true;

				if (NPC.velocity.Y > 0f)
				{
					NPC.velocity.Y = Math.Abs(NPC.velocity.Y) * -1f;
					NPC.directionY = -1;
					YMovement = -1f;
				}
				else if (NPC.velocity.Y < 0f)
				{
					NPC.velocity.Y = Math.Abs(NPC.velocity.Y);
					NPC.directionY = 1;
					YMovement = 1f;
				}
			}

			// movement
			if (Resting != 1)
			{
				NPC.velocity.X += NPC.direction * (Main.dayTime ? .06f : .1f);

				if (NPC.velocity.X < (Main.dayTime ? -.8f : 1.1f) || NPC.velocity.X > (Main.dayTime ? .8f : 1.1f))
					NPC.velocity.X *= 0.95f;
			}

			// fish goes up and down, and goes the other way upon reaching a limit
			if (YMovement == -1f)
			{
				NPC.velocity.Y -= 0.01f;
				if (NPC.velocity.Y < -0.3f)
					YMovement = 1f;
			}
			else
			{
				NPC.velocity.Y += 0.01f;
				if (NPC.velocity.Y > 0.3f)
					YMovement = -1f;
			}

			// don't swim too close to bottom tiles
			if (Main.tile[tileX, tileY - 1].LiquidAmount > 128)
			{
				if (Main.tile[tileX, tileY + 1].HasTile)
					YMovement = -1f;
				else if (Main.tile[tileX, tileY + 2].HasTile)
					YMovement = -1f;
			}

			// limits on y velocity
			if (NPC.velocity.Y > 0.4f || NPC.velocity.Y < -0.4f)
				NPC.velocity.Y *= 0.95f;

			// Gar Resting AI
			if (Main.dayTime)
			{
				if (RestTimer > 60 * 20 && RestTimer < 60 * 30)
				{
					if (Main.rand.NextBool(3))
					{
						Resting = 1;
						NPC.netUpdate = true;
					}
					else
						Resting = 0;
				}

				if (RestTimer > 60 * 60)
				{
					RestTimer = 0;
					Resting = 0;
				}
			}
			else
			{
				Resting = 0;
			}

			if (Resting == 1)
			{
				if (Main.rand.NextBool(40))
				{
					float bubbleX = NPC.position.X + (NPC.width / 2) + (NPC.direction == 1 ? NPC.width / 2 + 20 : -NPC.width / 2 - 20);
					float bubbleY = NPC.position.Y + NPC.height / 2 - 4;
					Dust.NewDust(new Vector2(bubbleX, bubbleY), 0, 0, DustID.BreatheBubble, .1f * NPC.direction, Main.rand.NextFloat(-1.14f, -1.48f), 0, new Color(255, 255, 255, 200), Main.rand.NextFloat(.65f, .85f));
				}

				if (NPC.velocity.X != 0)
					NPC.velocity.X *= 0.5f;
			}
			// check for proximity
			if (NPC.DistanceSQ(target.Center) < 40 * 65 && target.wet)
				Proximity = 1;
			else
				Proximity = 0;

			if (Proximity == 1) //Swimming away from player
			{
				Vector2 vel = NPC.DirectionFrom(target.Center) * 2.5f;
				NPC.velocity = vel;
				NPC.rotation = NPC.velocity.X * .04f;
				if (target.position.X > NPC.position.X)
				{
					NPC.spriteDirection = -1;
					NPC.direction = -1;
					NPC.netUpdate = true;
				}
				else if (target.position.X < NPC.position.X)
				{
					NPC.spriteDirection = 1;
					NPC.direction = 1;
					NPC.netUpdate = true;
				}
			}
		}
		else // flopping around
		{
			// falling rotation
			NPC.rotation = NPC.velocity.Y * 0.1f;
			if (NPC.rotation < -0.2f)
				NPC.rotation = -0.2f;

			if (NPC.rotation > 0.2f)
				NPC.rotation = 0.2f;

			// no running away
			Proximity = 0;
			Resting = 0;
			// floppa velocity
			if (NPC.velocity.Y == 0f)
			{
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					NPC.velocity.Y = Main.rand.Next(-50, -20) * 0.1f;
					NPC.velocity.X = Main.rand.Next(-20, 20) * 0.1f;
					NPC.netUpdate = true;
				}
			}

			// fall
			NPC.velocity.Y += 0.3f;
			if (NPC.velocity.Y > 10f)
				NPC.velocity.Y = 10f;
		}

		if (!hasPicked)
		{
			NPC.scale = Main.rand.NextFloat(.9f, 1f);
			pickedType = Main.rand.Next(0, 2);
			hasPicked = true;
		}
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		drawColor = NPC.GetNPCColorTintedByBuffs(drawColor);
		var effects = NPC.direction == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
		spriteBatch.Draw(TextureAssets.Npc[NPC.type].Value, NPC.Center - screenPos + new Vector2(0, NPC.gfxOffY), NPC.frame, drawColor, NPC.rotation, NPC.frame.Size() / 2, NPC.scale, effects, 0);
		return false;
	}

	public override void SendExtraAI(BinaryWriter writer)
	{
		writer.Write(pickedType);
		writer.Write(hasPicked);
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
		pickedType = reader.ReadInt32();
		hasPicked = reader.ReadBoolean();
	}

	public override void FindFrame(int frameHeight)
	{
		if (Resting == 1)
			frameTimer = .1f;
		else
			frameTimer = Math.Abs(.18f * NPC.velocity.X);

		NPC.frameCounter += frameTimer;
		NPC.frameCounter %= Main.npcFrameCount[NPC.type];
		int frame = (int)NPC.frameCounter;
		NPC.frame.Y = frame * frameHeight;
		NPC.frame.X = 80 * pickedType;
		NPC.frame.Width = 80;
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		for (int i = 0; i < 13; i++)
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, 2f * hit.HitDirection, -2f, 0, default, Main.rand.NextFloat(0.75f, 0.95f));

		if (NPC.life <= 0 && Main.netMode != NetmodeID.Server)
		{
			if (pickedType == 0)
			{
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("GarGore1").Type, 1f);
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("GarGore2").Type, Main.rand.NextFloat(.5f, .7f));
			}
			else
			{
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("GarGore3").Type, 1f);
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("GarGore4").Type, Main.rand.NextFloat(.5f, .7f));
			}
		}
	}
	
	public override void ModifyNPCLoot(NPCLoot npcLoot) => npcLoot.AddCommon<RawFish>(3);
	public override float SpawnChance(NPCSpawnInfo spawnInfo) => spawnInfo.Player.InModBiome<SavannaBiome>() && spawnInfo.Water ? (spawnInfo.PlayerInTown ? 0.8f : 0.2f) : 0f;
}