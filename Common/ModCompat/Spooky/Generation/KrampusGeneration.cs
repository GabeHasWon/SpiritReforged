using SpiritReforged.Content.Crossmod.Spooky.Tiles.Pots;
using System.Reflection;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.ModCompat.Spooky.Generation;

internal class KrampusGeneration : SpookyMicropass
{
	public delegate void hook_DungeonAmbience(object self, int PositionX, int PositionY, int Width, int Height);

	private static Point Position = new(-1, -1);

	public override string WorldGenName => "Uncommon Workshop";
	public override string ModifyType => "ChristmasDungeon";
	public override string MatchPass => "Christmas Dungeon";

	public override void Load()
	{
		Type parentType = LoadType();
		MethodInfo positionHook = parentType.GetMethod("DungeonAmbienceAndDetails");
		MonoModHooks.Add(positionHook, DungeonAmbienceHook);
	}

	private static void DungeonAmbienceHook(hook_DungeonAmbience orig, object self, int PositionX, int PositionY, int Width, int Height)
	{
		orig(self, PositionX, PositionY, Width, Height);
		Position = new Point(PositionX, PositionY);
	}

	public override void Run(GenerationProgress progress, GameConfiguration config)
	{
		List<int> walls =
		[
			SpookyWall("ChristmasBrickRedWall"),
			SpookyWall("ChristmasBrickBlueWall"),
			SpookyWall("ChristmasBrickGreenWall"),
			SpookyWall("ChristmasWoodWall"),
			SpookyWall("ChristmasWindow")
		];

		int Width = Main.maxTilesX / 30;
		int Height = Main.maxTilesY / 10;

		for (int i = Position.X - 25 - Width / 2; i <= Position.X + 25 + Width / 2; i++)
		{
			for (int j = Position.Y - 25 - Height / 2; j <= Position.Y + 25 + Height / 2; j++)
			{
				if (WorldGen.genRand.NextBool(24) && walls.Contains(Main.tile[i, j].WallType))
					WorldGen.PlaceObject(i, j - 1, ModContent.TileType<UncommonSpookyPots>(), true, 6 + WorldGen.genRand.Next(0, 3));
			}
		}

		static int SpookyWall(string name) => CrossMod.Spooky.Instance.Find<ModWall>(name).Type;
	}
}
