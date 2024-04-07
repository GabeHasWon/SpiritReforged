using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Content.Ocean.Tiles;

namespace SpiritReforged.Content.Ocean.Items;

public class Kelp : FloatingItem
{
	public override float SpawnWeight => 1.2f;
	public override float Weight => base.Weight * 0.9f;
	public override float Bouyancy => base.Bouyancy * 1.15f;

	public override void SetDefaults()
	{
		Item.width = 30;
		Item.height = 24;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.value = 0;
		Item.rare = ItemRarityID.Blue;
		Item.createTile = ModContent.TileType<OceanKelp>();
		Item.maxStack = Item.CommonMaxStack;
		Item.autoReuse = true;
		Item.consumable = true;
		Item.useAnimation = 15;
		Item.useTime = 10;
	}
}