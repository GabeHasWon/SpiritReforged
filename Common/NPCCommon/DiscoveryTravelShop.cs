using SpiritReforged.Content.Forest.ButterflyStaff;
using SpiritReforged.Content.Forest.Safekeeper;
using SpiritReforged.Content.Ocean.Items.Blunderbuss;
using SpiritReforged.Content.Ocean.Items.Pearl;

namespace SpiritReforged.Common.NPCCommon;

internal class DiscoveryTravelShop : GlobalNPC
{
	public override void SetupTravelShop(int[] shop, ref int nextSlot)
	{
		if (!Main.rand.NextBool(2))
			return;

		int[] types = [ModContent.ItemType<Blunderbuss>(), ModContent.ItemType<SafekeeperRing>(), ModContent.ItemType<ButterflyStaff>(), ModContent.ItemType<PearlString>()];

		shop[nextSlot] = types[Main.rand.Next(types.Length)]; //Select one discovery item
		nextSlot++;
	}
}
