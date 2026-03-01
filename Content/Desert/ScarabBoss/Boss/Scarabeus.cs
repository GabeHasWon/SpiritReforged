using SpiritReforged.Common.Easing;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.Glowmasks;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Desert.ScarabBoss.Boss;

[AutoloadBossHead]
[AutoloadGlowmask("255,255,255", false)]
public partial class Scarabeus : ModNPC
{
	private static VisualProfile PhaseOneProfile;
	private static VisualProfile PhaseTwoProfile;

	public int CurrentState
	{
		get => (int)NPC.ai[0];
		set => NPC.ai[0] = value;
	}

	public int Counter
	{
		get => (int)NPC.ai[1];
		set => NPC.ai[1] = value;
	}

	public Player Target => Main.player[NPC.target];

	/// <summary> Whether the second phase has started. </summary>
	public bool phaseTwo;
	/// <summary> Whether this NPC should deal contact damage. Resets every frame. </summary>
	public bool dealContactDamage = false;

	private Vector2 _dashDirection;
	private bool _escapeJump = false;

	private Action[] _states;

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = 17; //The highest frame count
		NPCID.Sets.TrailCacheLength[Type] = 8;
		NPCID.Sets.TrailingMode[Type] = 3;

		NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers()
		{
			Position = new Vector2(8f, 12f),
			PortraitPositionXOverride = 0f
		});

		PhaseOneProfile = new(TextureAssets.Npc[Type], [7, 8, 16, 8, 8, 8, 6, 17]);
		PhaseTwoProfile = new(DrawHelpers.RequestLocal<Scarabeus>("ScarabeusPhaseTwo", false), [3, 6, 4, 5]);
	}

	public override void SetDefaults()
	{
		_states = [
			SpawnAnimation,
			Walking,
			Skitter,
			HornSwipe,
			Leap,
			RollDash,
			GroundedSlam,
			Dig,
			BounceGroundPound,
			FlyHover,
			FlyingDash,
			ChainGroundPound,
			LeapDig,
			ScarabSwarm
		];

		Profile = PhaseOneProfile;

		NPC.width = 90;
		NPC.height = 90;
		NPC.value = 30000;
		NPC.damage = 40;
		NPC.defense = 10;
		NPC.lifeMax = 2550;
		NPC.aiStyle = -1;
		NPC.boss = true;
		NPC.npcSlots = 15f;
		NPC.HitSound = SoundID.NPCHit31;
		NPC.DeathSound = SoundID.NPCDeath5;
		NPC.dontTakeDamage = true;
		NPC.knockBackResist = 0;

		if (!NPC.IsABestiaryIconDummy)
			NPC.Opacity = 0;

		Music = MusicLoader.GetMusicSlot(Mod, "Assets/Music/Scarabeus");
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "Desert");

	public override bool CheckActive() => Target.active && !Target.dead;

	public override void AI()
	{
		NPC.TargetClosest(false);
		NPC.behindTiles = false;

		dealContactDamage = false;
		showTrail = false;

		if (!phaseTwo && NPC.life < NPC.lifeMax / 2)
		{
			ChangeState(FlyHover);
			Profile = PhaseTwoProfile;
			phaseTwo = true;
		}

		_states[CurrentState].Invoke();
		Counter++;

		if (!NPC.noGravity)
			NPC.Step();
	}

	public override bool CanHitPlayer(Player target, ref int cooldownSlot) => dealContactDamage;

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (Main.dedServ)
			return;

		for (int i = 0; i < 5; i++)
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, hit.HitDirection, -1f, 0, default, 1f);

		if (NPC.life <= 0)
		{
			//SoundEngine.TryGetActiveSound(wingSoundSlot, out ActiveSound sound);

			//if (sound is not null && sound.IsPlaying)
			//{
			//	sound.Stop();
			//wingSoundSlot = SlotId.Invalid;
			//}

			//for (int i = 1; i <= 7; i++)
			//	Gore.NewGoreDirect(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Scarab" + i.ToString()).Type, 1f);

			NPC.position += NPC.Size / 2;
			NPC.Size = new Vector2(100, 60);
			NPC.position -= NPC.Size / 2;

			for (int i = 0; i < 30; i++)
				Dust.NewDustDirect(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, Main.rand.NextFromList(5, 36, 32), 0f, 0f, 100, default, Main.rand.NextBool() ? 2f : 0.5f).velocity *= 3f;

			for (int j = 0; j < 50; j++)
			{
				var dust = Dust.NewDustDirect(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, Main.rand.NextFromList(5, 36, 32), 0f, 0f, 100, default, 1f);
				dust.velocity *= 5f;
				dust.noGravity = true;

				Dust.NewDustDirect(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, Main.rand.NextFromList(5, 36, 32), 0f, 0f, 100, default, .82f).velocity *= 2f;
			}
		}
	}

	public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
	{
		if (projectile.TryGetOwner(out Player player) && !player.ZoneDesert)
			modifiers.FinalDamage /= 3;
	}

	public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
	{
		if (!player.ZoneDesert)
			modifiers.FinalDamage /= 3;
	}

	/*public override bool PreKill()
	{
		if (Main.netMode != NetmodeID.SinglePlayer)
			NetMessage.SendData(MessageID.WorldData);

		//NPC.PlayDeathSound("ScarabDeathSound");
		return true;
	}*/

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

	public override void ModifyHoverBoundingBox(ref Rectangle boundingBox) => boundingBox = NPC.Hitbox;

	public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
	{
		NPC.lifeMax = (int)(NPC.lifeMax * (Main.masterMode ? 0.85f : 1.0f) * 0.7143f * balance);
		NPC.damage = (int)(NPC.damage * 0.626f);
	}

	public void ChangeState(Action state) => ChangeState(Array.IndexOf(_states, state));

	public void ChangeState(int state)
	{
		for (int i = 0; i < 4; i++)
			NPC.ai[i] = 0;

		CurrentState = state;
		NPC.netUpdate = true;

		_dashDirection = default;
		NPC.rotation = 0;
		currentFrame.Y = 0;
	}

	#region helpers
	private Action SelectRandomState()
	{
		List<Action> availablePatterns = [];

		Action[] phase1standard = [Walking, Leap];
		Action[] phase1strong = [Dig, GroundedSlam, RollDash];

		Action[] phase2standard = [FlyHover, FlyingDash];
		Action[] phase2strong = [ChainGroundPound, LeapDig, ScarabSwarm];

		if (phaseTwo)
		{
			availablePatterns.AddRange(phase2standard);
			availablePatterns.AddRange(phase2strong);
		}
		else
		{
			availablePatterns.AddRange(phase1standard);
			availablePatterns.AddRange(phase1strong);
		}

		//Prune the current attack and attacks that shouldn't be used
		List<Action> temp = [];

		for (int i = 0; i < availablePatterns.ToArray().Length; i++)
		{
			if (availablePatterns[i] != _states[CurrentState] && IsStateValid(availablePatterns[i]))
				temp.Add(availablePatterns[i]);
		}

		//Set a random attack from the remainders
		return Main.rand.NextFromCollection(temp);
	}

	/// <summary> Checks if the given attack is viable for random selection, given the current position of the boss and terrain around it </summary>
	private bool IsStateValid(Action state)
	{
		if (state == Walking)
			return CurrentState != Array.IndexOf(_states, Skitter);
		else if (state == Leap)
			return NPC.Distance(Target.Center) > 160;
		else if (state == RollDash)
			return Math.Abs(NPC.Center.Y - Target.Center.Y) < 64 && Math.Abs(NPC.Center.X - Target.Center.X) > 48;
		else if (state == GroundedSlam)
			return Collision.SolidTiles(NPC.BottomLeft, NPC.width / 16, 3, false);
		else if (state == Dig)
			return Collision.SolidTiles(NPC.BottomLeft, NPC.width / 16, 3, false) && CurrentState != Array.IndexOf(_states, BounceGroundPound);
		else if (state == LeapDig)
			return !Collision.SolidTiles(NPC.position, NPC.width, NPC.height);

		return true;
	}

	/// <summary> From a given input, translates the input to the surfacemost tile on the ground. <br/>
	/// If the given input is inside the ground, instead moves upwards until reaching the surface. </summary>
	private static Vector2 FindGroundFromPosition(Vector2 input)
	{
		const int dimensions = 16;

		while (!Collision.SolidTiles(input - new Vector2(dimensions / 2), dimensions, dimensions))
			input.Y += dimensions;

		while (Collision.SolidTiles(input - new Vector2(dimensions / 2), dimensions, dimensions))
			input.Y -= dimensions;

		return input + new Vector2(0, dimensions);
	}

	private void BouncingTileWave(int numTiles, float maxHeight, int totalTime = 60, Vector2? offset = null)
	{
		for (int j = -1; j <= 1; j += 2)
			BouncingTileWave(j, numTiles, maxHeight, totalTime, offset);

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

	private void CheckPlatform()
	{
		bool onplatform = true;
		for (int i = (int)NPC.position.X; i < NPC.position.X + NPC.width; i += NPC.width / 4)
		{ //check tiles beneath the boss to see if they are all platforms
			Tile tile = Framing.GetTileSafely(new Point((int)NPC.position.X / 16, (int)(NPC.position.Y + NPC.height + 8) / 16));
			if (!TileID.Sets.Platforms[tile.TileType])
				onplatform = false;
		}

		if (onplatform && NPC.Center.Y < Target.position.Y - 20) //if they are and the player is lower than the boss, temporarily let the boss ignore tiles to go through them
			NPC.noTileCollide = true;
		else
			NPC.noTileCollide = false;
	}
	#endregion
}