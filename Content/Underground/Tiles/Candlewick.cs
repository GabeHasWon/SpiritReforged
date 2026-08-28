using TileHelper.Common;

namespace SpiritReforged.Content.Underground.Tiles;

public class Candlewick : ModTile, ILoadItem
{
	void ILoadItem.SetItemDefaults(ModItem modItem)
	{
		modItem.Item.value = Item.buyPrice(copper: 10);
		modItem.Item.tileBoost = 3;
	}

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileBlockLight[Type] = false;
		Main.tileRope[Type] = true;

		AddMapEntry(FurnitureTile.MapColor);

		DustType = DustID.Rope;
	}
}