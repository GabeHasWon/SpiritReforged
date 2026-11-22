using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat.Classic;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using TileHelper.Common;

namespace SpiritReforged.Content.Ocean.Tiles;

public class Driftwood : ModTile, ILoadItem
{
	private class DriftwoodCatchPlayer : ModPlayer
	{
		public override void CatchFish(FishingAttempt attempt, ref int itemDrop, ref int npcSpawn, ref AdvancedPopupRequest sonar, ref Vector2 sonarPosition)
		{
			if (attempt.common && Main.rand.NextBool(3) && Player.ZoneBeach)
				itemDrop = ModContent.GetInstance<Driftwood>().AutoItemType();
		}

		public override void ModifyCaughtFish(Item fish)
		{
			if (fish.type == ModContent.GetInstance<Driftwood>().AutoItemType())
				fish.stack = Main.rand.Next(2, 5);
		}
	}

	void ILoadItem.SetItemStaticDefaults(ModItem modItem) => ItemLootDatabase.AddItemRule(ItemID.OceanCrate, ItemDropRule.Common(this.AutoItemType(), 3, 6, 12));

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;
		Main.tileBrick[Type] = true;
		Main.tileMergeDirt[Type] = true;

		AddMapEntry(new Color(138, 79, 45));

		var item = this.AutoItem();
		Recipes.AddToGroup(RecipeGroupID.Wood, item.type);

		ItemLootDatabase.AddItemRule(ItemID.OceanCrate, ItemDropRule.Common(item.type, 4, 15, 35));
		ItemLootDatabase.AddItemRule(ItemID.OceanCrateHard, ItemDropRule.Common(item.type, 4, 15, 35));

		SpiritClassic.AddItemReplacement("DriftwoodTileItem", item.type);
		ItemID.Sets.ShimmerTransformToItem[item.type] = ItemID.Wood;
	}
}