using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Content.SaltFlats.Biome;
using System.IO;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.SaltFlats.NPCs;

[SpawnPack(3, 4)]
public class Flamingo : ModNPC
{
	private enum State : byte
	{
		IdleReset,
		Walk,
		Lower,
		Muncha,
		Rise,
		Flamingosis
	}

	public int AnimationState
	{
		get => (int)NPC.ai[0];
		set => NPC.ai[0] = value;
	} //What animation is currently being played
	public ref float Counter => ref NPC.ai[1]; //Used to change behaviour at intervals
	public ref float TargetSpeed => ref NPC.ai[2]; //Stores a direction to lerp to over time

	private static readonly int[] endFrames = [4, 8, 6, 12, 5, 5];
	private float _frameRate = 0.2f;
	private bool _pink;

	public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 12; //Rows
	public override void SetDefaults()
	{
		NPC.Size = new Vector2(20, 40);
		NPC.lifeMax = 50;
		NPC.value = 44f;
		NPC.chaseable = false;
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath1;
		NPC.knockBackResist = 0.5f;
		NPC.direction = 1; //Don't start at 0
		AIType = -1;
		SpawnModBiomes = [ModContent.GetInstance<SaltBiome>().Type];
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "");
	public override void OnSpawn(IEntitySource source)
	{
		_pink = Main.rand.NextBool(3);
		NPC.netUpdate = true;
	}

	public override void AI()
	{
		_frameRate = 0.2f; //Default
		NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, TargetSpeed, 0.025f);

		if (AnimationState == (int)State.Flamingosis)
		{
			NPC.velocity.Y = (Counter < 60) ? Math.Max(NPC.velocity.Y - 0.05f, -3f) : NPC.velocity.Y * 0.99f;
			NPC.noGravity = true;

			_frameRate = Math.Min(Math.Abs(NPC.velocity.X) / 5f, 0.2f);
		}
		else
		{
			if (Counter % 250 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
			{
				float oldTargetSpeed = TargetSpeed;
				TargetSpeed = Main.rand.NextFromList(-1, 0, 1) * Main.rand.NextFloat(0.8f, 1.5f);

				if (TargetSpeed != oldTargetSpeed)
					NPC.netUpdate = true;
			}

			var state = (Math.Abs(NPC.velocity.X) > 0.2f) ? State.Walk : State.IdleReset;
			ChangeAnimationState(state);

			if (state is State.Walk)
			{
				Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);
				_frameRate = Math.Min(Math.Abs(NPC.velocity.X) / 5f, 0.2f); //Rate depends on movement speed
			}
		}

		//Set direction
		if (Math.Sign(NPC.velocity.X) is int value && value != 0)
			NPC.direction = NPC.spriteDirection = value;

		Counter++;
	}

	private void ChangeAnimationState(State toState)
	{
		if (AnimationState != (int)toState)
		{
			AnimationState = (int)toState;
			NPC.frameCounter = 0;
			Counter = 0;
		}
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		bool dead = NPC.life <= 0;
		if (!Main.dedServ)
		{
			for (int i = 0; i < (dead ? 20 : 3); i++)
				Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Blood, Scale: Main.rand.NextFloat(0.8f, 2f)).velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f);

			if (dead)
			{
				for (int i = 1; i < 4; i++)
				{
					int type = Mod.Find<ModGore>((_pink ? "FlamingoPink" : "FlamingoRed") + i).Type;
					Gore.NewGore(NPC.GetSource_Death(), Main.rand.NextVector2FromRectangle(NPC.getRect()), NPC.velocity * Main.rand.NextFloat(.3f), type);
				}
			}
		}

		if (!dead)
		{
			NPC.TargetClosest();
			if (NPC.HasPlayerTarget)
			{
				ChangeAnimationState(State.Flamingosis);
				TargetSpeed = ((NPC.Center.X < Main.player[NPC.target].Center.X) ? -1 : 1) * 5;
			}
		}
	}

	public override void FindFrame(int frameHeight)
	{
		bool canLoop = AnimationState != (int)State.IdleReset;

		NPC.frame.Width = 84;
		NPC.frame.X = NPC.frame.Width * AnimationState + (_pink ? 504 : 0);

		NPC.frameCounter += _frameRate;

		if (canLoop)
			NPC.frameCounter %= endFrames[AnimationState];

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
		return false;
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo)
	{
		if (spawnInfo.Invasion || spawnInfo.Water || !spawnInfo.Player.InModBiome<SaltBiome>())
			return 0;

		return 0.2f;
	}

	public override void SendExtraAI(BinaryWriter writer) => writer.Write(_pink);
	public override void ReceiveExtraAI(BinaryReader reader) => _pink = reader.ReadBoolean();
}