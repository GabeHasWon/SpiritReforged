using SpiritReforged.Content.Crossmod.Spooky.Tiles.Pots;
using System.Reflection;
using Terraria.GameContent.Biomes;

namespace SpiritReforged.Common.ModCompat.Spooky.Generation;

internal class FetidFarmsGeneration : ILoadable
{
	private delegate void hook_BiomeAmbience(object self, int PositionX, int PositionY, int SizeX, int SizeY);

	bool ILoadable.IsLoadingEnabled(Mod mod) => CrossMod.Spooky.Enabled;

	void ILoadable.Load(Mod mod)
	{
		Type type = CrossMod.Spooky.Instance.Code.GetType("Spooky.Content.Generation.VegetableGarden");
		MethodInfo method = type.GetMethod("PlaceAmbience", BindingFlags.Public | BindingFlags.Instance);

		MonoModHooks.Add(method, AddUncommonPots);
	}

	private static void AddUncommonPots(hook_BiomeAmbience orig, object self, int PositionX, int PositionY, int SizeX, int SizeY)
	{
		orig(self, PositionX, PositionY, SizeX, SizeY);

		int grass = CrossMod.Spooky.Find<ModTile>("JungleSoilGrass").Type;
		int moss = CrossMod.Spooky.Find<ModTile>("JungleMoss").Type;

		for (int i = PositionX - SizeX * 2; i < PositionX + SizeX * 2; i++)
		{
			for (int j = PositionY - SizeY * 2; j < PositionY + SizeY * 2; j++)
			{
				Tile tile = Main.tile[i, j];
				Tile tileAbove = Main.tile[i, j - 1];

				if ((tile.TileType == grass || tile.TileType == moss) && !tileAbove.HasTile)
				{
					if (WorldGen.genRand.NextBool(25) && !Main.tile[i, j - 1].HasTile)
						WorldGen.PlaceObject(i, j - 1, ModContent.TileType<UncommonSpookyPots>(), true, 15 + WorldGen.genRand.Next(0, 3)); 
				}
			}
		} 
	}

	void ILoadable.Unload() { }
}
