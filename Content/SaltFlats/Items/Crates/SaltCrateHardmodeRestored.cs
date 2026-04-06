using Terraria.GameContent.ItemDropRules;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using SpiritReforged.Content.Savanna.Tiles;

namespace SpiritReforged.Content.SaltFlats.Items.Crates;

public class SaltCrateHardmodeRestored : ModItem
{
	public class SaltCrateHardmodeRestoredTile : SaltCrateHardmode.SaltCrateHardmodeTile;

	public override void SetStaticDefaults()
	{
		ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<SaltCrate>();
		Item.ResearchUnlockCount = 10;
	}
	
	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<SaltCrateHardmodeRestoredTile>());
		Item.rare = ItemRarityID.Green;
		Item.value = Item.sellPrice(0, 1, 0, 0);
	}

	public override bool CanRightClick() => true;

	public override void ModifyItemLoot(ItemLoot itemLoot)
	{
		int[] dropOptions = [ModContent.ItemType<MahakalaMaskBlue>(),
			ModContent.ItemType<MahakalaMaskRed>(),
			ModContent.ItemType<BoStaff>(),
			ItemID.CloudinaBottle,
			ItemID.WaterWalkingBoots];

		IItemDropRule main = ItemDropRule.OneFromOptions(1, dropOptions);

		itemLoot.AddCommon(ItemID.LawnFlamingo, 5);

		CrateHelper.HardmodeBiomeCrate(itemLoot, main,
			ItemDropRule.NotScalingWithLuck(AutoContent.ItemType<SaltBlockDull>(), 3, 20, 50),
			ItemDropRule.NotScalingWithLuck(AutoContent.ItemType<Drywood>(), 3, 20, 50));
	}
}