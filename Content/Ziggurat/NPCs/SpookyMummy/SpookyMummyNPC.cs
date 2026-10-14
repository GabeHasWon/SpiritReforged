using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Crossmod.Spooky.Items;
using SpiritReforged.Content.Crossmod.Spooky.Tiles;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Ziggurat.Biome;
using SpiritReforged.Content.Ziggurat.Tiles;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Ziggurat.NPCs.SpookyMummy;

[AutoloadGlowmask("255,255,255")]
internal class SpookyMummyNPC : ModNPC
{
	public class LastMummyCondition : IItemDropRuleCondition
	{
		bool IItemDropRuleCondition.CanDrop(DropAttemptInfo info)
		{
			int count = 0;

			foreach (NPC npc in Main.ActiveNPCs)
			{
				if (npc.type == ModContent.NPCType<SpookyMummyHand>())
					count++;
				else if (npc.type == ModContent.NPCType<SpookyMummyLegs>())
					count++;
				else if (npc.type == ModContent.NPCType<SpookyMummyHead>())
					count++;
			}

			return count == 1;
		}

		bool IItemDropRuleCondition.CanShowItemDropInUI() => true;
		string IProvideItemConditionDescription.GetConditionDescription() => Language.GetTextValue("Mods.SpiritReforged.NPCs.SpookyMummyNPC.DropCondition");
	}
	public Player Target => Main.player[NPC.target];

	private ref float SoundTimer => ref NPC.ai[0];

	internal static bool IsGrounded(NPC npc) => npc.collideY || Collision.SolidCollision(npc.BottomLeft, npc.width, 6, true);

	internal static void AddRelic(NPCLoot loot)
	{
		LeadingConditionRule rule = new(new LastMummyCondition());
		rule.OnSuccess(ItemDropRule.MasterModeCommonDrop(ModContent.ItemType<MummyRelic>()));
		loot.Add(rule);
	}

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
		NPC.friendly = false;
	
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
	
	// Used by the children (besides heart)

	/// <summary>
	/// Helper method to spawn dust for the Spooky Mummy and children
	/// </summary>
	/// <param name="NPC">the NPC</param>
	/// <param name="hit">the <see cref="NPC.HitInfo"/></param>
	/// <param name="scale">the scale multiplier of the dust</param>
	/// <param name="strength">the strength multiplier of loops (iterations) and velocity</param>
	public static void SpookyMummyHitEffect(NPC NPC, NPC.HitInfo hit, float scale = 1f, float strength = 1f)
	{
		if (Main.dedServ)
			return;

		for (int i = 0; i < Math.Max(2 * strength, 1); i++)
		{
			Vector2 hitSpot = NPC.Center + Main.rand.NextVector2Circular(NPC.width / 3, NPC.height / 3);

			for (int x = 0; x < Math.Max(5 * strength, 1); x++)
			{
				Vector2 hitVelocity = Vector2.UnitX.RotatedByRandom(2f) * Main.rand.NextFloat(6f) * hit.HitDirection * strength;

				var d = Dust.NewDustPerfect(hitSpot + Main.rand.NextVector2Circular(20, 20), DustID.Blood,
					hitVelocity, 50 + Main.rand.Next(100), default, Main.rand.NextFloat(2f) * scale);

				d.noGravity = !Main.rand.NextBool(5);
				d.fadeIn = d.noGravity ? 1f : 0f;

				Dust.NewDustPerfect(hitSpot + Main.rand.NextVector2Circular(15, 15), DustID.Sand, hitVelocity.RotatedByRandom(0.15f) * Main.rand.NextFloat(1.1f),
					90, default, Main.rand.NextFloat(1.5f) * scale).noGravity = true;
			}

			hitSpot = NPC.Center + Main.rand.NextVector2Circular(NPC.width / 3, NPC.height / 3);

			// Tight puff of smoke and dust
			if (Main.rand.NextBool(5))
			{
				Vector2 hitVelocity = Vector2.UnitX.RotatedByRandom(1.5f) * Main.rand.NextFloat(4f) * hit.HitDirection * strength;

				for (int x = 0; x < Math.Max(7 * strength, 1); x++)
				{
					if (Main.rand.NextBool(10))
					{
						int type = ModContent.Find<ModGore>("SpiritReforged/Bandage" + Main.rand.Next(2)).Type;
						var gore = Gore.NewGoreDirect(NPC.GetSource_Death(), hitSpot, hitVelocity * Main.rand.NextFloat(0.5f), type);

						gore.sticky = true;
						gore.alpha = 70;
						gore.velocity -= Vector2.UnitY * 2f;
						gore.timeLeft = 60;
					}

					Dust.NewDustPerfect(hitSpot + Main.rand.NextVector2Circular(15, 15), DustID.Smoke, hitVelocity,
										235, default, Main.rand.NextFloat(5f) * scale);

					ParticleHandler.SpawnParticle(new SmokeCloud(hitSpot + Main.rand.NextVector2Circular(5, 5), hitVelocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.33f, 0.75f), new Color(160, 155, 106) * 0.12f, Main.rand.NextFloat(0.05f, 0.2f) * scale, EaseFunction.EaseQuadOut, 30 + Main.rand.Next(100))
					{
						Pixellate = true,
						PixelDivisor = 2,
						Layer = ParticleLayer.BelowNPC
					});
				}
			}
		}
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		SpookyMummyHitEffect(NPC, hit);

		bool dead = NPC.life <= 0;

		if (dead && !Main.dedServ)
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
		NPC.frame.Y = frameHeight * (int)(NPC.frameCounter * 0.10f % Main.npcFrameCount[Type]);
	}
}
