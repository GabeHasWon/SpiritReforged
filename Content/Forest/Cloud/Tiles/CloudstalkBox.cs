using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;

namespace SpiritReforged.Content.Forest.Cloud.Tiles;

public class CloudstalkBox : PlanterBoxTile
{
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		NPCShopHelper.AddEntry(new NPCShopHelper.ConditionalEntry((shop) => shop.NpcType == NPCID.Dryad, new NPCShop.Entry(this.AutoItemType())));
	}
}
