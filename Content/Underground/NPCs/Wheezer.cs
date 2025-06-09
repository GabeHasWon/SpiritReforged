using SpiritReforged.Common.MathHelpers;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Content.Vanilla.Food;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader.Utilities;

namespace SpiritReforged.Content.Underground.NPCs;

[AutoloadBanner]
public class Wheezer : ModNPC
{
	private enum State : byte
	{
		Idle,
		Sleep,
		Hiccup,
		Wake,
		Walk,
		Wheeze
	}

	private static readonly int[] endFrames = [2, 5, 2, 2, 9, 13];

	public ref float Animation => ref NPC.ai[0];
	public ref float LocalCounter => ref NPC.localAI[0];

	private bool OnTransitionFrame => NPC.frameCounter == LastFrame - 1;
	private int LastFrame => endFrames[(int)Animation % endFrames.Length];
	/// <summary> Whether the animation should be reversed. </summary>
	private bool Reverse => (State)Animation is State.Walk && _cooldown > 0;

	/// <summary> Determines the colouration of this NPC. </summary>
	private byte _style;
	/// <summary> The cooldown between attacks. </summary>
	private int _cooldown;
	/// <summary> Whether this NPC is experiencing an expert-exclusive death. </summary>
	private bool _explosiveDeath;
	/// <summary> Tracks for how long this NPC has stayed in the same spot. </summary>
	private int _idleTimer;
	private bool _resetIdle = true;

	public static readonly SoundStyle WheezerHit = new("SpiritReforged/Assets/SFX/NPCHit/WheezerHit")
	{
		Volume = .2f,
		PitchVariance = .4f,
		SoundLimitBehavior = SoundLimitBehavior.IgnoreNew
	};

	public static readonly SoundStyle WheezerAmbient = new("SpiritReforged/Assets/SFX/Ambient/WheezerIdle")
	{
		Volume = .45f,
		SoundLimitBehavior = SoundLimitBehavior.IgnoreNew
	};

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = 13;
		NPCHelper.ImmuneTo(this, BuffID.Poisoned);
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "Caverns");
	public override void SetDefaults()
	{
		NPC.Size = new(30, 36);
		NPC.damage = 25;
		NPC.defense = 8;
		NPC.lifeMax = 80;
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath53;
		NPC.value = 150;
		NPC.knockBackResist = .25f;
		NPC.aiStyle = -1;
	}

	public override void OnSpawn(IEntitySource source)
	{
		_style = (byte)(Main.rand.NextBool(10) ? 3 : Main.rand.Next(0, 3));
		NPC.netUpdate = true;
	}

	public override void AI()
	{
		const int idleDistance = 600;

		if (!Main.dedServ)
		{
			if (Main.rand.NextBool(700))
				SoundEngine.PlaySound(WheezerAmbient, NPC.Center);
		}

		if (_explosiveDeath)
		{
			ExplodeOnDeath();
			return;
		}

		NPC.TargetClosest((State)Animation is State.Walk or State.Idle);
		var target = Main.player[NPC.target];
		bool canHit = target.DistanceSQ(NPC.Center) < idleDistance * idleDistance && CollisionCheckHelper.CanReachFromGround(NPC, target, 50);

		if (!TrySleeping(canHit))
			WalkingBehaviour(canHit);

		NPC.spriteDirection = NPC.direction;
		LocalCounter = ++LocalCounter;
		_cooldown = Math.Max(_cooldown - 1, 0);

		if (_resetIdle)
			_idleTimer = 0;

		_resetIdle = true;
	}

	private void ExplodeOnDeath()
	{
		const int images = 6;

		NPC.velocity.X *= .9f;
		ChangeState(State.Wheeze);

		if (NPC.frameCounter > 3.8f)
		{
			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				int type = ModContent.ProjectileType<WheezerCloud>();

				for (int i = 0; i < images; i++)
				{
					var velocity = Vector2.UnitX.RotatedBy(MathHelper.TwoPi / images * i);

					PreNewProjectile.New(Entity.GetSource_FromAI(), NPC.Center + velocity * 2, velocity * 5f, type, 10, 1, preSpawnAction: delegate (Projectile p)
					{ p.scale *= 1.5f; });
				}

				PreNewProjectile.New(Entity.GetSource_FromAI(), NPC.Center, Vector2.Zero, type, preSpawnAction: delegate (Projectile p)
				{ p.scale *= 2f; });

				NPC.NPCLoot();
			}

			if (!Main.dedServ)
			{
				SoundEngine.PlaySound(SoundID.Item95 with { Pitch = 1f }, NPC.Center);
				SoundEngine.PlaySound(SoundID.NPCDeath63 with { Pitch = 1f }, NPC.Center);

				SpawnGores();
			}

			NPC.active = false;
		}
	}

	private void WalkingBehaviour(bool canHit)
	{
		const int attackDistance = 100;
		const int backOffDistance = 200;

		var target = Main.player[NPC.target];

		if (target.DistanceSQ(NPC.Center) < attackDistance * attackDistance && canHit && _cooldown == 0)
			ChangeState(State.Wheeze);

		if ((State)Animation is State.Wheeze) //Attack
		{
			NPC.velocity.X *= .9f;

			if ((int)NPC.frameCounter == 6)
			{
				SoundEngine.PlaySound(SoundID.Item34 with { PitchRange = (-.35f, .35f), Volume = .5f, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew }, NPC.Center);
				SoundEngine.PlaySound(SoundID.NPCHit36 with { PitchRange = (-.2f, .2f), SoundLimitBehavior = SoundLimitBehavior.IgnoreNew }, NPC.Center);
			} 

			if (Main.netMode != NetmodeID.MultiplayerClient && NPC.frameCounter > 5f && LocalCounter % 6 == 0)
			{
				var vel = (Vector2.UnitX * NPC.direction * Main.rand.NextFloat(3f, 12f)).RotatedByRandom(.5f);
				var source = NPC.Center + new Vector2(24 * NPC.direction, 0);

				Projectile.NewProjectile(Entity.GetSource_FromAI(), source, vel, ModContent.ProjectileType<WheezerCloud>(), 10, 1);
			}

			if (OnTransitionFrame)
			{
				_cooldown = 360;
				ChangeState(State.Walk);
			}
		}
		else
		{
			ChangeState(State.Walk);

			if (_cooldown == 0)
			{
				if (Math.Abs(NPC.velocity.X) < .5f)
				{
					ChangeState(State.Idle);
					NPC.frameCounter = 0;
				}

				int targetX = (Math.Abs(NPC.Center.X - target.Center.X) < 16) ? 0 : NPC.direction;
				NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, targetX, .1f);
			}
			else
			{
				if (NPC.DistanceSQ(target.Center) < backOffDistance * backOffDistance)
				{
					NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, NPC.direction * -0.5f, .1f); //Back off when on cooldown
				}
				else
				{
					NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, 0, .1f); //Stop when far enough away
					ChangeState(State.Idle);
				}
			}

			Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);

			if (!Reverse && (NPC.collideX || FoundGap()) && NPC.velocity.Y == 0)
				NPC.velocity.Y = -5f; //Jump

			if (NPC.collideX)
				MakeIdle();
		}

		bool FoundGap()
		{
			var ahead = Framing.GetTileSafely(NPC.Bottom + new Vector2(16 * NPC.direction, 0));
			return !Collision.SolidCollision(NPC.position + new Vector2(16 * NPC.direction, 16), NPC.width, NPC.height) && !Main.tileSolidTop[ahead.TileType];
		}
	}

	private bool TrySleeping(bool canHit)
	{
		if (!canHit)
			MakeIdle();

		if ((State)Animation is State.Hiccup)
		{
			if (OnTransitionFrame)
			{
				ChangeState(State.Sleep);
				NPC.frameCounter = LastFrame;
			}
		}
		else
		{
			if (_idleTimer > 180 && !canHit)
				ChangeState(State.Sleep);
			else if ((State)Animation is State.Sleep)
				ChangeState(State.Wake);

			if ((State)Animation is State.Wake && OnTransitionFrame)
				ChangeState(State.Walk);

			if ((State)Animation is State.Sleep)
			{
				NPC.velocity.X *= .92f;

				if (LocalCounter % 60 == 0 && Main.rand.NextBool(5))
					ChangeState(State.Hiccup);
			}
		}

		return (State)Animation is State.Sleep or State.Hiccup or State.Wake;
	}

	private void MakeIdle(bool reset = false)
	{
		if (reset)
		{
			_idleTimer = 0;
			return;
		}

		_resetIdle = false;
		_idleTimer++;
	}

	private void ChangeState(State to)
	{
		if ((State)Animation == to)
			return;

		Animation = (int)to;
		NPC.frameCounter = 0;
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (Main.dedServ)
			return;

		for (int k = 0; k < 11; k++)
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, hit.HitDirection, -1f, 0, default, Main.rand.NextFloat(.55f, .95f));

		if (NPC.life <= 0 && !Main.expertMode)
			SpawnGores();

		SoundEngine.PlaySound(WheezerHit, NPC.Center);
	}

	private void SpawnGores()
	{
		string name = _style switch
		{
			1 => "Purple",
			2 => "Teal",
			3 => "Albino",
			_ => "Red"
		};

		for (int i = 1; i < 5; i++)
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Wheezer" + name + i).Type);
	}

	public override bool CheckDead()
	{
		if (Main.expertMode)
		{
			NPC.life = 1;
			NPC.dontTakeDamage = true;
			_explosiveDeath = true;

			NPC.netUpdate = true;
			return false;
		}

		return true;
	}

	//No contact damage
	public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;
	public override bool CanHitNPC(NPC target) => false;

	public override void FindFrame(int frameHeight)
	{
		if (NPC.IsABestiaryIconDummy)
			ChangeState(State.Walk);

		bool canLoop = (State)Animation is State.Walk;

		NPC.frame.Width = 80;
		NPC.frame.X = NPC.frame.Width * (int)Animation + NPC.frame.Width * endFrames.Length * _style;

		NPC.frameCounter += Reverse ? -0.15f : 0.15f;
		if (NPC.frameCounter < 0)
			NPC.frameCounter = LastFrame - 1;

		NPC.frameCounter = canLoop ? NPC.frameCounter % LastFrame : Math.Min(NPC.frameCounter, LastFrame - 1);

		int frame = (int)NPC.frameCounter;
		NPC.frame.Y = frame * frameHeight;

		if ((State)Animation is State.Walk or State.Idle && NPC.velocity.Y != 0) //Jump
		{
			NPC.frame.X = 480 * _style;
			NPC.frame.Y = frameHeight;
		}
	}

	public override void ModifyNPCLoot(NPCLoot npcLoot)
	{
		npcLoot.AddCommon(ModContent.ItemType<Hummus>(), 30);
		npcLoot.Add(ItemDropRule.Common(ItemID.PotatoChips, 35));
		npcLoot.Add(ItemDropRule.Common(ItemID.DepthMeter, 80));
		npcLoot.Add(ItemDropRule.Common(ItemID.Compass, 80));
		npcLoot.Add(ItemDropRule.Common(ItemID.Rally, 20));
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo)
	{
		if (spawnInfo.PlayerSafe || spawnInfo.Player.ZoneSnow)
			return 0f;

		return SpawnCondition.Cavern.Chance * (Main.hardMode ? .03f : .17f);
	}

	public override void SendExtraAI(BinaryWriter writer)
	{
		writer.Write(_style);
		writer.Write(_explosiveDeath);
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
		_style = reader.ReadByte();
		_explosiveDeath = reader.ReadBoolean();
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		var texture = TextureAssets.Npc[Type].Value;
		var origin = new Vector2(NPC.frame.Width / 2 + 8 * -NPC.spriteDirection, NPC.frame.Height - NPC.height / 2 - 4);
		var effects = (NPC.spriteDirection == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
		var pos = NPC.Center - screenPos + new Vector2(0, NPC.gfxOffY);

		Main.EntitySpriteDraw(texture, pos, NPC.frame, NPC.DrawColor(drawColor), NPC.rotation, origin, NPC.scale, effects);
		return false;
	}
}