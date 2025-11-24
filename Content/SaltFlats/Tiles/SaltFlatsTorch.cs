using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.SaltFlats.Biome;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using SpiritReforged.Content.Savanna.Tiles;

namespace SpiritReforged.Content.SaltFlats.Tiles;

public class SaltFlatsTorch : TorchTile
{
	public override Vector3 Light => ModContent.GetInstance<SaltFlatsTorchItem>().Light;

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		DustType = DustID.WhiteTorch;
	}

	public override float GetTorchLuck(Player player)
	{
		float value = -0.5f;

		if (player.InModBiome<SaltBiome>())
			value = 1f;
		else if (player.ZoneSnow || player.ZoneDesert)
			value = 0.5f;
		else if (player.ZoneJungle)
			value = -1f;

		return value;
	}
}

public class SaltFlatsTorchItem : TorchItem
{
	public override int TileType => ModContent.TileType<SaltFlatsTorch>();
	public override Vector3 Light => (Color.MediumPurple * 1.1f).ToVector3();

	public override void AddRecipes() => CreateRecipe(3).AddIngredient(ItemID.Gel).AddIngredient(AutoContent.ItemType<Drywood>()).AddIngredient(AutoContent.ItemType<SaltBlockDull>()).Register();
}