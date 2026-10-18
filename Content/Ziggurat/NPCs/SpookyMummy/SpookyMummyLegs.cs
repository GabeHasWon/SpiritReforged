using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Crossmod.Spooky.Items;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Ziggurat.Biome;
using Terraria.Audio;
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
		NPC.damage = 30;
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

	public override void ModifyNPCLoot(NPCLoot npcLoot)
	{
		npcLoot.AddCommon<MummyBandageScrap>();
		SpookyMummyNPC.AddRelic(npcLoot);
	}

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
			for (int i = 0; i < 3; i++)
			{
				Vector2 pos = NPC.direction == -1 ? NPC.BottomRight : NPC.BottomLeft;

				Vector2 velocity = Vector2.UnitX.RotatedByRandom(0.1f) * NPC.direction * Main.rand.NextFloat(3f, 6f) - Vector2.UnitY;

				ParticleRenderers.UnderNPCs.Add(new SmokeCloud(pos, velocity, new Color(160, 155, 106) * 0.22f, Main.rand.NextFloat(0.05f, 0.12f), EaseFunction.EaseQuadOut, 30 + Main.rand.Next(100))
				{
					Pixellate = true,
					PixelDivisor = 2,
				});

				Dust.NewDustPerfect(pos, DustID.Sand, velocity, 150, default, Main.rand.NextFloat(1f, 2.5f)).noGravity = !Main.rand.NextBool(10);
			}

			SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing with { Volume = 0.05f, Pitch = -0.5f }, NPC.Bottom);

			NPC.velocity.X = MathF.Sign(Target.Center.X - NPC.Center.X) * strength;
			NPC.velocity.Y = -6 * JumpStrength;
			Timer = 0;
		}

		if (SpookyMummyNPC.IsGrounded(NPC))
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

		SpookyMummyNPC.SpookyMummyHitEffect(NPC, hit, 0.35f, 0.5f);

		if (dead && !Main.dedServ)
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
