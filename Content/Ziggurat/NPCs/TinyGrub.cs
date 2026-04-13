using SpiritReforged.Content.Ziggurat.Biome;
using SpiritReforged.Content.Ziggurat.Tiles;
using SpiritReforged.Content.Ziggurat.Walls;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Ziggurat.NPCs;

public class TinyGrub : ModNPC
{
	public override void SetStaticDefaults()
	{
		NPCID.Sets.ShimmerTransformToNPC[Type] = NPCID.Shimmerfly;
		Main.npcFrameCount[Type] = 4;
		NPCID.Sets.CountsAsCritter[Type] = true;
	}

	public override void SetDefaults()
	{
		NPC.width = 20;
		NPC.height = 20;
		NPC.lifeMax = 5;
		NPC.dontCountMe = true;
		NPC.npcSlots = 0;
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath1;
		NPC.value = 0;
		NPC.knockBackResist = 0;
		NPC.noGravity = true;
		NPC.aiStyle = NPCAIStyleID.Butterfly;

		SpawnModBiomes = [ModContent.GetInstance<ZigguratBiome>().Type];
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "");

	public override void AI()
	{
		Point16 tilePos = new((int)(NPC.Center.X / 16), (int)(NPC.Center.Y / 16));

		NPC.scale = 1f;
		NPC.rotation = NPC.velocity.ToRotation() - MathHelper.Pi;
		NPC.velocity *= 0.92f;

		if (Framing.GetTileSafely(tilePos).WallType == WallID.None && Main.netMode != NetmodeID.MultiplayerClient)
			NPC.StrikeInstantKill();
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		for (int k = 0; k < 3; k++)
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.BlueMoss, hit.HitDirection, -1f, 0, default, 1f);

		if (NPC.life <= 0 && !Main.dedServ)
			for (int k = 0; k < 10; k++)
				Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.BlueMoss, hit.HitDirection, -1f, 0, default, 1f);
	}

	public override void FindFrame(int frameHeight)
	{
		if (NPC.velocity != Vector2.Zero)
			NPC.frameCounter += Math.Clamp(NPC.velocity.Length() / 2, 0.1f, 0.2f);
		else
			NPC.frameCounter = 0;

		NPC.frameCounter %= Main.npcFrameCount[NPC.type];
		int frame = (int)NPC.frameCounter;
		NPC.frame.Y = frame * frameHeight;
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo)
	{
		int type = spawnInfo.SpawnTileType;
		return ((type == ModContent.TileType<PaleHive>() || type == ModContent.TileType<GooeyHive>()) && Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY - 1].WallType == PaleHiveWall.UnsafeType) ? 0.62f : 0;
	}
}