using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Content.SaltFlats.Biome;
using SpiritReforged.Content.Savanna.Biome;
using System.IO;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.SaltFlats.NPCs.Shrimp;

[AutoloadCritter]
public class BrineShrimp : ModNPC, ItemEvents.IQuickRecipeNPC
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

	private ref float IdleTime => ref NPC.ai[2];

	public virtual void AddRecipes() => Recipe.Create(ItemID.CookedShrimp, 1).AddIngredient(this.AutoItemType(), 3).Register();

	public override void SetStaticDefaults()
	{
		CreateItemDefaults();

		Main.npcFrameCount[Type] = 4;
		Main.npcCatchable[Type] = true;

		NPCID.Sets.CountsAsCritter[Type] = true;
		NPCID.Sets.TakesDamageFromHostilesWithoutBeingFriendly[Type] = true;
		NPCID.Sets.ShimmerTransformToNPC[Type] = NPCID.Shimmerfly;
	}

	public virtual void CreateItemDefaults() => ItemEvents.CreateItemDefaults(this.AutoItemType(), item => item.value = Item.sellPrice(0, 0, 5, 37));

	public override void SetDefaults()
	{
		NPC.width = 8;
		NPC.height = 8;
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
		SpawnModBiomes = [ModContent.GetInstance<SaltBiome>().Type];
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
		NPC.target = Player.FindClosest(NPC.position, NPC.width, NPC.height);
		var target = Main.player[NPC.target];

		Timer++;

		if (target.DistanceSQ(NPC.Center) < 120 * 120)
			State = BehaviourState.Panic;

		if (State == BehaviourState.Idle)
		{
			if (NPC.velocity.LengthSquared() > MaxSpeed)
				NPC.velocity *= 0.98f;

			if (Timer > IdleTime)
				State = BehaviourState.Swimming;
		}
		else if (State == BehaviourState.Swimming)
			SwimMovement(false);
		else if (State == BehaviourState.Panic)
		{
			SwimMovement(true);

			if (target.DistanceSQ(NPC.Center) > 120 * 120)
				State = BehaviourState.Swimming;
		}

		NPC.rotation = NPC.velocity.ToRotation() + MathHelper.PiOver2;
	}

	private void SwimMovement(bool beingChased)
	{
		if (NPC.velocity.LengthSquared() > MaxSpeed)
			NPC.velocity *= 0.98f;

		int interval = beingChased ? 15 : 90;
		float swimSpeed = beingChased ? 5 : 3f;

		float adjTimer = Timer % (interval + 1);

		if (NPC.velocity.X == 0)
		{
			NPC.velocity.X = -NPC.oldVelocity.X;

			if (NPC.velocity.X == 0 && Main.netMode != NetmodeID.MultiplayerClient)
			{
				NPC.velocity.X = Main.rand.NextFloat(-0.2f, 0.2f);
				NPC.netUpdate = true;
			}
		}

		if (NPC.velocity.Y == 0)
		{
			NPC.velocity.Y = -NPC.oldVelocity.Y;

			if (NPC.velocity.Y == 0 && Main.netMode != NetmodeID.MultiplayerClient)
			{
				NPC.velocity.Y = Main.rand.NextFloat(-0.2f, 0.2f);
				NPC.netUpdate = true;
			}
		}

		Point tilePos = NPC.Center.ToTileCoordinates();
		tilePos.Y--;
		Tile tile = Main.tile[tilePos];

		// Stop the shrimp from hopping out of the water
		if ((tile.HasTile && Main.tileSolid[tile.TileType] || tile.LiquidAmount < 150) && NPC.velocity.Y < 0)
			NPC.velocity.Y *= 0.80f;

		if (adjTimer == interval && Main.netMode != NetmodeID.MultiplayerClient)
		{
			if (Main.rand.NextBool(3))
			{
				State = BehaviourState.Idle;
				Timer = 0;
			}
			else
			{
				Vector2 dir = beingChased ? Main.player[NPC.target].DirectionTo(NPC.Center) : NPC.velocity;
				NPC.velocity = dir.SafeNormalize(Vector2.Zero).RotatedByRandom(0.7f) * swimSpeed;
			}

			NPC.netUpdate = true;
		}
	}

	private void DryBehaviour()
	{
		if (NPC.velocity.Y == 0)
			NPC.velocity.X *= 0.9f;

		NPC.velocity.Y += 0.2f;

		if (Timer++ > 180 && Main.netMode != NetmodeID.MultiplayerClient)
		{
			NPC.velocity.Y = -2;
			NPC.velocity.X = Main.rand.NextFloat(-2, 2);
			NPC.netUpdate = true;

			Timer = 0;
		}

		NPC.rotation = NPC.velocity.X * 0.08f + MathHelper.PiOver2;
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		drawColor = NPC.GetNPCColorTintedByBuffs(drawColor);
		var effects = NPC.direction == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
		Vector2 pos = NPC.Center - screenPos + new Vector2(0, NPC.gfxOffY);
		spriteBatch.Draw(TextureAssets.Npc[NPC.type].Value, pos, NPC.frame, drawColor, NPC.rotation, NPC.frame.Size() / 2, NPC.scale, effects, 0);
		return false;
	}

	public override void FindFrame(int frameHeight)
	{
		NPC.frameCounter += 0.08f;

		if (State != BehaviourState.Panic)
			NPC.frame.Y = (int)(NPC.frameCounter % 2) * frameHeight + frameHeight * 2;
		else
			NPC.frame.Y = (int)(NPC.frameCounter % 2) * frameHeight;
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
	}
	
	public override float SpawnChance(NPCSpawnInfo spawnInfo) => spawnInfo.Player.InModBiome<SaltBiome>() && spawnInfo.Water ? spawnInfo.PlayerInTown ? 0.8f : 0.2f : 0f;
}