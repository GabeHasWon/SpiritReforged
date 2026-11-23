using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.NPCCommon.Interfaces;

namespace SpiritReforged.Content.Savanna.NPCs;

[AutoloadCritter]
public class GoldJerboa : Jerboa, IGoldCritter, ItemEvents.IQuickRecipeNPC
{
	void ItemEvents.IQuickRecipeNPC.AddRecipes() => Recipe.Create(ItemID.GoldenDelight).AddIngredient(this.AutoItemType()).AddTile(TileID.CookingPots).Register();

	public int[] NormalPersistentIDs => [ModContent.NPCType<Jerboa>()];

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

	public override float SpawnChance(NPCSpawnInfo spawnInfo) => base.SpawnChance(spawnInfo) * 0.005f;
}