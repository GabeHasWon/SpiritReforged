using SpiritReforged.Content.Crossmod.Spooky.Items;
using SpiritReforged.Content.Ziggurat.Biome;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Ziggurat.NPCs.SpookyMummy;

internal class SpookyMummyHand : ModNPC
{
	public Player Target => Main.player[NPC.target];

	public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 6;

	public override void SetDefaults()
	{
		NPC.lifeMax = 200;
		NPC.defense = 4;
		NPC.aiStyle = -1;
		NPC.knockBackResist = 0f;
		NPC.noTileCollide = false;
		NPC.noGravity = false;
		NPC.Size = new Vector2(52, 44);
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath6;

		SpawnModBiomes = [ModContent.GetInstance<ZigguratBiome>().Type];
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
	{
		bestiaryEntry.UIInfoProvider =  new CommonEnemyUICollectionInfoProvider(ContentSamples.NpcBestiaryCreditIdsByNpcNetIds[Type], true);
		bestiaryEntry.AddInfo(this, "");
	}

	public override void ModifyNPCLoot(NPCLoot npcLoot) => npcLoot.AddCommon<MummyBandageScrap>();

	public override void AI()
	{
		NPC.TargetClosest();

		NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, MathF.Sign(Target.Center.X - NPC.Center.X) * 4f, 0.07f);
		NPC.direction = NPC.spriteDirection = -MathF.Sign(NPC.velocity.X);
		Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);

		if (NPC.velocity.Y == 0)
		{
			if (Collision.SolidCollision(NPC.TopRight, 6, 12))
				NPC.velocity.Y = -6;

			if (Collision.SolidCollision(NPC.TopLeft - new Vector2(6, 0), 6, 12))
				NPC.velocity.Y = -6;
		}
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		bool dead = NPC.life <= 0;

		for (int i = 0; i < (dead ? 20 : 6); ++i)
			Dust.NewDust(NPC.position, NPC.width, NPC.height, Main.rand.NextBool(3) ? DustID.Blood : DustID.Sand, Scale: Main.rand.NextFloat(1.2f, 2f));

		if (dead && !Main.dedServ)
		{
			for (int i = 0; i < 2; ++i)
				Gore.NewGore(NPC.GetSource_Death(), NPC.Center, NPC.velocity, ModContent.Find<ModGore>("SpiritReforged/Bandage" + Main.rand.Next(2)).Type);
		}
	}

	public override void FindFrame(int frameHeight)
	{
		NPC.frameCounter++;
		NPC.frame.Y = frameHeight * (int)(NPC.frameCounter * 0.15f % Main.npcFrameCount[Type]);

		if (NPC.velocity.Y != 0)
			NPC.frame.Y = frameHeight;
	}
}
