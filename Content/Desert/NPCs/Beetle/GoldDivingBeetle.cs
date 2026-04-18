using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.NPCCommon.Interfaces;

namespace SpiritReforged.Content.Desert.NPCs.Beetle;

[AutoloadCritter]
public class GoldDivingBeetle : DivingBeetle, IGoldCritter
{
	public int[] NormalPersistentIDs => [ModContent.NPCType<DivingBeetle>()];

	public override void CreateItemDefaults() =>
		ItemEvents.CreateItemDefaults(
		this.AutoItemType(),
		item =>
		{
			item.value = Item.sellPrice(0, 10, 0, 0);
			item.bait = 50;
			item.rare = ItemRarityID.Orange;
		}
	);
}