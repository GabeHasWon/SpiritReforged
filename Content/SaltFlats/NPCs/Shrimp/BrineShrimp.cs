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
		NPC.width = 16;
		NPC.height = 16;
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
		const float MaxSpeed = 1.2f * 1.2f;

		NPC.TargetClosest();
		var target = Main.player[NPC.target];

		Timer++;

		if (target.DistanceSQ(NPC.Center) < 60 * 60)
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
			if (NPC.velocity.LengthSquared() > MaxSpeed)
				NPC.velocity *= 0.98f;

			float adjTimer = Timer % 60;

			if (adjTimer == 45)
			{
				if (Direction == Vector2.Zero)
					Direction = new Vector2(3, 0).RotatedByRandom(MathHelper.TwoPi);
				else
					Direction = Direction.RotatedByRandom(0.3f);

				NPC.velocity = Direction;
			}

			if (!Collision.WetCollision(NPC.position + NPC.velocity * 4, NPC.width, NPC.height))
		}
	}

	public static bool WetCollision(Vector2 Position, int Width, int Height)
	{
		Vector2 vector = new Vector2(Position.X + (float)(Width / 2), Position.Y + (float)(Height / 2));

		int num = 10;
		int num2 = Height / 2;
		if (num > Width)
			num = Width;

		if (num2 > Height)
			num2 = Height;

		vector = new Vector2(vector.X - (float)(num / 2), vector.Y - (float)(num2 / 2));
		int value = (int)(Position.X / 16f) - 1;
		int right = (int)((Position.X + (float)Width) / 16f) + 2;
		int y = (int)(Position.Y / 16f) - 1;
		int bottom = (int)((Position.Y + (float)Height) / 16f) + 2;
		int x = Utils.Clamp(value, 0, Main.maxTilesX - 1);
		right = Utils.Clamp(right, 0, Main.maxTilesX - 1);
		y = Utils.Clamp(y, 0, Main.maxTilesY - 1);
		bottom = Utils.Clamp(bottom, 0, Main.maxTilesY - 1);
		Vector2 vector2 = default(Vector2);

		for (int i = x; i < right; i++)
		{
			for (int j = y; j < bottom; j++)
			{
				if (Main.tile[i, j].LiquidAmount == 0)
				{
					vector2.X = i * 16;
					vector2.Y = j * 16;
					int num4 = 16;
					float num5 = 256 / 32f;
					vector2.Y += num5 * 2f;
					num4 -= (int)(num5 * 2f);

					if (vector.X + num > vector2.X && vector.X < vector2.X + 16f && vector.Y + num2 > vector2.Y && vector.Y < vector2.Y + num4)
					{
						return true;
					}
				}
				else
				{
					if (!Main.tile[i, j].HasTile || Main.tile[i, j].Slope == SlopeType.Solid || j <= 0 || Main.tile[i, j - 1] == null || Main.tile[i, j - 1].LiquidAmount <= 0)
						continue;

					vector2.X = i * 16;
					vector2.Y = j * 16;
					int num6 = 16;
					if (vector.X + (float)num > vector2.X && vector.X < vector2.X + 16f && vector.Y + (float)num2 > vector2.Y && vector.Y < vector2.Y + (float)num6)
					{
						if (Main.tile[i, j - 1].honey())
							honey = true;
						else if (Main.tile[i, j - 1].shimmer())
							shimmer = true;

						return true;
					}
				}
			}
		}

		return false;
	}

	private void DryBehaviour()
	{

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