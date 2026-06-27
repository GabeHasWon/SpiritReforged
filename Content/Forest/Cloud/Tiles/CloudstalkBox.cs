using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using TileHelper.Common;

namespace SpiritReforged.Content.Forest.Cloud.Tiles;

public class CloudstalkBox : PlanterBoxTile, ILoadItem
{
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		NPCShopHelper.AddEntry(new NPCShopHelper.ConditionalEntry((shop) => shop.NpcType == NPCID.Dryad, new NPCShop.Entry(this.AutoItemType())));
	}
}