using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Savanna.Biome;
using SpiritReforged.Content.Savanna.Tiles;
using SpiritReforged.Content.Vanilla.Food;
using System.Linq;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using static Terraria.Utilities.NPCUtils;

namespace SpiritReforged.Content.Savanna.NPCs.Hyena;

[SpawnPack(2, 3)]
[AutoloadBanner]
[AutoloadGlowmask("255,255,255", false)]
public class Hyena : ModNPC
{
	private enum State : byte
	{
		TrotEnd,
		TrotStart,
		Trotting,
		TrottingAngry,
		BarkingAngry,
		Laugh,
		Walking
	}

	private static Asset<Texture2D> meatItemTexture;
	private static readonly int[] endFrames = [4, 2, 5, 5, 5, 13, 8];

	private bool OnTransitionFrame => (int)NPC.frameCounter >= endFrames[AnimationState]; //Used to determine whether an animation is complete and can be looped or exited

	public int AnimationState { get => (int)NPC.ai[0]; set => NPC.ai[0] = value; } //What animation is currently being played
	public ref float Counter => ref NPC.ai[1]; //Used to change behaviour at intervals
	public ref float TargetSpeed => ref NPC.ai[2]; //Stores a direction to lerp to over time

	public static readonly SoundStyle Laugh = new("SpiritReforged/Assets/SFX/Ambient/Hyena_Laugh")
	{
		Volume = 0.15f,
		PitchVariance = 0.4f,
		MaxInstances = 2
	};

	public static readonly SoundStyle Bark = new("SpiritReforged/Assets/SFX/Ambient/Hyena_Bark")
	{
		Volume = 0.05f,
		PitchVariance = 0.4f
	};

	public static readonly SoundStyle Death = new("SpiritReforged/Assets/SFX/NPCDeath/Hyena_Death")
	{
		Volume = 0.75f,
		Pitch = 0.2f,
		MaxInstances = 0
	};

	public static readonly SoundStyle Hit = new("SpiritReforged/Assets/SFX/NPCHit/Hyena_Hit")
	{
		Volume = 0.75f,
		PitchRange = (-0.45f, -0.35f),
		MaxInstances = 2
	};

	/// <summary> Whether this NPC is holding raw meat. </summary>
	private bool holdingMeat;
	/// <summary> Whether this NPC can deal damage. </summary>
	private bool dealDamage;
	/// <summary> Similar to <see cref="dealDamage"/> but is reset differently. </summary>
	private bool isAngry;
	/// <summary> Tracks the last horizontal jump coordinate so the NPC doesn't constantly jump in the same place. </summary>
	private int oldX;
	/// <summary> Which target types should be focused when searching. </summary>
	private TargetSearchFlag focus = TargetSearchFlag.All;

	public override void SetStaticDefaults()
	{
		NPC.SetNPCTargets(ModContent.NPCType<Ostrich.Ostrich>());
		Main.npcFrameCount[Type] = 13; //Rows

		meatItemTexture = ModContent.Request<Texture2D>(Texture + "_MeatItem");
	}

	public override void SetDefaults()
	{
		NPC.Size = new Vector2(40, 40);
		NPC.damage = 10;
		NPC.defense = 4;
		NPC.lifeMax = 56;
		NPC.value = 44f;
		NPC.chaseable = false;
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath1;
		NPC.knockBackResist = .41f;
		NPC.direction = 1; //Don't start at 0
		AIType = -1;
		SpawnModBiomes = [ModContent.GetInstance<SavannaBiome>().Type];
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "");
	public static SearchFilter<Player> WithinDistanceAndVisible(Vector2 position, float maxDistance) => (Player player) => player.Distance(position) <= maxDistance && !player.invis;

	public override void AI()
	{
		dealDamage = false;

		int searchDist = (focus is TargetSearchFlag.All) ? 350 : 450;
		var search = NPC.FindTarget(focus, WithinDistanceAndVisible(NPC.Center, searchDist), NPCsByDistanceAndType(NPC, searchDist));
		bool wounded = NPC.life < NPC.lifeMax * .25f;

		NPC.CheckDrowning();
		TryJump();

		if (!wounded && Main.bloodMoon) //Permanently hate players during blood moons
		{
			isAngry = true;
			focus = TargetSearchFlag.Players;
		}

		if (wounded)
		{
			if (search.FoundTarget)
				NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, Math.Sign(NPC.Center.X - NPC.GetTargetData().Center.X) * 1.25f, .05f);
			else
				NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, NPC.direction, .05f);

			SetPace();

			if (Main.rand.NextBool(8))
				Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood);
		}
		else if (!search.FoundTarget) //Idle
		{
			focus = TargetSearchFlag.All;
			isAngry = false;
			NPC.chaseable = false;
			NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, TargetSpeed, .025f);

			if (AnimationState == (int)State.Laugh && Math.Abs(NPC.velocity.X) < .1f)
			{
				if (OnTransitionFrame)
				{
					ChangeAnimationState(State.TrotEnd);
					NPC.frameCounter = endFrames[AnimationState];
				}
			}
			else
			{
				/*if (swimming)
				{
					if (drownTime == drownTimeMax / 2)
						TargetSpeed = -TargetSpeed; //Turn around because I've been swimming for too long

					if (TargetSpeed == 0)
						TargetSpeed = NPC.direction * 1.75f;

					ChangeAnimationState(State.Trotting, true);
				}
				else*/
				//{
					if (!FoundPickup() && Counter % 250 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
					{
						float oldTargetSpeed = TargetSpeed;
						TargetSpeed = Main.rand.NextFromList(-1, 0, 1) * Main.rand.NextFloat(.8f, 1.5f);

						if (TargetSpeed != oldTargetSpeed)
							NPC.netUpdate = true;
					}

					SetPace();

					if (!holdingMeat && AnimationState == (int)State.TrotEnd && Main.rand.NextBool(800)) //Randomly laugh when still; not synced
					{
						ChangeAnimationState(State.Laugh);
						SoundEngine.PlaySound(Laugh, NPC.Center);
					}
				//}
			}
		}
		else //Targeting
		{
			const int spotDistance = 16 * 12;

			NPC.chaseable = isAngry && focus.HasFlag(TargetSearchFlag.Players);
			TargetSpeed = 0;
			var target = NPC.GetTargetData();
			Separate();

			if (TryChaseTarget(search))
			{
				if (AnimationState == (int)State.BarkingAngry)
				{
					if (OnTransitionFrame)
						ChangeAnimationState(State.TrottingAngry);
				}
				else
				{
					ChangeAnimationState(State.TrottingAngry, true);

					if (!holdingMeat && Main.rand.NextBool(250)) //Randomly bark; not synced
					{
						ChangeAnimationState(State.BarkingAngry);
						SoundEngine.PlaySound(Bark, NPC.Center);
					}
				}

				dealDamage = true;
			}
			else if (AnimationState == (int)State.Trotting)
			{
				if (Math.Abs(NPC.velocity.X) < .1f)
					ChangeAnimationState(State.TrotEnd);
				else
					ChangeAnimationState(State.Trotting, true);

				NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, Math.Sign(NPC.Center.X - target.Center.X) * 3f, .05f); //Run from the target
			}
			else if (NPC.Distance(target.Center) > spotDistance)
			{
				if (!FoundPickup())
					NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, Math.Sign(target.Center.X - NPC.Center.X) * 1.5f, .05f); //Move toward the target

				ChangeAnimationState(State.Walking, true);
			}
			else
			{
				NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, 0, .1f);

				if (AnimationState == (int)State.Laugh)
				{
					if (OnTransitionFrame)
					{
						ChangeAnimationState(State.TrotEnd);
						NPC.frameCounter = endFrames[AnimationState];
					}
				}
				else
				{
					ChangeAnimationState(State.TrotEnd);

					if (!holdingMeat && !NPC.wet && Main.rand.NextBool(250)) //Randomly laugh; not synced
					{
						ChangeAnimationState(State.Laugh);
						SoundEngine.PlaySound(Laugh, NPC.Center);
					}
				}

				if (target.Velocity.Length() > 3f && NPC.Distance(target.Center) < spotDistance - 16)
				{
					if ((int)NPC.Center.X != oldX)
						ChangeAnimationState(State.Trotting); //Begin to run from the target

					focus = (target.Type is NPCTargetType.NPC) ? TargetSearchFlag.NPCs : TargetSearchFlag.Players; //Strictly remember the nearest target until reset
				}
			}
		}

		if (NPC.velocity.X < 0) //Set direction
			NPC.direction = NPC.spriteDirection = -1;
		else if (NPC.velocity.X > 0)
			NPC.direction = NPC.spriteDirection = 1;

		Counter++;

		void TryJump(float height = 6.5f)
		{
			Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);

			if (NPC.collideX && (NPC.velocity == Vector2.Zero || NPC.wet))
			{
				if ((int)NPC.Center.X == oldX)
					TargetSpeed = -NPC.direction;
				else
				{
					oldX = (int)NPC.Center.X;
					NPC.velocity.Y = -height;
				}
			}
		}

		void Separate(int distance = 32)
		{
			var nearest = Main.npc.OrderBy(x => x.Distance(NPC.Center)).Where(x => x != Main.npc[Main.maxNPCs]
				&& x.active && x.whoAmI != NPC.whoAmI && x.type == Type && x.Distance(NPC.Center) < distance).FirstOrDefault();

			if (nearest != default)
			{
				float update = Math.Sign(NPC.Center.X - nearest.Center.X) * .1f;

				if (Math.Sign(NPC.velocity.X) == Math.Sign(NPC.velocity.X + update)) //Does this require a change in direction?
					NPC.velocity.X += update;
			}
		}

		void SetPace()
		{
			if (Math.Abs(NPC.velocity.X) < .1f)
				ChangeAnimationState(State.TrotEnd);
			else if (Math.Abs(NPC.velocity.X) < 1.9f)
				ChangeAnimationState(State.Walking, true);
			else
				ChangeAnimationState(State.Trotting, true);
		}

		bool FoundPickup() //Scans for a nearby item pickup and does related logic
		{
			const int distance = 16 * 10;

			if (holdingMeat)
				return false;

			foreach (var item in Main.ActiveItems)
			{
				if (item.type == ModContent.ItemType<RawMeat>() && item.Distance(NPC.Center) < distance && Collision.CanHitLine(item.position, item.width, item.height, NPC.position, NPC.width, NPC.height))
				{
					if (NPC.getRect().Intersects(item.getRect()))
					{
						if (--item.stack <= 0)
							item.TurnToAir();

						SoundEngine.PlaySound(SoundID.Grab, NPC.Center);

						TargetSpeed = 0;
						holdingMeat = true;
					}
					else
						TargetSpeed = Math.Sign(item.Center.X - NPC.Center.X) * 1.7f;

					return true;
				}
			}

			return false;
		}
	}

	private bool TryChaseTarget(TargetSearchResults search)
	{
		if (NPC.HasPlayerTarget && (Main.player[NPC.target].statLife < Main.player[NPC.target].statLifeMax2 * .25f || focus is TargetSearchFlag.Players && isAngry))
		{
			NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, Math.Sign(Main.player[NPC.target].Center.X - NPC.Center.X) * 4.8f, .03f); //Chase the player target
			return true;
		}
		else if (search.FoundNPC && (search.NearestNPC.life < search.NearestNPC.lifeMax * .25f || focus is TargetSearchFlag.NPCs && isAngry))
		{
			NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, Math.Sign(search.NearestNPC.Center.X - NPC.Center.X) * 4.8f, .03f); //Chase the npc target
			return true;
		}

		return false;
	}

	private void ChangeAnimationState(State toState, bool loop = false)
	{
		if (OnTransitionFrame && loop)
			NPC.frameCounter = 0;

		if (AnimationState != (int)toState)
		{
			AnimationState = (int)toState;
			NPC.frameCounter = 0;
			Counter = 0;
		}
	}

	public override bool CanBeHitByNPC(NPC attacker) => attacker.friendly && dealDamage || attacker.IsTarget();
	public override bool? CanBeHitByItem(Player player, Item item) => (player.dontHurtCritters && !dealDamage) ? false : null;
	public override bool? CanBeHitByProjectile(Projectile projectile)
	{
		if (projectile.npcProj || projectile.owner == 255)
			return projectile.friendly && dealDamage;

		var p = Main.player[projectile.owner];
		return (p.dontHurtCritters && !dealDamage) ? false : null;
	}

	public override bool CanHitPlayer(Player target, ref int cooldownSlot) => dealDamage;
	public override bool CanHitNPC(NPC target) => (target.IsTarget() || target.friendly) && dealDamage;

	public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
	{
		if ((Main.expertMode || Main.masterMode) && Main.rand.NextBool(5))
			target.AddBuff(BuffID.Rabies, 60 * 20);
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (!Main.dedServ)
		{
			bool dead = NPC.life <= 0;
			for (int i = 0; i < (dead ? 20 : 3); i++)
			{
				Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Blood, Scale: Main.rand.NextFloat(.8f, 2f))
					.velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f);
			}

			if (dead)
			{
				for (int i = 1; i < 4; i++)
					Gore.NewGore(NPC.GetSource_Death(), Main.rand.NextVector2FromRectangle(NPC.getRect()), NPC.velocity * Main.rand.NextFloat(.3f), Mod.Find<ModGore>("Hyena" + i).Type);

				SoundEngine.PlaySound(Death, NPC.Center);
			}

			SoundEngine.PlaySound(Hit, NPC.Center);
		}

		TargetSpeed = hit.HitDirection * 3;
		AggroNearby();
	}

	private void AggroNearby()
	{
		const int aggroRange = 16 * 25;
		const int searchDist = 400;

		var search = NPC.FindTarget(playerFilter: SearchFilters.OnlyPlayersInCertainDistance(NPC.Center, searchDist), npcFilter: NPCsByDistanceAndType(NPC, 500));
		TargetSearchFlag flag;

		if (search.NearestTargetType == TargetType.NPC)
			flag = TargetSearchFlag.NPCs;
		else if (NPC.HasPlayerTarget)
			flag = TargetSearchFlag.Players;
		else
			return;

		foreach (var other in Main.ActiveNPCs)
		{
			if (other.type == Type && other.Distance(NPC.Center) <= aggroRange && other.ModNPC is Hyena hyena)
			{
				hyena.focus = flag;
				hyena.isAngry = true;
			}
		}
	}

	/// <summary> Modified <see cref="AdvancedTargetingHelper.NPCsByDistanceAndType"/> to include friendly NPCs. </summary>
	private static SearchFilter<NPC> NPCsByDistanceAndType(NPC thisNPC, int distance) => delegate (NPC target)
	{
		return target.Distance(thisNPC.Center) <= distance && (target.friendly || AdvancedTargetingHelper.TargetLookup.TryGetValue(ModContent.NPCType<Hyena>(), out int[] targets) && targets.Contains(target.type));
	};

	public override void FindFrame(int frameHeight)
	{
		NPC.frame.Width = 72; //frameHeight = 48
		NPC.frame.X = NPC.frame.Width * AnimationState;

		if (!NPC.wet && NPC.velocity.Y != 0 && NPC.frameCounter == 0)
			NPC.frame.X = NPC.frame.Width * 2; //Jump frame
		else if (AnimationState == (int)State.Walking)
			NPC.frameCounter += Math.Min(Math.Abs(NPC.velocity.X) / 5f, .2f); //Rate depends on movement speed
		else
			NPC.frameCounter += .2f;

		NPC.frame.Y = (int)Math.Min(endFrames[AnimationState] - 1, NPC.frameCounter) * frameHeight;
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		var texture = TextureAssets.Npc[Type].Value;
		var source = NPC.frame with { Width = NPC.frame.Width - 2, Height = NPC.frame.Height - 2 }; //Remove padding
		var position = NPC.Center - screenPos + new Vector2(0, NPC.gfxOffY - (source.Height - NPC.height) / 2 + 2);

		var effects = (NPC.spriteDirection == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
		var color = NPC.GetAlpha(NPC.GetNPCColorTintedByBuffs(drawColor));

		Main.EntitySpriteDraw(texture, position, source, color, NPC.rotation, source.Size() / 2, NPC.scale, effects);

		if (isAngry && Main.bloodMoon)
			Main.EntitySpriteDraw(GlowmaskNPC.NpcIdToGlowmask[Type].Glowmask.Value, position, source, NPC.GetAlpha(Color.Red), NPC.rotation, source.Size() / 2, NPC.scale, effects);

		if (holdingMeat)
			Main.EntitySpriteDraw(meatItemTexture.Value, position, source, NPC.GetAlpha(drawColor), NPC.rotation, source.Size() / 2, NPC.scale, effects);

		return false;
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo)
	{
		if (spawnInfo.Invasion)
			return 0;

		int x = spawnInfo.SpawnTileX;
		int y = spawnInfo.SpawnTileY;
		int wall = Framing.GetTileSafely(x, y).WallType;

		if (spawnInfo.Player.InModBiome<SavannaBiome>() && !spawnInfo.Water && IsValidGround() && wall == WallID.None)
			return (NPC.CountNPCS(Type) > 4) ? .13f : .36f;

		return 0;

		bool IsValidGround()
		{
			int type = Main.tile[x, y].TileType;
			return NPC.IsValidSpawningGroundTile(x, y) && type == ModContent.TileType<SavannaGrass>();
		}
	}

	public override void ModifyNPCLoot(NPCLoot npcLoot)
	{
		npcLoot.AddCommon<RawMeat>(3);
		npcLoot.AddCommon(ItemID.Leather, 2, 3, 5);
	}

	public override void OnKill()
	{
		if (holdingMeat)
		{
			int item = Item.NewItem(NPC.GetSource_Death(), NPC.getRect(), ModContent.ItemType<RawMeat>());

			if (Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendData(MessageID.SyncItem, number: item);
		}
	}
}
