using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Content.Savanna.Tiles;

namespace SpiritReforged.Content.Savanna.Items.Tools;

internal class LivingBaobabWand : ModItem
{
	public override void SetStaticDefaults() => ItemID.Sets.DisableAutomaticPlaceableDrop[Type] = true;

	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.LivingWoodWand);

		Item.Size = new Vector2(36, 28);
		Item.tileWand = AutoContent.ItemType<Drywood>();
		Item.createTile = ModContent.TileType<LivingBaobab>();
		Item.useStyle = ItemUseStyleID.Swing;
		Item.useTurn = true;
		Item.autoReuse = true;
		Item.rare = ItemRarityID.Blue;
	}
}