using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Content.Savanna.Biome;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Savanna.NPCs.Termite;

[AutoloadCritter]
public class Termite : ModNPC
{
	public ref float TermiteTimeLeft => ref NPC.ai[2];
	public bool OnTree { get => NPC.ai[3] == 1; set => NPC.ai[3] = value ? 1 : 0; }

	public override void SetStaticDefaults()
	{
		ItemEvents.CreateItemDefaults(this.AutoItemType(), item =>
		{
			item.value = Item.sellPrice(copper: 95);
			item.bait = 9;
		});

		Main.npcFrameCount[Type] = 3;
		Main.npcCatchable[Type] = true;
		NPCID.Sets.CountsAsCritter[Type] = true;
		NPCID.Sets.ShimmerTransformToNPC[Type] = NPCID.Shimmerfly;
	}

	public override void SetDefaults()
	{
		NPC.width = 16;
		NPC.height = 12;
		NPC.damage = 0;
		NPC.defense = 0;
		NPC.lifeMax = 5;
		NPC.dontCountMe = true;
		NPC.HitSound = SoundID.NPCHit1;
		NPC.knockBackResist = .45f;
		NPC.aiStyle = 66;
		NPC.npcSlots = 0;
		NPC.noGravity = false;
		AIType = NPCID.Grubby;
		NPC.dontTakeDamageFromHostiles = false;
		SpawnModBiomes = [ModContent.GetInstance<SavannaBiome>().Type];
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "");

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (NPC.life <= 0 && Main.netMode != NetmodeID.Server)
		{
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("TermiteGore").Type, NPC.scale);
			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/NPCDeath/BugDeath"), NPC.Center);

			for (int k = 0; k < 5; k++)
				Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Scarecrow, 1.05f * hit.HitDirection, -1.95f, 0, new Color(), 0.6f);
		}
	}

	public override void OnSpawn(IEntitySource source) //Set non-deterministic features on the server/singleplayer then sync
	{
		NPC.scale = Main.rand.NextFloat(.6f, 1f);
		TermiteTimeLeft = Main.rand.Next(255, 510);
		NPC.netUpdate = true;
	}

	public override void AI()
	{
		bool allowedOnTree = TermiteTimeLeft > 30;
		if (OnTree)
		{
			if (FoundTree(0) && allowedOnTree)
			{
				NPC.noGravity = true;
				NPC.velocity = Vector2.Zero;

				var tilePos = (NPC.Center / 16).ToPoint();
				if (Main.rand.NextBool(80))
					WorldGen.KillTile_MakeTileDust(tilePos.X, tilePos.Y, Framing.GetTileSafely(tilePos));

				TermiteTimeLeft = Math.Max(TermiteTimeLeft - 1, 0);
				return;
			}

			if (NPC.velocity.Y == 0)
			{
				OnTree = false;
				NPC.noGravity = false;
			}
		}
		else if (allowedOnTree && NPC.velocity.Y == 0 && FoundTree(2)) //Jump toward the tree
		{
			OnTree = true;
			NPC.velocity = new Vector2(NPC.direction * 2, -Main.rand.NextFloat(2f, 4f));
		}

		if (TermiteTimeLeft - 1 == 0)
			NPC.velocity.Y -= 2f * (NPC.scale * 2f); //Jump into the air before despawning on collision
		else if (TermiteTimeLeft == 0)
		{
			if (NPC.collideY || NPC.collideX)
			{
				SoundEngine.PlaySound(SoundID.Dig with { Volume = .05f }, NPC.Center);
				SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Ambient/BugChitter") with { Volume = 1.14f, PitchVariance = 0.4f }, NPC.Center);

				for (int i = 0; i < 3; i++)
				{
					var dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Dirt);
					dust.velocity = NPC.velocity;
					dust.noGravity = true;
				}

				NPC.active = false;
				NPC.netUpdate = true;
			}
		}

		NPC.spriteDirection = NPC.direction;
		NPC.rotation = (NPC.velocity.Y != 0) ? NPC.velocity.ToRotation() : 0; //Only rotate when airborne

		TermiteTimeLeft = Math.Max(TermiteTimeLeft - 1, 0);
	}

	private bool FoundTree(int offsetX = 0)
	{
		var tilePos = NPC.Center + new Vector2(offsetX * 16, -16);
		var tile = Framing.GetTileSafely(tilePos);

		return TileID.Sets.IsShakeable[tile.TileType];
	}

	public override void FindFrame(int frameHeight)
	{
		if (NPC.velocity != Vector2.Zero || NPC.IsABestiaryIconDummy)
		{
			NPC.frameCounter += 0.12f;
			NPC.frameCounter %= Main.npcFrameCount[Type];
			int frame = (int)NPC.frameCounter;
			NPC.frame.Y = frame * frameHeight;
		}
	}
}
