using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.Savanna.Biome;

namespace SpiritReforged.Content.Savanna.Tiles;

public class SavannaTorch : TorchTile
{
	public override float GetTorchLuck(Player player)
	{
		float value = -0.5f;

		if (player.InModBiome<SavannaBiome>())
			value = 1f;
		else if (player.ZoneDesert || player.ZoneJungle)
			value = 0.5f;
		else if (player.ZoneSnow)
			value = -1f;

		return value;
	}
}

public class SavannaTorchItem : TorchItem
{
	public override int TileType => ModContent.TileType<SavannaTorch>();
	public override void AddRecipes() => CreateRecipe(3).AddIngredient(ItemID.Gel).AddIngredient(ItemMethods.AutoItemType<Drywood>()).Register();
}