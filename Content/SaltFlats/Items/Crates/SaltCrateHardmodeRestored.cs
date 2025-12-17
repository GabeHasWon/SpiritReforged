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
	}

	public override bool CanRightClick() => true;
	public override void ModifyItemLoot(ItemLoot itemLoot) => SaltCrate.ModifyLoot(itemLoot);
}