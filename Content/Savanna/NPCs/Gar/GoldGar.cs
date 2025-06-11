using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Content.Vanilla.Food;

namespace SpiritReforged.Content.Savanna.NPCs.Gar;

[AutoloadCritter]
public class GoldGar : Gar
{
	public override void CreateItemDefaults() => ItemEvents.CreateItemDefaults(this.AutoItemType(), item => item.value = Item.sellPrice(gold: 10));

	public override void AI()
	{
		base.AI();

		Lighting.AddLight((int)(NPC.Center.X / 16f), (int)(NPC.Center.Y / 16f), .1f, .1f, .1f);

		if (Main.rand.NextBool(30))
		{
			var dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.GoldCoin);
			dust.velocity *= 0f;
			dust.fadeIn += 0.5f;
		}
	}

	public override void FindFrame(int frameHeight)
	{
		NPC.frameCounter += 0.22f;
		NPC.frameCounter %= Main.npcFrameCount[NPC.type];
		int frame = (int)NPC.frameCounter;
		NPC.frame.Y = frame * frameHeight;
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		for (int i = 0; i < 13; i++)
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Sunflower, 2f * hit.HitDirection, -2f, 0, default, Main.rand.NextFloat(0.75f, 0.95f));

		if (NPC.life <= 0 && Main.netMode != NetmodeID.Server)
		{
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("GarGore5").Type, 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("GarGore6").Type, Main.rand.NextFloat(.5f, .7f));
		}
	}

	public override void ModifyNPCLoot(NPCLoot npcLoot) => npcLoot.AddCommon<RawFish>(2);
	public override float SpawnChance(NPCSpawnInfo spawnInfo) => base.SpawnChance(spawnInfo) * 0.05f;
}