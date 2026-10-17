using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Ziggurat.Biome;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Ziggurat.NPCs.SpookyMummy;

internal class SpookyMummyHeart : ModNPC
{
	public Player Target => Main.player[NPC.target];

	public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 24;

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
		for (int i = 0; i < 8; ++i)
			Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(25, 25), DustID.Blood, Main.rand.NextVector2Circular(2f, 2f), 70, default, Main.rand.NextFloat(2f)).noGravity = true;

		// Dead
		if (NPC.life <= 0)
		{
			for (int i = 0; i < 14; ++i)
				Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(25, 25), DustID.Blood, Main.rand.NextVector2CircularEdge(3f, 3f), 150, default, Main.rand.NextFloat(4f));

			TexturedPulseCircle circle = new(NPC.Center, Color.DarkRed * 0.2f, 2, 90, 20, "Bloom", new Vector2(1), EaseFunction.EaseCircularOut);
			circle.Angle = Main.rand.NextFloat(MathHelper.TwoPi);
			ParticleHandler.SpawnParticle(circle);
		}
	}

	public override void FindFrame(int frameHeight)
	{
		NPC.frameCounter++;
		NPC.frame.Y = frameHeight * (int)(NPC.frameCounter * 0.85f % Main.npcFrameCount[Type]);

		if (NPC.velocity.Y != 0)
			NPC.frame.Y = Math.Sign(NPC.velocity.Y) == -1 ? 0 : frameHeight;
	}
}
