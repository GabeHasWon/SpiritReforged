using SpiritReforged.Content.Desert.ScarabBoss.Boss;
using SpiritReforged.Content.Desert.ScarabBoss.Items.ScarabPet;
using SpiritReforged.Content.Desert.ScarabBoss.Items.Crook;
using Terraria.GameContent.ItemDropRules;
using SpiritReforged.Common.ItemCommon;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items;

public class BagOScarabs : ModItem
{
	public override void SetStaticDefaults()
	{
		ItemID.Sets.BossBag[Type] = true;
		ItemID.Sets.PreHardmodeLikeBossBag[Type] = true;
		ItemID.Sets.OpenableBag[Type] = true;
	}

	public override void SetDefaults() => Item.DefaultToBossBag();

	public override void ModifyItemLoot(ItemLoot itemLoot)
	{
		itemLoot.Add(ItemDropRule.OneFromOptions(1, ModContent.ItemType<AdornedBow>(), ModContent.ItemType<SunStaff>(), ModContent.ItemType<RoyalKhopesh>(), ModContent.ItemType<LocustCrook>()));
		itemLoot.Add(ItemDropRule.FewFromOptions(2, 1, ModContent.ItemType<BedouinCowl>(), ModContent.ItemType<BedouinBreastplate>(), ModContent.ItemType<BedouinLeggings>()));
		itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<SerratedClaws>()));
		itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<ScarabRadio>(), 5));
		itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<SpaceHeater>(), 8));
		itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<IridescentDye>(), 4, 3, 3));
		itemLoot.Add(ItemDropRule.CoinsBasedOnNPCValue(ModContent.NPCType<Scarabeus>()));
		itemLoot.Add(ItemDropRule.MasterModeCommonDrop(ModContent.ItemType<ScarabLightPetItem>()));
		itemLoot.Add(ItemDropRule.Common(ItemID.ScarabBomb, 1, 8, 12));
		itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BeetleLicense>(), 4));
		itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<ScarabMask>(), 7));
	}
}