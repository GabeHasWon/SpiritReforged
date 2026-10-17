using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Crossmod.Spooky.Items;
using SpiritReforged.Content.Crossmod.Spooky.Tiles;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Ziggurat.Biome;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.Map;

namespace SpiritReforged.Content.Ziggurat.NPCs.SpookyMummy;

[AutoloadGlowmask("255,255,255")]
internal class SpookyMummyHead : ModNPC
{
	public Player Target => Main.player[NPC.target];

	private ref float Timer => ref NPC.ai[0];
	private ref float MaxTime => ref NPC.ai[1];
	private ref float JumpSpeed => ref NPC.ai[2];
	private ref float JumpStrength => ref NPC.ai[3];

	public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 5;

	public override void SetDefaults()
	{
		NPC.lifeMax = 300;
		NPC.defense = 30;
		NPC.damage = 50;
		NPC.aiStyle = -1;
		NPC.knockBackResist = 0f;
		NPC.noTileCollide = false;
		NPC.noGravity = true;
		NPC.Size = new Vector2(42, 54);
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
		npcLoot.AddCommon<MummyBandageScrap>(1, 2, 2);
		SpookyMummyNPC.AddRelic(npcLoot);
	}

	public override void AI()
	{
		NPC.TargetClosest();

		if (MaxTime == 0)
		{
			MaxTime = Main.rand.NextFloat(12, 18);
			JumpSpeed = Main.rand.NextFloat(0.8f, 1);
			JumpStrength = Main.rand.NextFloat(0.8f, 1.1f);
			NPC.netUpdate = true;
		}

		float strength = JumpSpeed * 4.5f;

		if (Timer >= MaxTime)
		{
			for (int i = 0; i < 3; i++)
			{
				Vector2 pos = NPC.Bottom;

				Vector2 velocity = Vector2.UnitX.RotatedByRandom(0.1f) * NPC.direction * Main.rand.NextFloat(3f, 6f) - Vector2.UnitY;

				ParticleHandler.SpawnParticle(new SmokeCloud(pos, velocity, new Color(160, 155, 106) * 0.22f, Main.rand.NextFloat(0.05f, 0.12f), EaseFunction.EaseQuadOut, 30 + Main.rand.Next(100))
				{
					Pixellate = true,
					PixelDivisor = 2,
					Layer = ParticleLayer.BelowNPC
				});

				Dust.NewDustPerfect(pos, DustID.Sand, velocity, 150, default, Main.rand.NextFloat(1f, 2.5f)).noGravity = !Main.rand.NextBool(10);
			}

			var easeFunction = EaseBuilder.EaseCubicOut;
			float ringWidth = 0.6f;
			int lifetime = 40;
			float zRotation = 0.9f;

			ParticleHandler.SpawnParticle(new TexturedPulseCircle(
				NPC.Bottom,
				Color.Gray * 0.66f,
				new Color(160, 155, 106) * 0.66f,
				ringWidth,
				150,
				lifetime,
				"supPerlin",
				new Vector2(2, 3),
				easeFunction).WithSkew(zRotation, MathHelper.PiOver2).UsesLightColor());

			SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing with { Volume = 0.15f, Pitch = -0.5f }, NPC.Bottom);

			NPC.velocity.X = MathF.Sign(Target.Center.X - NPC.Center.X) * strength;
			NPC.velocity.Y = -8 * JumpStrength;
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

		NPC.velocity.Y += 0.5f;
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		bool dead = NPC.life <= 0;

		SpookyMummyNPC.SpookyMummyHitEffect(NPC, hit, 0.75f, 0.75f);

		if (dead && !Main.dedServ)
		{
			for (int i = 0; i < 4; ++i)
				Gore.NewGore(NPC.GetSource_Death(), NPC.Center, NPC.velocity, ModContent.Find<ModGore>("SpiritReforged/Bandage" + Main.rand.Next(2)).Type);
		}
	}

	public override void FindFrame(int frameHeight)
	{
		if (NPC.IsABestiaryIconDummy)
		{
			NPC.frameCounter++;
			NPC.frame.Y = frameHeight * (int)(NPC.frameCounter * 0.15f % Main.npcFrameCount[Type]);
			return;
		}

		if (NPC.velocity.Y == 0)
			NPC.frame.Y = 0;
		else
		{
			if (NPC.velocity.Y < -2.5f)
				NPC.frame.Y = 0;
			else if (NPC.velocity.Y < -1.5f)
				NPC.frame.Y = frameHeight;
			else // if (NPC.velocity.Y < 1f)
				NPC.frame.Y = frameHeight * 2;
			//else if (NPC.velocity.Y < 2.5f)
			//	NPC.frame.Y = frameHeight * 3;
			//else
			//	NPC.frame.Y = frameHeight * 4;
		}
	}
}
