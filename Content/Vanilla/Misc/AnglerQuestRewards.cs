using SpiritReforged.Content.Ocean.Items;

namespace SpiritReforged.Content.Vanilla.Misc;

internal class AnglerQuestRewards : ModPlayer
{
	public override void AnglerQuestReward(float quality, List<Item> rewardItems)
	{
		if (Main.rand.NextBool(50))
		{
			var treasure = new Item();
			treasure.SetDefaults(ModContent.ItemType<SunkenTreasure>());
			rewardItems.Add(treasure);
		}

		if (Main.rand.NextBool(15))
		{
			var lure = new Item();
			lure.SetDefaults(ModContent.ItemType<FishLure>());
			lure.stack = Main.rand.Next(1, 3);
			rewardItems.Add(lure);
		}
	}
}