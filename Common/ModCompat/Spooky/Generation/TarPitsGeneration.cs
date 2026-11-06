using SpiritReforged.Content.Crossmod.Spooky.Tiles.Pots;
using System.Reflection;

namespace SpiritReforged.Common.ModCompat.Spooky.Generation;

internal class TarPitsGeneration : ILoadable
{
	private delegate void hook_BiomeAmbience(object self, int PositionX, int PositionY, int SizeX, int SizeY);

	bool ILoadable.IsLoadingEnabled(Mod mod) => CrossMod.Spooky.Enabled;

	void ILoadable.Load(Mod mod)
	{
		Type tarPitsType = CrossMod.Spooky.Instance.Code.GetType("Spooky.Content.Generation.TarPits");
		MethodInfo pitDetour = tarPitsType.GetMethod("BiomeAmbience", BindingFlags.Public | BindingFlags.Instance);

		MonoModHooks.Add(pitDetour, AddUncommonPots);
	}

	private static void AddUncommonPots(hook_BiomeAmbience orig, object self, int PositionX, int PositionY, int SizeX, int SizeY)
	{
		orig(self, PositionX, PositionY, SizeX, SizeY);

		int sand = CrossMod.Spooky.Find<ModTile>("DesertSand").Type;
		int stone = CrossMod.Spooky.Find<ModTile>("DesertSandstone").Type;

		for (int i = PositionX - SizeX + SizeX / 2; i < PositionX + SizeX - SizeX / 2; i++)
		{
			for (int j = PositionY - SizeY - SizeY / 2; j < PositionY + SizeY + SizeY / 2; j++)
			{
				if (Main.tile[i, j].TileType == sand || Main.tile[i, j].TileType == stone)
				{
					if (WorldGen.genRand.NextBool(20) && !Main.tile[i, j - 1].HasTile)
						WorldGen.PlaceObject(i, j - 1, ModContent.TileType<UncommonSpookyPots>(), true, 9 + WorldGen.genRand.Next(0, 3));
				}
			}
		} 
	}

	void ILoadable.Unload() { }
}
