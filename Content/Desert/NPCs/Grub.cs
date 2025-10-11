using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using System.IO;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Desert.NPCs;

public class Grub : ModNPC
{
	public enum State : byte
	{
		Idle,
		Jump,
		Crawl
	}

	public ref float Counter => ref NPC.ai[0]; //Used to change behaviour at intervals
	public ref float TargetSpeed => ref NPC.ai[1]; //Stores a direction to lerp to over time

	private static readonly int[] endFrames = [1, 1, 4];

	public State AnimationState = State.Idle;
	private bool _isPouncing;

	public static readonly SoundStyle Death = new("SpiritReforged/Assets/SFX/NPCDeath/BugDeath")
	{
		Volume = 0.75f,
		PitchVariance = 0.2f,
		MaxInstances = 0
	};

	public override void SetStaticDefaults()
	{
		NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers() { Velocity = 1 });
		Main.npcFrameCount[Type] = 4; //Rows
	}

	public override void SetDefaults()
	{
		NPC.Size = new Vector2(16);
		NPC.lifeMax = 30;
		NPC.damage = 8;
		NPC.HitSound = SoundID.NPCHit45;
		NPC.knockBackResist = 1f;
		AIType = -1;
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "");

	public override void AI()
	{
		NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, TargetSpeed, 0.1f);

		if (PlayerInRange(400))
		{
			TargetSpeed = ((Main.player[NPC.target].Center.X > NPC.Center.X) ? 1 : -1) * 2f;
		}
		else if (Main.netMode != NetmodeID.MultiplayerClient && Counter % 80 == 0)
		{
			float oldTargetSpeed = TargetSpeed;
			int direction = Main.rand.NextFromList(-1, 0, 1);

			TargetSpeed = direction * Main.rand.NextFloat(1f, 2f);

			if (TargetSpeed != oldTargetSpeed)
				NPC.netUpdate = true;
		}

		if (_isPouncing)
		{
			TargetSpeed = 0;
			ChangeAnimationState(State.Idle);

			if (Counter > 20)
			{
				_isPouncing = false;

				NPC.velocity.X = ((Main.player[NPC.target].Center.X > NPC.Center.X) ? 1 : -1) * 5f;
				NPC.velocity.Y -= 3;

				SoundEngine.PlaySound(SoundID.Critter with { Pitch = 0.5f, PitchVariance = 0.1f }, NPC.Center);
				ChangeAnimationState(State.Jump);
			}
		}
		else
		{
			var state = Math.Abs(NPC.velocity.X) > 0.2f ? State.Crawl : State.Idle;

			if (NPC.velocity.Y != 0)
				state = State.Jump;

			ChangeAnimationState(state);

			if (state is State.Crawl)
			{
				Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);

				if (NPC.HasPlayerTarget && Counter > 30 && Main.player[NPC.target].DistanceSQ(NPC.Center) < 100 * 100)
					_isPouncing = true;
			}
			else if (state is State.Idle && NPC.collideX && Counter % 20 == 0)
			{
				NPC.velocity.Y -= 5; //Jump
			}
		}

		NPC.rotation = (AnimationState is State.Jump) ? -(NPC.velocity.X * 0.1f) : 0;

		if (Math.Sign(NPC.velocity.X) is int value && value != 0) //Set direction
			NPC.direction = NPC.spriteDirection = value;

		Counter++;
	}

	private void ChangeAnimationState(State toState)
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
		if (!Main.dedServ)
		{
			bool dead = NPC.life <= 0;

			for (int i = 0; i < (dead ? 20 : 5); i++)
				Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.GreenMoss, Scale: Main.rand.NextFloat(0.5f, 1.2f)).velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f);

			if (dead)
			{
				SoundEngine.PlaySound(Death, NPC.Center);

				for (int i = 1; i < 3; i++)
					Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Grub" + i).Type, 1f);
			}
		}
	}

	public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
	{
		if (Main.expertMode)
			target.AddBuff(BuffID.Poisoned, 180);
	}

	public override void FindFrame(int frameHeight)
	{
		bool canLoop = AnimationState == State.Crawl;
		float frameRate = (AnimationState == State.Crawl) ? Math.Min(Math.Abs(NPC.velocity.X) / 5f, 0.3f) : 0.2f; //Rate depends on movement speed

		NPC.frame.Width = 36;
		NPC.frame.X = NPC.frame.Width * (int)AnimationState;
		NPC.frameCounter += frameRate;

		if (canLoop)
			NPC.frameCounter %= endFrames[(int)AnimationState];

		NPC.frame.Y = (int)Math.Min(endFrames[(int)AnimationState] - 1, NPC.frameCounter) * frameHeight;
	}

	public override void SendExtraAI(BinaryWriter writer) => writer.Write((byte)AnimationState);
	public override void ReceiveExtraAI(BinaryReader reader)
	{
		int state = reader.ReadByte();
		ChangeAnimationState((State)state);
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		var texture = TextureAssets.Npc[Type].Value;
		var source = NPC.frame with { Width = NPC.frame.Width - 2, Height = NPC.frame.Height - 2 }; //Remove padding
		var position = NPC.Center - screenPos + new Vector2(0, NPC.gfxOffY - (source.Height - NPC.height) / 2 + 2);
		var effects = NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		Main.EntitySpriteDraw(texture, position, source, NPC.DrawColor(drawColor), NPC.rotation, source.Size() / 2, NPC.scale, effects);
		return false;
	}
}