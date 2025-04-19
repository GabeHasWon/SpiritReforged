using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Underground.NPCs;

[AutoloadBanner]
public class DunceCrab : ModNPC
{
	private enum State : byte
	{
		Crawl,
		Hide,
		UnHide,
		Fall,
		Flail
	}

	private enum Side : byte
	{
		Up,
		Right,
		Down,
		Left
	}

	private static readonly int[] endFrames = [4, 6, 7, 1, 4];

	public ref float Animation => ref NPC.ai[0];
	public ref float Surface => ref NPC.ai[1];

	private float Angle => Surface / 4f * MathHelper.TwoPi;
	private bool OnTransitionFrame => NPC.frameCounter == LastFrame - 1;
	private int LastFrame => endFrames[(int)Animation % endFrames.Length];

	public static readonly SoundStyle ShellHide = new("SpiritReforged/Assets/SFX/Ambient/Jar")
	{
		PitchVariance = .25f,
		SoundLimitBehavior = SoundLimitBehavior.IgnoreNew
	};

	/// <summary> Determines the colouration of this crab. </summary>
	private byte _style;

	public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 7;
	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "Caverns");

	public override void SetDefaults()
	{
		NPC.aiStyle = -1;
		NPC.noGravity = !NPC.IsABestiaryIconDummy; //Ensures the bestiary portrait is visually grounded
		NPC.Size = new Vector2(16); //Hitbox size directly affects tile collision accuracy
		NPC.damage = 20;
		NPC.lifeMax = 50;
		NPC.defense = 8;
		NPC.direction = 1;
		NPC.DeathSound = SoundID.NPCDeath16;
		NPC.HitSound = SoundID.NPCHit33;
		NPC.value = Item.buyPrice(silver: 1, copper: 50);
		NPC.knockBackResist = .5f;
	}

	public override void OnSpawn(IEntitySource source)
	{
		_style = (byte)Main.rand.Next(3);
		NPC.netUpdate = true;
	}

	public override void AI()
	{
		NPC.TargetClosest(false);

		NPC.spriteDirection = NPC.direction;
		NPC.noGravity = false;
		NPC.behindTiles = false;
		NPC.height = 16;

		if ((State)Animation is State.Fall or State.Flail)
		{
			NPC.noGravity = true;
			NPC.behindTiles = true;
			NPC.height = 48; //Extend the hitbox for more convincing falling collision

			FallAndEmbed();
		}
		else if (NPC.wet)
		{
			CrawlInWater();
		}
		else if (Colliding())
		{
			NPC.noGravity = true;
			CrawlAlongTiles();
		}
		else
		{
			NPC.frameCounter = 0;
		}

		bool Colliding()
		{
			const int fluff = 2;
			return Collision.SolidCollision(NPC.position - new Vector2(fluff), NPC.width + fluff * 2, NPC.height + fluff * 2) || NPC.collideX || NPC.collideY;
		}
	}

	private void CrawlInWater()
	{
		TryTurnAround();

		NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, NPC.direction * 1.5f, .09f);

		Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);
		NPC.rotation = Utils.AngleLerp(NPC.rotation, Math.Min(NPC.velocity.X, 3f) * .07f, .15f);
	}

	private void FallAndEmbed()
	{
		if (NPC.collideY || NPC.velocity.Y == 0)
		{
			if ((State)Animation != State.Flail)
			{
				Collision.HitTiles(NPC.position, NPC.velocity, NPC.width, NPC.height);

				SoundEngine.PlaySound(SoundID.NPCHit38 with { Pitch = .5f }, NPC.Center);
				ParticleHandler.SpawnParticle(new SmokeCloud(NPC.Bottom, Vector2.UnitY * -.5f, Color.SandyBrown * .5f, .2f, Common.Easing.EaseFunction.EaseCircularOut, 120));

				for (int i = 0; i < 12; i++)
					Dust.NewDustPerfect(NPC.Bottom, DustID.Dirt, (Vector2.UnitY * -Main.rand.NextFloat(4f)).RotatedByRandom(1f), 150, Scale: Main.rand.NextFloat() + 1).noGravity = true;
			}

			ChangeState(State.Flail);
			NPC.velocity = Vector2.Zero;
		}
		else
		{
			NPC.velocity.X = 0;
			NPC.velocity.Y += 1f;
		}

		NPC.rotation = MathHelper.Pi;
	}

	private void CrawlAlongTiles()
	{
		#region hidey
		var target = Main.player[NPC.target];
		int distanceX = (int)Math.Abs(NPC.Center.X - target.Center.X);

		if ((Side)Surface is Side.Down && target.Center.Y > NPC.Center.Y && distanceX < 16 * 8)
		{
			ChangeState(State.Hide);
			NPC.rotation = MathHelper.Pi;
			NPC.velocity = Vector2.Zero;

			if ((int)NPC.frameCounter == 3)
				SoundEngine.PlaySound(ShellHide, NPC.Center);

			if (distanceX < 16)
			{
				ChangeState(State.Fall);
				NPC.velocity.Y = .5f;
			}

			return;
		}

		if ((State)Animation is State.Hide)
		{
			ChangeState(State.UnHide);
			return;
		}
		else if ((State)Animation is State.UnHide && !OnTransitionFrame)
		{
			return;
		}
		#endregion

		ChangeState(State.Crawl, false);

		if (!NPC.collideX && !NPC.collideY && !IntersectsSlope()) //If not colliding with anything (after crawling over an edge, for example), make a turn
			ResolveSide();

		if (Colliding(true)) //If colliding on the side, make a reverse turn
			ResolveSide(true);

		TryTurnAround();

		float gravity = 2;
		NPC.rotation = Utils.AngleLerp(NPC.rotation, MathHelper.WrapAngle(Angle), .13f);
		NPC.velocity = new Vector2(NPC.direction, gravity).RotatedBy(Angle);

		void ResolveSide(bool reverse = false)
		{
			int rev = reverse ? -1 : 1;
			Surface = (int)(Side)((Surface + 1 * NPC.direction * rev) % 4);
		}

		bool Colliding(bool x)
		{
			if (x) //The x axis
				return ((Side)Surface is Side.Up or Side.Down) ? NPC.collideX : NPC.collideY;
			else //The y axis
				return ((Side)Surface is Side.Up or Side.Down) ? NPC.collideY : NPC.collideX;
		}
	}

	private void TryTurnAround(int time = 10)
	{
		if (NPC.velocity.Length() < .05f) //Turn around
		{
			if (++NPC.localAI[0] > time)
			{
				NPC.direction = -NPC.direction;
				NPC.localAI[0] = 0;
			}
		}
		else
		{
			NPC.localAI[0] = 0;
		}
	}

	private bool IntersectsSlope() //Slopes are a problem.
	{
		var area = NPC.getRect();

		for (int i = 0; i < 4; i++)
		{
			Vector2 pos = i switch
			{
				1 => area.TopRight(),
				2 => area.BottomRight(),
				3 => area.BottomLeft(),
				_ => area.TopLeft()
			};

			if (Framing.GetTileSafely(pos).Slope != SlopeType.Solid)
				return true;
		}

		return false;
	}

	private void ChangeState(State to, bool resetCounter = true)
	{
		int newState = (int)to;

		if (Animation != newState)
		{
			Animation = newState;

			if (resetCounter)
				NPC.frameCounter = 0;
		}
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		if ((Side)Surface is Side.Down && (State)Animation is State.Hide)
		{
			ChangeState(State.Fall);
			NPC.velocity.Y = .5f;
		}

		if (Main.dedServ)
			return;

		int blood = 4;
		if (NPC.life <= 0)
		{
			blood = 15;
			int limit = _style * 3 + 1;

			for (int i = limit; i < limit + 3; i++)
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity * .5f, Mod.Find<ModGore>("Dunce" + i).Type);
		}

		for (int i = 0; i < blood; i++)
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GreenBlood);

		SoundEngine.PlaySound(SoundID.NPCHit1 with { Pitch = .2f }, NPC.Center);
	}

	public override void FindFrame(int frameHeight)
	{
		bool canLoop = (State)Animation is State.Crawl or State.Flail;

		NPC.frame.Width = 38;
		NPC.frame.X = NPC.frame.Width * (int)Animation + NPC.frame.Width * endFrames.Length * _style;

		NPC.frameCounter += 0.15f;
		NPC.frameCounter = canLoop ? NPC.frameCounter % LastFrame : Math.Min(NPC.frameCounter, LastFrame - 1);
		int frame = (int)NPC.frameCounter;

		NPC.frame.Y = frame * frameHeight;
	}

	public override bool ModifyCollisionData(Rectangle victimHitbox, ref int immunityCooldownSlot, ref MultipliableFloat damageMultiplier, ref Rectangle npcHitbox)
	{
		if ((State)Animation is State.Fall)
			damageMultiplier *= 5f; //Deal 5x damage when falling

		return true;
	}

	public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
	{
		if ((State)Animation is State.Flail)
		{
			modifiers.TargetDamageMultiplier *= 2; //Take 2x damage when flailing
			modifiers.Knockback *= 0;
		}
	}

	public override bool CanHitPlayer(Player target, ref int cooldownSlot) => (State)Animation is not State.Flail;
	public override bool CanHitNPC(NPC target) => (State)Animation is not State.Flail;

	public override float SpawnChance(NPCSpawnInfo spawnInfo)
	{
		if (spawnInfo.Invasion || spawnInfo.PlayerInTown)
			return 0;

		int x = spawnInfo.SpawnTileX;
		int y = spawnInfo.SpawnTileY;

		if (y > Main.worldSurface && spawnInfo.Player.ZonePurity && !spawnInfo.Water && NPC.IsValidSpawningGroundTile(x, y))
			return .09f;

		return 0;
	}

	public override int SpawnNPC(int tileX, int tileY)
	{
		int y = tileY;
		while (WorldGen.InWorld(tileX, y, 20) && !WorldGen.SolidOrSlopedTile(tileX, y - 1)) //Attempt to spawn on a ceiling
			y--;

		if (!WorldGen.PlayerLOS(tileX, y))
			tileY = y;

		return NPC.NewNPC(new EntitySource_SpawnNPC(), tileX * 16, tileY * 16, Type);
	}

	public override void ModifyNPCLoot(NPCLoot npcLoot)
	{
		npcLoot.AddCommon(ItemID.PotatoChips, 30);
		npcLoot.AddCommon(ItemID.DepthMeter, 28);
		npcLoot.AddCommon(ItemID.Compass, 32);
		npcLoot.AddCommon(ItemID.Rally, 25);
	}

	public override void SendExtraAI(BinaryWriter writer) => writer.Write(_style);
	public override void ReceiveExtraAI(BinaryReader reader) => _style = reader.ReadByte();

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		var texture = TextureAssets.Npc[Type].Value;
		var origin = new Vector2(NPC.frame.Width / 2, NPC.frame.Height - NPC.height / 2 - 4);
		var effects = (NPC.spriteDirection == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		Main.EntitySpriteDraw(texture, NPC.Center - screenPos + new Vector2(0, NPC.gfxOffY),
			NPC.frame, NPC.GetAlpha(drawColor), NPC.rotation, origin, NPC.scale, effects);

		return false;
	}
}