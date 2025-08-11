using Terraria.DataStructures;
using Terraria.GameContent.Biomes.CaveHouse;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes.MannequinInventories;

public abstract class MannequinInventory : ModSystem
{
	internal static Dictionary<HouseType, MannequinInventory> InventoryByBiome = [];

	public abstract HouseType Biome { get; }

	public override void Load()
	{
		InventoryByBiome.Add(Biome, this);
		Setup();
	}

	public override void PostSetupContent() => Setup();

	public abstract void Setup();

	public abstract void SetMannequin(Point16 position);
}
