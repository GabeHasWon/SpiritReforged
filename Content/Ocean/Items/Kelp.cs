using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ItemCommon.FloatingItem;
using SpiritReforged.Content.Ocean.Tiles;

namespace SpiritReforged.Content.Ocean.Items;

public class Kelp : FloatingItem
{
	public override float SpawnWeight => 1.2f;
	public override float Weight => base.Weight * 0.9f;
	public override float Bouyancy => base.Bouyancy * 1.15f;

	public override void SetStaticDefaults()
	{
		VariantGlobalItem.AddVariants(Type, [new Point(24, 18), new Point(20, 24), new Point(22, 22), new Point(20, 22)]);
		Item.ResearchUnlockCount = 25;
		ItemID.Sets.ShimmerTransformToItem[Type] = AutoContent.ItemType<Driftwood>();
	}

	public override void SetDefaults()
	{
		Item.width = 30;
		Item.height = 24;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.value = 0;
		Item.rare = ItemRarityID.White;
		Item.createTile = ModContent.TileType<OceanKelp>();
		Item.maxStack = Item.CommonMaxStack;
		Item.autoReuse = true;
		Item.consumable = true;
		Item.useAnimation = 15;
		Item.useTime = 10;
	}
}