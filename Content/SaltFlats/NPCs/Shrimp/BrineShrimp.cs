using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.WorldGeneration.Micropasses.Passes;
using SpiritReforged.Content.SaltFlats.Biome;
using SpiritReforged.Content.Savanna.Biome;
using System.IO;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.SaltFlats.NPCs.Shrimp;

[AutoloadCritter]
public class BrineShrimp : ModNPC
{
	private enum BehaviourState : byte
	{
		Idle,
		Swimming,
		Panic
	}

	const float MaxSpeed = 1.2f * 1.2f;

	private BehaviourState State
	{
		get => (BehaviourState)NPC.ai[0];
		set => NPC.ai[0] = (float)value;
	}

	private ref float Timer => ref NPC.ai[1];

	private Vector2 Direction
	{
		get => new(NPC.ai[2], NPC.ai[3]);
		set => (NPC.ai[2], NPC.ai[3]) = (value.X, value.Y);
	}

	public override void SetStaticDefaults()
	{
		CreateItemDefaults();

		Main.npcFrameCount[Type] = 12;
		Main.npcCatchable[Type] = true;

		NPCID.Sets.CountsAsCritter[Type] = true;
		NPCID.Sets.TakesDamageFromHostilesWithoutBeingFriendly[Type] = true;
		NPCID.Sets.ShimmerTransformToNPC[Type] = NPCID.Shimmerfly;
	}

	public virtual void CreateItemDefaults() => ItemEvents.CreateItemDefaults(this.AutoItemType(), item => item.value = Item.sellPrice(0, 0, 5, 37));

	public override void SetDefaults()
	{
		NPC.width = 12;
		NPC.height = 12;
		NPC.damage = 0;
		NPC.defense = 0;
		NPC.lifeMax = 5;
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath1;
		NPC.knockBackResist = .35f;
		NPC.aiStyle = -1;
		NPC.noGravity = true;
		NPC.npcSlots = 0;
		NPC.dontCountMe = true;
		SpawnModBiomes = [ModContent.GetInstance<SavannaBiome>().Type];
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "");

	public override void AI()
	{
		if (NPC.wet)
			WaterBehaviour();
		else
			DryBehaviour();
	}

	private void WaterBehaviour()
	{
		NPC.TargetClosest();
		var target = Main.player[NPC.target];

		Timer++;

		if (target.DistanceSQ(NPC.Center) < 120 * 120)
			State = BehaviourState.Panic;

		if (State == BehaviourState.Idle)
		{
			if (NPC.velocity.LengthSquared() > MaxSpeed)
				NPC.velocity *= 0.98f;

			if (Timer > 120)
				State = BehaviourState.Swimming;
		}
		else if (State == BehaviourState.Swimming)
		{
			Direction = Vector2.Normalize(Direction) * 3;
			SwimMovement();
		}
		else if (State == BehaviourState.Panic)
		{
			Direction = NPC.DirectionFrom(target.Center) * 4;
			SwimMovement(15);
		}
	}

	private void SwimMovement(int interval = 60)
	{
		if (NPC.velocity.LengthSquared() > MaxSpeed)
			NPC.velocity *= 0.98f;

		float adjTimer = Timer % 60;

		if (NPC.velocity.X == 0)
			Direction = new Vector2(Direction.X * -1, Direction.Y);

		if (NPC.velocity.Y == 0)
			Direction = new Vector2(Direction.X, Direction.Y * -1);

		// this hit detection needs rewrite, will do tomorrow - gabe
		if (adjTimer == interval)
		{
			if (Direction == Vector2.Zero)
				Direction = new Vector2(3, 0).RotatedByRandom(MathHelper.TwoPi);
			else
				Direction = Direction.RotatedByRandom(0.3f);

			NPC.velocity = Direction;
		}

		Vector2 futurePos = NPC.position + Direction * 4;

		if (Collision.SolidCollision(futurePos, NPC.width, NPC.height))
		{
			Direction *= -1;
			NPC.velocity = Direction;
			NPC.position += Direction;
		}
	}

	private void DryBehaviour()
	{
		if (NPC.velocity.Y == 0)
			NPC.velocity.X *= 0.9f;

		NPC.velocity.Y += 0.2f;
		Direction = new Vector2(0, 3).RotatedByRandom(0.2f);
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		drawColor = NPC.GetNPCColorTintedByBuffs(drawColor);
		var effects = NPC.direction == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
		Vector2 pos = NPC.Center - screenPos + new Vector2(0, NPC.gfxOffY);
		spriteBatch.Draw(TextureAssets.Npc[NPC.type].Value, pos, NPC.frame, drawColor, NPC.rotation, NPC.frame.Size() / 2, NPC.scale, effects, 0);
		return false;
	}


	public override void SendExtraAI(BinaryWriter writer)
	{
		writer.Write(NPC.scale);
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
		NPC.scale = reader.ReadSingle();
	}

	public override void FindFrame(int frameHeight)
	{
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (Main.dedServ)
			return;

		for (int i = 0; i < 3; i++)
		{
			short type = Main.rand.NextBool() ? DustID.PinkSlime : DustID.Blood;
			Dust.NewDust(NPC.position, NPC.width, NPC.height, type, 2f * hit.HitDirection, -2f, 0, default, Main.rand.NextFloat(0.75f, 0.95f));
		}

		if (NPC.life <= 0)
		{
		}
	}
	
	public override float SpawnChance(NPCSpawnInfo spawnInfo) => spawnInfo.Player.InModBiome<SaltBiome>() && spawnInfo.Water ? spawnInfo.PlayerInTown ? 0.8f : 0.2f : 0f;
}