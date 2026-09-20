using SpiritReforged.Common.WorldGeneration.Micropasses;
using SpiritReforged.Content.Crossmod.Spooky.Tiles.Pots;
using System.Linq;
using System.Reflection;
using Terraria.GameContent.Generation;
using Terraria.IO;
using Terraria.ModLoader.Core;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.ModCompat.Spooky.Generation;

internal class SpookyForestGeneration : Micropass
{
	public delegate void hook_ModifyGen(object self, List<GenPass> tasks, ref double totalWeight);

	public override string WorldGenName => "Spooky Uncommon Pots";

	public override bool IsLoadingEnabled(Mod mod) => CrossMod.Spooky.Enabled;

	public override void Load()
	{
		Type[] types = AssemblyManager.GetLoadableTypes(CrossMod.Spooky.Instance.Code);
		Type spookyForestGen = types.FirstOrDefault(x => x.Name == "SpookyForest");
		MethodInfo modifyMethod = spookyForestGen.GetMethod("ModifyWorldGenTasks");
		MonoModHooks.Add(modifyMethod, HookGen);
	}

	private static void HookGen(hook_ModifyGen orig, object self, List<GenPass> passes, ref double totalWeight)
	{
		orig(self, passes, ref totalWeight);

		int index = passes.FindIndex(x => x.Name == "Spooky Forest") + 1;

		if (index != -1)
			passes.Insert(index, new PassLegacy("Spooky Uncommon Pots", ModContent.GetInstance<SpookyForestGeneration>().Run));
	}

	public override int GetWorldGenIndexInsert(List<GenPass> tasks, ref bool afterIndex) => -1;

	private static int SpookyTile(string name) => CrossMod.Spooky.Instance.Find<ModTile>(name).Type;

	public override void Run(GenerationProgress progress, GameConfiguration config)
	{
		if (!CrossMod.Spooky.TryCall(out Vector2 BiomePosition, "BiomePositions", "SpookyBiomeCenter"))
			return;

		Point pos = BiomePosition.ToPoint();

		for (int X = pos.X - Main.maxTilesX / 12; X <= pos.X + Main.maxTilesX / 12; X++)
		{
			for (int Y = (int)Main.worldSurface; Y < Main.maxTilesY - 200; Y++)
			{
				Tile tile = Main.tile[X, Y];
				
				if (tile.TileType == SpookyTile("SpookyStone") || tile.TileType == SpookyTile("SpookyGrass") || tile.TileType == SpookyTile("SpookyGrassGreen") 
					|| tile.TileType == SpookyTile("MushroomMoss"))
				{
					if (WorldGen.genRand.NextBool(30))
						WorldGen.PlaceObject(X, Y - 1, ModContent.TileType<UncommonSpookyPots>(), true, 24 + WorldGen.genRand.Next(0, 3));
				}
			}
		}
	}
}
