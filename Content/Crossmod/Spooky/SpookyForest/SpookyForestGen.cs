using SpiritReforged.Common.ItemCommon.Pins;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.WorldGeneration.Micropasses;
using SpiritReforged.Content.Forest.Stargrass.Items;
using System.Reflection;
using Terraria.DataStructures;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Content.Crossmod.Spooky.SpookyForest;

internal class SpookyForestGen : Micropass
{
	public override string WorldGenName => "Spirit Halloween: Spooky Forest";

	public override bool IsLoadingEnabled(Mod mod) => CrossMod.Spooky.Enabled;

	public override void Load()
	{
		Type forestType = CrossMod.Spooky.Instance.Code.GetType("Spooky.Content.Generation.SpookyForest");
		MethodInfo info = forestType.GetMethod("PostWorldGen", BindingFlags.Public | BindingFlags.Instance);

		MonoModHooks.Add(info, DetourPostWorldGen);
	}

	public static void DetourPostWorldGen(Action<object> orig, object self)
	{
		orig(self);

		int chestType = CrossMod.Spooky.Find<ModTile>("OldWoodChest").Type;
		int empType = CrossMod.Spooky.Find<ModItem>("EMFReaderBroke").Type;

		for (int i = 0; i < Main.maxChests; i++)
		{
			Chest chest = Main.chest[i];

			if (chest == null || !WorldGen.InWorld(chest.x, chest.y))
				continue;

			Tile chestTile = Main.tile[chest.x, chest.y];

			if (chestTile.TileType == chestType && chest.item[0].type != empType)
			{
				int index = 0;

				while (!chest.item[index].IsAir)
					index++;

				if (WorldGen.genRand.NextBool(2))
				{
					chest.item[index] = new Item(WorldGen.genRand.Next(3) switch
					{
						0 => ModContent.ItemType<PumpkinPailOrange>(),
						1 => ModContent.ItemType<PumpkinPailPurple>(),
						_ => ModContent.ItemType<PumpkinPailWhite>()
					});
				}
			}
		}
	}

	public override int GetWorldGenIndexInsert(List<GenPass> tasks, ref bool afterIndex) => tasks.FindIndex(x => x.Name == "Guide");

	public override void Run(GenerationProgress progress, GameConfiguration config)
	{
		List<Point16> positions = [];
		CrossMod.Spooky.TryFind("SpookyGrass", out ModTile orange);
		CrossMod.Spooky.TryFind("SpookyGrassGreen", out ModTile green);

		for (int i = WorldGen.beachDistance; i < Main.maxTilesX - WorldGen.beachDistance; ++i)
		{
			for (int j = (int)(Main.worldSurface * 0.45); j < Main.worldSurface; ++j)
			{
				Tile tile = Main.tile[i, j];

				if (!tile.HasTile)
					continue;

				if (tile.TileType == orange.Type || tile.TileType == green.Type)
					positions.Add(new Point16(i, j));
			}
		}

		int repeats = 0;

		while (true)
		{
			Point16 pos = WorldGen.genRand.Next(positions);
			WorldGen.PlaceObject(pos.X, pos.Y - 1, (ushort)ModContent.TileType<PumpkinPailTile>(), true, WorldGen.genRand.Next(3));
			Tile tile = Main.tile[pos.X, pos.Y - 1];

			if (tile.HasTile && tile.TileType == ModContent.TileType<PumpkinPailTile>())
				break;

			repeats++;

			if (repeats > 10_000)
				break;
		}

		float reps = Main.maxTilesX / 4200f * 3;

		for (int i = 0; i < reps; ++i)
		{
			Point16 pos = WorldGen.genRand.Next(positions);
			int size = WorldGen.genRand.Next(40, 71);
			WorldGen.Convert(pos.X, pos.Y, StarConversion.ConversionType, size, true, true);
		}
	}
}
