using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.NPCCommon.Interfaces;

namespace SpiritReforged.Content.Forest.Misc;

[AutoloadCritter]
public class GoldCricket : Cricket, IGoldCritter, ItemEvents.IQuickRecipeNPC
{
	public override int[] TypesToReplace => [NPCID.GoldGrasshopper];
	public int[] NormalPersistentIDs => [ModContent.NPCType<Cricket>()];

	void ItemEvents.IQuickRecipeNPC.AddRecipes() => Recipe.Create(ItemID.GoldenDelight).AddIngredient(this.AutoItemType()).AddTile(TileID.CookingPots).Register();

	public override void CreateItemDefaults() =>
		ItemEvents.CreateItemDefaults(
		this.AutoItemType(),
		item =>
		{
			item.value = Item.sellPrice(0, 10, 0, 0);
			item.bait = 50;
		}
	);
}