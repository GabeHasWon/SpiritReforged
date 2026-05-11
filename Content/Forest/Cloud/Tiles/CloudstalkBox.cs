using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;

namespace SpiritReforged.Content.Forest.Cloud.Tiles;

public class CloudstalkBox : PlanterBoxTile
{
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		NPCShopHelper.AddEntry(NPCShopHelper.ConditionalEntry.FromNPC(NPCID.Dryad, new NPCShop.Entry(this.AutoItemType())));
	}
}
