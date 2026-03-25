using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Content.Desert.ScarabBoss.Boss;
using SpiritReforged.Content.Desert.ScarabBoss.Items.ScarabPet;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items;

public class BagOScarabs : BossBagItem
{
	public override void ModifyItemLoot(ItemLoot itemLoot)
	{
		itemLoot.Add(ItemDropRule.OneFromOptions(1, ModContent.ItemType<AdornedBow>(), ModContent.ItemType<SunStaff>(), ModContent.ItemType<RoyalKhopesh>()/*, ModContent.ItemType<LocustCrook>()*/));
		itemLoot.Add(ItemDropRule.FewFromOptions(2, 1, ModContent.ItemType<BedouinCowl>(), ModContent.ItemType<BedouinBreastplate>(), ModContent.ItemType<BedouinLeggings>()));
		itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<SerratedClaws>()));
		itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<ScarabRadio>(), 5));
		itemLoot.Add(ItemDropRule.CoinsBasedOnNPCValue(ModContent.NPCType<Scarabeus>()));
		itemLoot.Add(ItemDropRule.MasterModeCommonDrop(ModContent.ItemType<ScarabLightPetItem>()));
	}
}