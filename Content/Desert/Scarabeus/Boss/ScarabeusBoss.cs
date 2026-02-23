using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals.Glowmasks;
using System.IO;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Desert.Scarabeus.Boss;

[AutoloadBossHead]
[AutoloadGlowmask("255, 255, 255", false)]
public partial class ScarabeusBoss : ModNPC
{
	public ref float AITimer => ref NPC.ai[0];
	public ref float CurrentPattern => ref NPC.ai[1];

	/// <summary>
	/// Determines if the boss has been stuck in one place when walking for too long.
	/// </summary>
	public ref float DigTimer => ref NPC.ai[2];

	/// <summary>
	/// Determines if the boss is met with a gap
	/// </summary>
	public ref float JumpTimer => ref NPC.ai[3];

	/// <summary>
	/// The current frame of the boss; x = x frame (0 is the first frame, 1 is the second...), y = y frame (same as x), 
	/// z = height of current frame (defaults to -1, which uses the) boss's default frame height.
	/// </summary>
	private Vector3 _curFrame;

	private bool _contactDmgEnabled = false;
	private bool _inGround = true;
	private bool _hasPhaseChanged = false;

	private int _jumpState = 0;
	private int _boredomTimer;
	private bool _escapeJump = false;

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[NPC.type] = 14;
		NPCID.Sets.TrailCacheLength[NPC.type] = 4;
		NPCID.Sets.TrailingMode[NPC.type] = 0;

		var drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers()
		{
			Position = new Vector2(8f, 12f),
			PortraitPositionXOverride = 0f
		};
		NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, drawModifiers);

		PopulateAttackDict();
	}

	public override void SetDefaults()
	{
		NPC.width = 110;
		NPC.height = 110;
		NPC.value = 30000;
		NPC.damage = 40;
		NPC.defense = 10;
		NPC.lifeMax = 2550;
		NPC.aiStyle = -1;
		Music = MusicLoader.GetMusicSlot(Mod, "Assets/Music/Scarabeus");
		NPC.boss = true;
		NPC.npcSlots = 15f;
		NPC.HitSound = SoundID.NPCHit31;
		NPC.DeathSound = SoundID.NPCDeath5;
		NPC.dontTakeDamage = true;
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "Desert");

	public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
	{
		NPC.lifeMax = (int)(NPC.lifeMax * (Main.masterMode ? 0.85f : 1.0f) * 0.7143f * balance);
		NPC.damage = (int)(NPC.damage * 0.626f);
	}

	public override bool CheckActive()
	{
		Player player = Main.player[NPC.target];
		if (!player.active || player.dead)
			return false;

		return true;
	}

	public override void AI()
	{
		NPC.TargetClosest(false);
		Player player = Main.player[NPC.target];
		_contactDmgEnabled = false;
		NPC.behindTiles = true;

		NPCID.Sets.TrailingMode[NPC.type] = 3;

		if (NPC.life < NPC.lifeMax / 2)
		{
			if(!_hasPhaseChanged)
			{
				NextAttack(player, AIPatterns.FlyHover);
				_hasPhaseChanged = true;
			}
		}

		_curFrame.Y = -1;

		PatternSelect(player);
	}

	/// <summary>
	/// From a given input, translates the input to the surfacemost tile on the ground <br/>
	/// If the given input is inside the ground, instead moves upwards until reaching the surface
	/// </summary>
	/// <param name="input"></param>
	/// <returns></returns>
	private static Vector2 FindGroundFromPosition(Vector2 input)
	{
		Point tile = input.ToTileCoordinates();

		while (!Collision.SolidTiles(tile.ToWorldCoordinates(), 1, 1))
		{
			tile.Y += 1;
		}

		while (Collision.SolidTiles(tile.ToWorldCoordinates(), 1, 1))
		{
			tile.Y -= 1;
		}

		tile.Y += 1;

		return tile.ToWorldCoordinates();
	}

	private void BouncingTileWave(int numTiles, float maxHeight, int totalTime = 60, Vector2? offset = null)
	{
		for (int j = -1; j <= 1; j += 2)
		{
			BouncingTileWave(j, numTiles, maxHeight, totalTime, offset);
		}

		ParticleHandler.SpawnParticle(new MovingBlockParticle(FindGroundFromPosition(NPC.Center + (offset ?? Vector2.Zero)), totalTime / 2, maxHeight));
	}

	private void BouncingTileWave(int direction, int numTiles, float maxHeight, int totalTime = 60, Vector2? offset = null)
	{
		for (float i = 0; i < numTiles; i++)
		{
			float height = MathHelper.Lerp(maxHeight, 0, EaseFunction.EaseQuadIn.Ease(i / numTiles));
			int delay = (int)MathHelper.Lerp(0, totalTime / 2, (i + 1) / numTiles);
			ParticleHandler.SpawnQueuedParticle(new MovingBlockParticle(FindGroundFromPosition(NPC.Center + (offset ?? Vector2.Zero) + direction * Vector2.UnitX * 16 * (i + 1)), totalTime / 2, height), delay);
		}
	}

	private void CheckPlatform(Player player)
	{
		bool onplatform = true;
		for (int i = (int)NPC.position.X; i < NPC.position.X + NPC.width; i += NPC.width / 4)
		{ //check tiles beneath the boss to see if they are all platforms
			Tile tile = Framing.GetTileSafely(new Point((int)NPC.position.X / 16, (int)(NPC.position.Y + NPC.height + 8) / 16));
			if (!TileID.Sets.Platforms[tile.TileType])
				onplatform = false;
		}

		if (onplatform && NPC.Center.Y < player.position.Y - 20) //if they are and the player is lower than the boss, temporarily let the boss ignore tiles to go through them
			NPC.noTileCollide = true;
		else
			NPC.noTileCollide = false;
	}

	private void StepUp(Player player)
	{
		bool flag15 = true; //copy pasted collision step code from zombies
		if (player.Center.Y * 16 - 32 > NPC.position.Y)
			flag15 = false;

		if (!flag15 && NPC.velocity.Y == 0f)
			Collision.StepDown(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);

		if (NPC.velocity.Y >= 0f)
			Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY, 1, flag15, 1);
	}

	private void SyncNPC()
	{
		if (Main.netMode != NetmodeID.SinglePlayer)
			NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
	}

	public override bool CanHitPlayer(Player target, ref int cooldownSlot) => _contactDmgEnabled;

	public override void SendExtraAI(BinaryWriter writer)
	{
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
	}

	public override int SpawnNPC(int tileX, int tileY)
	{
		NPC.velocity.Y = 1;
		return base.SpawnNPC(tileX, tileY);
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		for (int k = 0; k < 5; k++)
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, hit.HitDirection, -1f, 0, default, 1f);

		if (NPC.life <= 0 && Main.netMode != NetmodeID.Server)
		{
			//SoundEngine.TryGetActiveSound(wingSoundSlot, out ActiveSound sound);

			//if (sound is not null && sound.IsPlaying)
			//{
			//	sound.Stop();
				//wingSoundSlot = SlotId.Invalid;
			//}

			SpawnGores();
		}
	}

	public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
	{
		modifiers.Knockback *= 0.7f;

		if (!Main.player[projectile.owner].ZoneDesert)
			modifiers.FinalDamage /= 3;
	}

	public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
	{
		if (!player.ZoneDesert)
			modifiers.FinalDamage /= 3;
	}

	public override bool PreKill()
	{
		if (Main.netMode != NetmodeID.SinglePlayer)
			NetMessage.SendData(MessageID.WorldData);

		//NPC.PlayDeathSound("ScarabDeathSound");
		return true;
	}

	public override void ModifyNPCLoot(NPCLoot npcLoot)
	{
		/*npcLoot.AddMasterModeRelicAndPet<ScarabeusRelicItem, ScarabPetItem>();
		npcLoot.AddBossBag<BagOScarabs>();

		var notExpertRule = new LeadingConditionRule(new Conditions.NotExpert());
		notExpertRule.AddCommon<ScarabMask>(7);
		notExpertRule.AddCommon<Trophy1>(10);
		notExpertRule.AddCommon<SandsOfTime>(15);
		notExpertRule.AddCommon<Chitin>(1, 25, 36);
		notExpertRule.AddOneFromOptions<ScarabBow, LocustCrook, RoyalKhopesh, RadiantCane>();

		npcLoot.Add(notExpertRule);*/
	}

	private void SpawnGores()
	{
		for (int i = 1; i <= 7; i++)
			Gore.NewGoreDirect(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Scarab" + i.ToString()).Type, 1f);

		NPC.position += NPC.Size / 2;
		NPC.Size = new Vector2(100, 60);
		NPC.position -= NPC.Size / 2;

		static int randomDustType() => Main.rand.Next(3) switch
		{
			0 => 5,
			1 => 36,
			_ => 32,
		};

		for (int i = 0; i < 30; i++)
			Dust.NewDustDirect(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, randomDustType(), 0f, 0f, 100, default, Main.rand.NextBool() ? 2f : 0.5f).velocity *= 3f;

		for (int j = 0; j < 50; j++)
		{
			var dust = Dust.NewDustDirect(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, randomDustType(), 0f, 0f, 100, default, 1f);
			dust.velocity *= 5f;
			dust.noGravity = true;

			Dust.NewDustDirect(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, randomDustType(), 0f, 0f, 100, default, .82f).velocity *= 2f;
		}
	}
}