namespace SpiritReforged.Content.SaltFlats.Items.Crates;

public class SaltCrateHardmodeRestored : ModItem
{
	public class SaltCrateHardmodeRestoredTile : SaltCrateHardmode.SaltCrateHardmodeTile;

	public override void SetStaticDefaults()
	{
		ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<SaltCrate>();
		Item.ResearchUnlockCount = 5;
	}
	
	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<SaltCrateHardmodeRestoredTile>());
		Item.rare = ItemRarityID.Green;
		Item.value = Item.sellPrice(0, 1, 0, 0);
	}

	public override bool CanRightClick() => true;
	public override void ModifyItemLoot(ItemLoot itemLoot) => SaltCrateHardmode.ModifyLoot(itemLoot);
}