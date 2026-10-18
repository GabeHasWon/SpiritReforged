using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
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

	public const int MAX_DEATH_TIMER = 180;

	public Player Target => Main.player[NPC.target];

	private ref float SoundTimer => ref NPC.ai[0];
	private bool DoingDeathAnimation
	{
		get => NPC.ai[1] == 1;
		set => NPC.ai[1] = value ? 1 : 0;
	}
	private ref float DeathTimer => ref NPC.ai[2];
	private ref float FootstepTimer => ref NPC.ai[3];

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
		NPC.damage = 75;
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
		if (FootstepTimer > 0)
			FootstepTimer--;

		SoundTimer++;

		if (NPC.frame.Y == 112 * 5 && FootstepTimer <= 0)
		{
			if (!Main.dedServ && Main.LocalPlayer.Distance(NPC.Center) < 250)
				ScreenshakeHelper.Shake(NPC.Center, Vector2.UnitX * NPC.direction, 2, 4, 20);

			for (int i = 0; i < 3; i++)
			{
				Vector2 pos = NPC.direction == -1 ? NPC.BottomRight : NPC.BottomLeft;

				ParticleRenderers.UnderNPCs.Add(new SmokeCloud(pos, -Vector2.UnitX.RotatedByRandom(0.1f) * NPC.direction * Main.rand.NextFloat(3f, 6f), new Color(160, 155, 106) * 0.22f, Main.rand.NextFloat(0.05f, 0.12f), EaseFunction.EaseQuadOut, 30 + Main.rand.Next(100))
				{
					Pixellate = true,
					PixelDivisor = 2,
				});

				Dust.NewDustPerfect(pos, DustID.Sand, -Vector2.UnitX.RotatedByRandom(0.1f) * NPC.direction * Main.rand.NextFloat(3f, 12f), 150, default, Main.rand.NextFloat(1f, 2.5f)).noGravity = !Main.rand.NextBool(4);
			}		

			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/GenericClubImpact") with { Volume = 0.05f, Pitch = -0.5f}, NPC.Bottom);
			FootstepTimer = 30;
		}

		if (DoingDeathAnimation)
		{
			DeathTimer--;
			NPC.velocity.X *= 0.985f;

			if (DeathTimer <= 0)
			{
				if (!Main.dedServ && Main.LocalPlayer.Distance(NPC.Center) < 1200)
					ScreenshakeHelper.Shake(NPC.Center, Vector2.UnitX * NPC.direction, 5, 7, 35);

				NPC.life = 0;
				NPC.checkDead();
				NPC.HitEffect();
				NPC.active = false;
			}
		}
		else
		{
			NPC.TargetClosest();
			NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, MathF.Sign(Target.Center.X - NPC.Center.X) * MathHelper.Lerp(3.5f, 2, NPC.life / NPC.lifeMax), 0.02f);

			if (SoundTimer > 300 && Main.rand.NextBool(150))
			{
				SoundEngine.PlaySound(SoundID.Mummy with { Volume = 0.5f, PitchVariance = 0.5f}, NPC.Center);
				SoundTimer = 0;
			}
		}

		NPC.direction = NPC.spriteDirection = -MathF.Sign(NPC.velocity.X);
		Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);
	}

	public override bool CanHitPlayer(Player target, ref int cooldownSlot) => !DoingDeathAnimation;

	public override bool CheckDead()
	{		
		if (!DoingDeathAnimation)
		{
			NPC.life = 1;
			DeathTimer = MAX_DEATH_TIMER;
			NPC.dontTakeDamage = true;
			DoingDeathAnimation = true;

			return false;
		}

		return DoingDeathAnimation && DeathTimer <= 0;
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

					Dust.NewDustPerfect(hitSpot + Main.rand.NextVector2Circular(15, 15), DustID.Smoke, hitVelocity, 235, default, Main.rand.NextFloat(5f) * scale);

					ParticleRenderers.UnderNPCs.Add(new SmokeCloud(hitSpot + Main.rand.NextVector2Circular(5, 5), hitVelocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.33f, 0.75f), new Color(160, 155, 106) * 0.12f, Main.rand.NextFloat(0.05f, 0.2f) * scale, EaseFunction.EaseQuadOut, 30 + Main.rand.Next(100))
					{
						Pixellate = true,
						PixelDivisor = 2,
					});
				}
			}
		}
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		SpookyMummyHitEffect(NPC, hit);

		bool dead = NPC.life <= 0 && DoingDeathAnimation && DeathTimer <= 0;

		if (dead && !Main.dedServ)
		{
			SoundEngine.PlaySound(SoundID.NPCDeath1, NPC.Center);

			Rectangle hitbox = NPC.Hitbox;
			hitbox.Inflate(-16, -16);

			for (int i = 0; i < 6; ++i)
			{
				int type = ModContent.Find<ModGore>("SpiritReforged/Bandage" + Main.rand.Next(2)).Type;
				Gore.NewGore(NPC.GetSource_Death(), Main.rand.NextVector2FromRectangle(hitbox), NPC.velocity, type);

				type = ModContent.Find<ModGore>("SpiritReforged/Bandage" + Main.rand.Next(2)).Type;

				var gore = Gore.NewGoreDirect(NPC.GetSource_Death(), NPC.Center + Main.rand.NextVector2Circular(30, 30), Main.rand.NextVector2CircularEdge(5f, 5f) - Vector2.UnitY * Main.rand.NextFloat(3f), type);

				gore.sticky = true;
				gore.alpha = 70;
				gore.velocity -= Vector2.UnitY * 2f;
				gore.timeLeft = 60;
			}

			for (int i = 0; i < 15; i++)
			{
				ParticleRenderers.OverNPCs.Add(new StickyBloodParticle(NPC.Center + Main.rand.NextVector2Circular(30, 30), Main.rand.NextVector2CircularEdge(5f, 5f)
					- Vector2.UnitY * Main.rand.NextFloat(3f), Main.rand.NextFloat(1.5f), 120 + Main.rand.Next(180), 0.2f));
			}

			hit.HitDirection = -NPC.direction;
			SpookyMummyHitEffect(NPC, hit, 1.2f, 3f);
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
		var n = NPC.NewNPCDirect(NPC.GetSource_Death(), center.X - NPC.spriteDirection * 24, center.Y - 18, ModContent.NPCType<SpookyMummyHead>());

		n.velocity = -Vector2.UnitY.RotatedByRandom(1f) * Main.rand.NextFloat(2f, 5f);

		n = NPC.NewNPCDirect(NPC.GetSource_Death(), center.X, center.Y, ModContent.NPCType<SpookyMummyHeart>());

		n.velocity = -Vector2.UnitY.RotatedByRandom(1f) * 7f;

		for (int i = 0; i < 2; ++i)
		{
			n = NPC.NewNPCDirect(NPC.GetSource_Death(), center.X - NPC.spriteDirection * 12, center.Y + 6, ModContent.NPCType<SpookyMummyHand>());

			n.velocity = -Vector2.UnitY.RotatedByRandom(2f) * Main.rand.NextFloat(3f);

			n = NPC.NewNPCDirect(NPC.GetSource_Death(), center.X - NPC.spriteDirection * (i == 0 ? 20 : 16), center.Y + 20 - i * 6, ModContent.NPCType<SpookyMummyLegs>());

			n.velocity = -Vector2.UnitY.RotatedByRandom(1f) * Main.rand.NextFloat(3f);

		}
	}

	public override void FindFrame(int frameHeight)
	{
		NPC.frameCounter += Math.Abs(NPC.velocity.X) * 0.7f;

		NPC.frame.Y = frameHeight * (int)(NPC.frameCounter * 0.1f % Main.npcFrameCount[Type]);
	}
}
