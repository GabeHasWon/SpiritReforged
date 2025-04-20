using SpiritReforged.Content.Ocean.Items.JellyCandle;

namespace SpiritReforged.Content.Ocean;

internal class OceanGlobalNPC : GlobalNPC
{
	public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
	{
		//assumedly will add more here
		if (npc.type == NPCID.PinkJellyfish)
			npcLoot.AddCommon(ModContent.ItemType<JellyCandle>(), 50);
	}

	public override void ModifyShop(NPCShop shop)
	{
		if (shop.NpcType == NPCID.Merchant)
		{
			shop.Add(Mod.Find<ModItem>("BeachUmbrellaItem").Type, Condition.InBeach);
			shop.Add(Mod.Find<ModItem>("LoungeChairItem").Type, Condition.InBeach);
		}

		if (shop.NpcType == NPCID.Clothier)
		{
			var mCondition = new Condition(Language.GetText("LegacyMenu.22"), () => Main.LocalPlayer.Male);
			var fCondition = new Condition(Language.GetText("LegacyMenu.23"), () => !Main.LocalPlayer.Male);

			shop.Add<Items.Vanity.TintedGlasses>(Condition.InBeach);
			shop.Add<Items.Vanity.Towel.BeachTowel>(Condition.InBeach, mCondition);
			shop.Add<Items.Vanity.SwimmingTrunks>(Condition.InBeach, mCondition);
			shop.Add<Items.Vanity.BikiniTop>(Condition.InBeach, fCondition);
			shop.Add<Items.Vanity.BikiniBottom>(Condition.InBeach, fCondition);
		}
	}

	public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
	{
		Player player = spawnInfo.Player;

		if (player.ZoneBeach && spawnInfo.Water && !spawnInfo.PlayerInTown && !spawnInfo.Invasion)
			pool.Add(NPCID.Shark, 0.02f);
	}
}