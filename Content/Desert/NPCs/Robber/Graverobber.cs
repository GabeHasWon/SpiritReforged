using SpiritReforged.Common.MathHelpers;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Desert.Biome;
using SpiritReforged.Content.Desert.Tiles;
using System.IO;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Desert.NPCs.Robber;

[AutoloadBanner]
[AutoloadGlowmask("255,255,255", false)]
public class Graverobber : ModNPC
{
	private class DroppedBag(Vector2 position, int direction)
	{
		public Rectangle Hitbox
		{
			get
			{
				const int size = 32;
				return new((int)_position.X - size / 2, (int)_position.Y - size / 2, size, size);
			}
		}

		private readonly int _direction = direction;
		private Vector2 _position = position;
		private Vector2 _velocity;

		public void Update()
		{
			if (CollisionChecks.Tiles(Hitbox, CollisionChecks.SolidOrPlatform))
				_velocity = Vector2.Zero;
			else
				_velocity.Y = Math.Min(_velocity.Y + 0.5f, 4);

			_position += _velocity;
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			int type = ModContent.NPCType<LootBag>();
			Texture2D texture = TextureAssets.Npc[type].Value;
			Rectangle source = texture.Frame(1, Main.npcFrameCount[type], 0, 2, 0, -2);
			SpriteEffects effects = (_direction == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

			spriteBatch.Draw(texture, _position - Main.screenPosition, source, Lighting.GetColor(_position.ToTileCoordinates()), 0, source.Size() / 2, 1, effects, 0);
		}
	}

	public enum State : byte { Idle, Walk, Jump }

	/// <summary> Used to change behaviour at intervals. </summary>
	public ref float Counter => ref NPC.ai[0];
	/// <summary> Stores a direction to lerp to over time. </summary>
	public ref float TargetSpeed => ref NPC.ai[1];
	/// <summary> The visual style of the NPC selected on spawn. </summary>
	public ref float VisualStyle => ref NPC.ai[2];
	/// <summary> The end frame of the NPC animation mod 3. </summary>
	public int EndFrame => endFrames[(int)AnimationState % endFrames.Length];

	public State AnimationState { get; private set; } = State.Idle;

	private static readonly int[] endFrames = [6, 7, 3];
	private bool _alerted;
	private DroppedBag _bag;

	public override void SetStaticDefaults()
	{
		NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers() { Velocity = 1 });
		Main.npcFrameCount[Type] = 7; //Rows
	}

	public override void SetDefaults()
	{
		NPC.Size = new(32);
		NPC.lifeMax = 120;
		NPC.defense = 5;
		NPC.damage = 12;
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath1;
		NPC.knockBackResist = 0.5f;

		AIType = -1;
		SpawnModBiomes = [ModContent.GetInstance<ZigguratBiome>().Type];
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "");

	public override void OnSpawn(IEntitySource source)
	{
		VisualStyle = Main.rand.Next(2);
		NPC.netUpdate = true;
	}

	public override void AI()
	{
		NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, TargetSpeed, 0.1f);

		if (AnimationState is State.Idle)
		{
			if (PlayerInRange(200))
				_alerted = true;

			if (_alerted && NPC.frameCounter >= EndFrame)
			{
				ChangeAnimationState(State.Walk);
				_bag = null;
			}
			else
			{
				int direction = -NPC.direction;
				_bag ??= new(NPC.Center + new Vector2(direction * 40, 0), direction);
				_bag.Update();
			}
		}
		else
		{
			if (PlayerInRange(400))
			{
				Player target = Main.player[NPC.target];
				TargetSpeed = Math.Sign(target.Center.X - NPC.Center.X) * 2f;
			}
			else if (Main.netMode != NetmodeID.MultiplayerClient && Counter % 80 == 0)
			{
				float oldTargetSpeed = TargetSpeed;
				int direction = Main.rand.NextFromList(-1, 0, 1);

				TargetSpeed = direction * Main.rand.NextFloat(1f, 2f);

				if (TargetSpeed != oldTargetSpeed)
					NPC.netUpdate = true;
			}

			ChangeAnimationState((NPC.velocity.Y != 0) ? State.Jump : State.Walk);

			if (AnimationState is State.Walk)
			{
				Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);
			}
			else if (AnimationState is State.Idle && NPC.collideX && Counter % 20 == 0)
			{
				NPC.velocity.Y -= 5; //Jump
			}
		}

		if (Math.Sign(NPC.velocity.X) is int value && value != 0) //Set direction
			NPC.direction = NPC.spriteDirection = value;

		Counter++;
	}

	public void ChangeAnimationState(State toState)
	{
		if (AnimationState != toState)
		{
			NPC.frameCounter = 0;
			Counter = 0;
			AnimationState = toState;
		}
	}

	private bool PlayerInRange(int distance)
	{
		NPC.TargetClosest();
		return NPC.HasPlayerTarget && Main.player[NPC.target].DistanceSQ(NPC.Center) < distance * distance;
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		bool dead = NPC.life <= 0;
		if (!Main.dedServ)
		{
			for (int i = 0; i < (dead ? 20 : 5); i++)
				Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Blood, Scale: Main.rand.NextFloat(0.5f, 1.2f)).velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f);

			if (dead)
			{
				for (int i = 1; i < 6; i++)
					Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Graverobber" + i).Type, 1f);

				//Drop an additional shovel or lantern gore depending on visual style
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Graverobber" + (int)(6 + VisualStyle)).Type, 1f);
			}
		}

		if (dead && Main.netMode != NetmodeID.MultiplayerClient)
		{
			var origin = NPC.Center.ToPoint();
			int whoAmI = NPC.NewNPC(NPC.GetSource_Death(), origin.X, origin.Y, ModContent.NPCType<LootBag>());
			Main.npc[whoAmI].velocity = (Vector2.UnitY * -5).RotatedByRandom(1) + NPC.velocity * 0.5f;

			if (Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendData(MessageID.SyncNPC, number: whoAmI);
		}

		if (!_alerted && AnimationState is State.Idle)
			_alerted = true;
	}

	public override void FindFrame(int frameHeight)
	{
		bool canLoop = AnimationState is State.Walk;
		float frameRate = (AnimationState is State.Walk) ? Math.Min(Math.Abs(NPC.velocity.X) / 5f, 0.2f) : 0.15f; //Rate depends on movement speed

		NPC.frame.Width = 68;
		NPC.frame.X = NPC.frame.Width * ((int)AnimationState + (int)(3 * VisualStyle));
		NPC.frameCounter += frameRate;

		if (canLoop)
			NPC.frameCounter %= endFrames[(int)AnimationState];

		if (!_alerted && AnimationState is State.Idle)
			NPC.frameCounter %= 2; //Mod the animation until alerted
		else if (AnimationState is State.Jump)
			NPC.frameCounter = Math.Min(NPC.frameCounter, (NPC.velocity.Y > 0) ? 2 : 1); //Stagger the animation with logic cues
		else if (AnimationState is State.Walk && Math.Abs(NPC.velocity.X) < 0.1f)
			NPC.frameCounter = 0; //Stop the animation

		NPC.frame.Y = (int)Math.Min(EndFrame - 1, NPC.frameCounter) * frameHeight;
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo) => (spawnInfo.Player.InModBiome<ZigguratBiome>() && spawnInfo.SpawnTileType == ModContent.TileType<RedSandstoneBrick>()) ? 0.1f : 0;

	public override void SendExtraAI(BinaryWriter writer) => writer.Write((byte)AnimationState);
	public override void ReceiveExtraAI(BinaryReader reader)
	{
		int state = reader.ReadByte();
		ChangeAnimationState((State)state);
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		Texture2D texture = TextureAssets.Npc[Type].Value;
		Texture2D glowmask = GlowmaskNPC.NpcIdToGlowmask[Type].Glowmask.Value;

		Rectangle source = NPC.frame with { Width = NPC.frame.Width - 2, Height = NPC.frame.Height - 2 }; //Remove padding
		Vector2 position = NPC.Center - screenPos + new Vector2(0, NPC.gfxOffY - (source.Height - NPC.height) / 2 + 2);
		SpriteEffects effects = (NPC.spriteDirection == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		Main.EntitySpriteDraw(texture, position, source, NPC.DrawColor(drawColor), NPC.rotation, source.Size() / 2, NPC.scale, effects);
		Main.EntitySpriteDraw(glowmask, position, source, NPC.DrawColor(Color.White), NPC.rotation, source.Size() / 2, NPC.scale, effects);

		if (AnimationState is State.Idle)
		{
			if (_alerted)
			{
				source = new(396, 348, 10, 14);
				Main.EntitySpriteDraw(texture, position - new Vector2(0, 10), source, NPC.DrawColor(drawColor), NPC.rotation, source.Size() / 2, NPC.scale, effects);
			}

			_bag?.Draw(spriteBatch);
		}

		return false;
	}
}