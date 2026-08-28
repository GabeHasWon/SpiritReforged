using TileHelper.Common;

namespace SpiritReforged.Content.Underground.Tiles;

public class WaxBlock : ModTile, ILoadItem
{
	void ILoadItem.SetItemDefaults(ModItem modItem) => modItem.Item.value = Item.buyPrice(copper: 10);

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;

		AddMapEntry(new Color(230, 215, 190));

		DustType = DustID.Bone;
	}
}