using SpiritReforged.Content.Ziggurat.Biome;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Ziggurat.NPCs.SpookyMummy;

internal class SpookyMummyHeart : ModNPC
{
	public Player Target => Main.player[NPC.target];

	public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 6;

	public override void SetDefaults()
	{
		NPC.lifeMax = 50;
		NPC.defense = 0;
		NPC.aiStyle = -1;
		NPC.knockBackResist = 1.1f;
		NPC.noTileCollide = false;
		NPC.noGravity = false;
		NPC.Size = new Vector2(32, 32);
		NPC.HitSound = SoundID.NPCHit20;

		SpawnModBiomes = [ModContent.GetInstance<ZigguratBiome>().Type];
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
	{
		bestiaryEntry.UIInfoProvider = new CommonEnemyUICollectionInfoProvider(ContentSamples.NpcBestiaryCreditIdsByNpcNetIds[Type], true);
		bestiaryEntry.AddInfo(this, "");
	}

	public override void AI()
	{
		NPC.TargetClosest();

		NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, MathF.Sign(NPC.Center.X - Target.Center.X) * 2.5f, 0.07f);
		NPC.direction = NPC.spriteDirection = -MathF.Sign(NPC.velocity.X);
		Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);

		if (NPC.velocity.Y == 0)
		{
			if (Collision.SolidCollision(NPC.BottomRight - new Vector2(0, 20), 6, 18))
				NPC.velocity.Y = -4;

			if (Collision.SolidCollision(NPC.BottomLeft - new Vector2(6, 20), 6, 18))
				NPC.velocity.Y = -4;
		}
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		for (int i = 0; i < (NPC.life <= 0 ? 16 : 5); ++i)
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, Scale: Main.rand.NextFloat(0.8f, 1.8f));
	}

	public override void FindFrame(int frameHeight)
	{
		NPC.frameCounter++;
		NPC.frame.Y = frameHeight * (int)(NPC.frameCounter * 0.15f % Main.npcFrameCount[Type]);

		if (NPC.velocity.Y != 0)
			NPC.frame.Y = Math.Sign(NPC.velocity.Y) == -1 ? 0 : frameHeight;
	}
}
