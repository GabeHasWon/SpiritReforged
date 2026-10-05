using SpiritReforged.Content.Crossmod.Spooky.Items;
using SpiritReforged.Content.Ziggurat.Biome;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Ziggurat.NPCs.SpookyMummy;

internal class SpookyMummyLegs : ModNPC
{
	public Player Target => Main.player[NPC.target];

	private ref float Timer => ref NPC.ai[0];
	private ref float MaxTime => ref NPC.ai[1];
	private ref float JumpSpeed => ref NPC.ai[2];
	private ref float JumpStrength => ref NPC.ai[3];

	public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 3;

	public override void SetDefaults()
	{
		NPC.lifeMax = 250;
		NPC.defense = 10;
		NPC.aiStyle = -1;
		NPC.knockBackResist = 0f;
		NPC.noTileCollide = false;
		NPC.noGravity = false;
		NPC.Size = new Vector2(32, 40);
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath6;

		SpawnModBiomes = [ModContent.GetInstance<ZigguratBiome>().Type];
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
	{
		bestiaryEntry.UIInfoProvider = new CommonEnemyUICollectionInfoProvider(ContentSamples.NpcBestiaryCreditIdsByNpcNetIds[Type], true);
		bestiaryEntry.AddInfo(this, "");
	}

	public override void ModifyNPCLoot(NPCLoot npcLoot) => npcLoot.AddCommon<MummyBandageScrap>();

	public override void AI()
	{
		NPC.TargetClosest();

		if (MaxTime == 0)
		{
			MaxTime = Main.rand.NextFloat(55, 70);
			JumpSpeed = Main.rand.NextFloat(0.8f, 1);
			JumpStrength = Main.rand.NextFloat(0.8f, 1.1f);
			NPC.netUpdate = true;
		}

		float strength = JumpSpeed * 6.5f;

		if (Timer >= MaxTime)
		{
			NPC.velocity.X = MathF.Sign(Target.Center.X - NPC.Center.X) * strength;
			NPC.velocity.Y = -6 * JumpStrength;
			Timer = 0;
		}

		if (NPC.collideY)
		{
			NPC.velocity.X *= 0.85f;
			Timer++;
		}
		else
			NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, MathF.Sign(Target.Center.X - NPC.Center.X) * strength, 0.01f);

		if (Math.Abs(NPC.velocity.X) > 0.01f)
			NPC.direction = NPC.spriteDirection = -MathF.Sign(NPC.velocity.X);
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		bool dead = NPC.life <= 0;

		for (int i = 0; i < (dead ? 20 : 6); ++i)
			Dust.NewDust(NPC.position, NPC.width, NPC.height, Main.rand.NextBool(3) ? DustID.Blood : DustID.Sand, Scale: Main.rand.NextFloat(1.2f, 2f));

		if (dead)
		{
			for (int i = 0; i < 2; ++i)
				Gore.NewGore(NPC.GetSource_Death(), NPC.Center, NPC.velocity, ModContent.Find<ModGore>("SpiritReforged/Bandage" + Main.rand.Next(2)).Type);
		}
	}

	public override void FindFrame(int frameHeight)
	{
		NPC.frameCounter++;
		NPC.frame.Y = 0;

		if (NPC.IsABestiaryIconDummy && MaxTime == 0)
			MaxTime = 50;

		if (Timer >= MaxTime - 10)
			NPC.frame.Y = frameHeight;

		if (NPC.velocity.Y != 0)
			NPC.frame.Y = frameHeight * 2;
	}
}
