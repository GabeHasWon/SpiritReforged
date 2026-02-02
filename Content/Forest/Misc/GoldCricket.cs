using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.NPCCommon;

namespace SpiritReforged.Content.Forest.Misc;

[AutoloadCritter]
public class GoldCricket : Cricket, IGoldCritter
{
	public int[] NormalPersistentIDs => [ModContent.NPCType<Cricket>()];

	public override void CreateItemDefaults() =>
		ItemEvents.CreateItemDefaults(
		this.AutoItemType(),
		item =>
		{
			item.value = Item.sellPrice(0, 10, 0, 0);
			item.bait = 50;
		}
	);

	public override float SpawnChance(NPCSpawnInfo spawnInfo) => base.SpawnChance(spawnInfo) / 100f;
}