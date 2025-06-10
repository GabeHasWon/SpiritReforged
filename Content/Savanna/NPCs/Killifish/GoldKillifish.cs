using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Content.Savanna.Biome;
using SpiritReforged.Content.Vanilla.Food;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Savanna.NPCs.Killifish;

[AutoloadCritter]
public class GoldKillifish : Killifish
{
	public override void CreateItemDefaults() => ItemEvents.CreateItemDefaults(this.AutoItemType(), item => item.value = Item.sellPrice(gold: 10));
	public override void OnSpawn(IEntitySource source) { }

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
		NPC.frameCounter += .25f;
		NPC.frameCounter %= Main.npcFrameCount[Type];
		int frame = (int)NPC.frameCounter;
		NPC.frame.Y = frame * frameHeight;
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		for (int i = 0; i < 13; i++)
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Sunflower, 2f * hit.HitDirection, -2f, 0, default, Main.rand.NextFloat(0.75f, 0.95f));

		if (NPC.life <= 0 && !Main.dedServ)
		{
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("KillifishGore5").Type, 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("KillifishGore6").Type, Main.rand.NextFloat(.5f, .7f));
		}
	}

	public override void ModifyNPCLoot(NPCLoot npcLoot) => npcLoot.AddCommon<RawFish>(2);
	public override float SpawnChance(NPCSpawnInfo spawnInfo) => spawnInfo.Player.InModBiome<SavannaBiome>() && spawnInfo.Water ? (spawnInfo.PlayerInTown ? 0.01f : 0.0025f) : 0f;
}