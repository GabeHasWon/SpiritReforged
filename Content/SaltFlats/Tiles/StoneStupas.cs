using RubbleAutoloader;
using SpiritReforged.Common.TileCommon.Loot;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.Underground.Pottery;
using Terraria.DataStructures;
using SpiritReforged.Content.Underground.Tiles;
using SpiritReforged.Common.TileCommon;

namespace SpiritReforged.Content.SaltFlats.Tiles;

public class StoneStupas : PotTile, ILootTile
{
	public override Dictionary<string, int[]> TileStyles => new() { { string.Empty, [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11] } };

	public override void AddItemRecipes(ModItem modItem, StyleDatabase.StyleGroup group)
	{
		LocalizedText dicovered = AutoloadedPotItem.Discovered;
		var function = (modItem as AutoloadedPotItem).RecordedPot;

		modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.StoneBlock, 3).AddTile(ModContent.TileType<PotteryWheel>()).AddCondition(dicovered, function).Register();
	}

	public override void AddObjectData()
	{
		DustType = Autoloader.IsRubble(Type) ? -1 : DustID.Stone;
		base.AddObjectData();
	}

	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (effectOnly || fail || Autoloader.IsRubble(Type) || !TileObjectData.IsTopLeft(i, j))
			return;

		for (int g = 1; g < 7; g++)
		{
			int goreType = Mod.Find<ModGore>("Stupa" + g).Type;
			Gore.NewGore(new EntitySource_TileBreak(i, j), new Vector2(i, j) * 16, Vector2.Zero, goreType);
		}
	}

	public void AddLoot(ILootTile.Context context, ILoot loot) => TileLootHandler.InvokeLootPool(ModContent.TileType<Pots>(), context, loot);
}