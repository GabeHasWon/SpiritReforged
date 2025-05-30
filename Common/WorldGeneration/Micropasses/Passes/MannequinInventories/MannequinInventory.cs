using Terraria.DataStructures;
using Terraria.GameContent.Biomes.CaveHouse;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes.MannequinInventories;

public abstract class MannequinInventory : ILoadable
{
	internal static Dictionary<HouseType, MannequinInventory> InventoryByBiome = new();

	public abstract HouseType Biome { get; }

	public void Load(Mod mod)
	{
		InventoryByBiome.Add(Biome, this);
		Setup();
	}

	public abstract void Setup();

	public abstract void SetMannequin(Point16 position);

	public void Unload()
	{
	}
}
