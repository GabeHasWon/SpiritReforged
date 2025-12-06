using Terraria.GameContent.ItemDropRules;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using SpiritReforged.Content.Savanna.Tiles;

namespace SpiritReforged.Content.SaltFlats.Items;

public class SaltCrateHardmode : ModItem
{
	public class SaltCrateHardmodeTile : SaltCrate.SaltCrateTile;

	public override void SetStaticDefaults()
	{
		ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<SaltCrate>();
		Item.ResearchUnlockCount = 10;
	}
	
	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<SaltCrateHardmodeTile>());
		Item.rare = ItemRarityID.Green;
	}

	public override bool CanRightClick() => true;
	public override void ModifyItemLoot(ItemLoot itemLoot)
	{
		int[] dropOptions = [ModContent.ItemType<MahakalaMaskBlue>(),
			ModContent.ItemType<MahakalaMaskRed>(),
			ItemID.AnkletoftheWind,
			ItemID.CloudinaBottle,
			ItemID.WaterWalkingBoots];

		IItemDropRule main = ItemDropRule.OneFromOptions(1, dropOptions);

		CrateHelper.HardmodeBiomeCrate(itemLoot, main, 
			ItemDropRule.NotScalingWithLuck(AutoContent.ItemType<SaltBlockDull>(), 3, 20, 50), 
			ItemDropRule.NotScalingWithLuck(AutoContent.ItemType<Drywood>(), 3, 20, 50));
	}
}