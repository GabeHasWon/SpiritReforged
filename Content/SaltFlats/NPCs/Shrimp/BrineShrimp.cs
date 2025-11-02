using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Content.SaltFlats.Biome;
using SpiritReforged.Content.Savanna.Biome;
using System.IO;
using System.Runtime.CompilerServices;
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
		NPC.TargetClosest();
		var target = Main.player[NPC.target];

		if (target.DistanceSQ(NPC.Center) < 60 * 60)
			State = BehaviourState.Panic;
	}

	private void DryBehaviour()
	{

	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		drawColor = NPC.GetNPCColorTintedByBuffs(drawColor);
		var effects = NPC.direction == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
		spriteBatch.Draw(TextureAssets.Npc[NPC.type].Value, NPC.Center - screenPos + new Vector2(0, NPC.gfxOffY), NPC.frame, drawColor, NPC.rotation, NPC.frame.Size() / 2, NPC.scale, effects, 0);
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