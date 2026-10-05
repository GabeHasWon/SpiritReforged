using SpiritReforged.Content.Crossmod.Spooky.Items;
using SpiritReforged.Content.Ziggurat.Biome;
using SpiritReforged.Content.Ziggurat.Tiles;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Ziggurat.NPCs.SpookyMummy;

internal class SpookyMummyNPC : ModNPC
{
	public Player Target => Main.player[NPC.target];

	private ref float SoundTimer => ref NPC.ai[0];

	public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 10;

	public override void SetDefaults()
	{
		NPC.lifeMax = 1200;
		NPC.defense = 30;
		NPC.aiStyle = -1;
		NPC.knockBackResist = 0f;
		NPC.noTileCollide = false;
		NPC.noGravity = false;
		NPC.Size = new Vector2(90, 102);
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath6;

		SpawnModBiomes = [ModContent.GetInstance<ZigguratBiome>().Type];
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
	{
		bestiaryEntry.UIInfoProvider = new CommonEnemyUICollectionInfoProvider(ContentSamples.NpcBestiaryCreditIdsByNpcNetIds[Type], true);
		bestiaryEntry.AddInfo(this, "");
	}

	public override void AI()
	{
		SoundTimer++;

		if (SoundTimer > 300 && Main.rand.NextBool(150))
		{
			SoundEngine.PlaySound(SoundID.Mummy, NPC.Center);
			SoundTimer = 0;
		}

		NPC.TargetClosest();
		
		NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, MathF.Sign(Target.Center.X - NPC.Center.X) * MathHelper.Lerp(3.5f, 2, NPC.life / NPC.lifeMax), 0.02f);
		NPC.direction = NPC.spriteDirection = -MathF.Sign(NPC.velocity.X);
		Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		bool dead = NPC.life <= 0;

		for (int i = 0; i < (dead ? 30 : 12); ++i)
			Dust.NewDust(NPC.position, NPC.width, NPC.height, Main.rand.NextBool(3) ? DustID.Blood : DustID.Sand, Scale: Main.rand.NextFloat(1.2f, 2f));

		if (dead)
		{
			Rectangle hitbox = NPC.Hitbox;
			hitbox.Inflate(-16, -16);

			for (int i = 0; i < 12; ++i)
			{
				int type = ModContent.Find<ModGore>("SpiritReforged/Bandage" + Main.rand.Next(2)).Type;
				Gore.NewGore(NPC.GetSource_Death(), Main.rand.NextVector2FromRectangle(hitbox), NPC.velocity, type);
			}
		}
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo)
	{
		Tile tile = Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY];
		bool hasItem = spawnInfo.Player.HasItem(ModContent.ItemType<MummyQuestItem>());
		
		if (hasItem && Main.tileSand[spawnInfo.SpawnTileType] && CanSpawn(spawnInfo) && tile.WallType == WallID.None && !NPC.AnyNPCs(Type))
			return 0.1f;

		return 0;
	}

	private static bool CanSpawn(NPCSpawnInfo spawnInfo)
	{
		int x = spawnInfo.SpawnTileX;
		int y = spawnInfo.SpawnTileY;

		for (int i = x - 20; i < x + 20; ++i)
		{
			for (int j = y - 15; j < y + 15; ++j)
			{
				Tile tile = Main.tile[i, j];

				if (tile.HasTile && (tile.TileType == ModContent.TileType<RedSandstoneBrick>() || tile.TileType == ModContent.TileType<RedSandstoneBrickCracked>() ||
					tile.TileType == ModContent.TileType<RedSandstoneSlab>()))
				{
					return true;
				}
			}
		}

		return false;
	}

	public override void OnKill()
	{
		Point center = NPC.Center.ToPoint();
		NPC.NewNPC(NPC.GetSource_Death(), center.X - NPC.spriteDirection * 24, center.Y - 18, ModContent.NPCType<SpookyMummyHead>());
		NPC.NewNPC(NPC.GetSource_Death(), center.X, center.Y, ModContent.NPCType<SpookyMummyHeart>());

		for (int i = 0; i < 2; ++i)
		{
			NPC.NewNPC(NPC.GetSource_Death(), center.X - NPC.spriteDirection * 12, center.Y + 6, ModContent.NPCType<SpookyMummyHand>());
			NPC.NewNPC(NPC.GetSource_Death(), center.X - NPC.spriteDirection * (i == 0 ? 20 : 16), center.Y + 20 - i * 6, ModContent.NPCType<SpookyMummyLegs>());
		}
	}

	public override void FindFrame(int frameHeight)
	{
		NPC.frameCounter++;
		NPC.frame.Y = frameHeight * (int)(NPC.frameCounter * 0.08f % Main.npcFrameCount[Type]);
	}
}
