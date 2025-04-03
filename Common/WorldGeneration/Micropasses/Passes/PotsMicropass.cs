using Terraria.WorldBuilding;
using SpiritReforged.Common.WorldGeneration.Micropasses;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.Underground.Tiles;
using SpiritReforged.Content.Underground.NPCs;

namespace SpiritMod.World.Micropasses;

internal class PotsMicropass : Micropass
{
	public override string WorldGenName => "Pots";

	public override void Load(Mod mod) => On_WorldGen.PlacePot += MushroomPotConversion;

	/// <summary> 50% chance to replace regular pots placed on mushroom grass. </summary>
	private static bool MushroomPotConversion(On_WorldGen.orig_PlacePot orig, int x, int y, ushort type, int style)
	{
		if (WorldGen.generatingWorld && Main.rand.NextBool())
		{
			var ground = Main.tile[x, y + 1];
			if (ground.HasTile && ground.TileType == TileID.MushroomGrass)
			{
				WorldGen.PlaceTile(x, y, ModContent.TileType<MushroomPots>(), true, style: Main.rand.Next(3));
				return false; //Skips orig
			}
		}

		return orig(x, y, type, style);
	}

	public override int GetWorldGenIndexInsert(List<GenPass> passes, ref bool afterIndex)
	{
		afterIndex = true;
		return passes.FindIndex(genpass => genpass.Name.Equals("Pots"));
	}

	public override void Run(GenerationProgress progress, Terraria.IO.GameConfiguration config)
	{
		const int maxTries = 5000; //Failsafe

		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.Caves");

		int maxPots = (int)(Main.maxTilesX * Main.maxTilesY * 0.0005); //Normal weight is 0.0008
		int pots = 0;

		for (int t = 0; t < maxTries; t++) //Generate stacked pots
		{
			int x = Main.rand.Next(20, Main.maxTilesX - 20);
			int y = Main.rand.Next((int)GenVars.worldSurfaceHigh, Main.maxTilesY - 20);

			WorldMethods.FindGround(x, ref y);

			if (CreateStack(x, y - 1) && ++pots >= maxPots)
				break;
		}

		maxPots = (int)(Main.maxTilesX * Main.maxTilesY * 0.00055);
		pots = 0;

		for (int t = 0; t < maxTries; t++) //Generate uncommon pots
		{
			int x = Main.rand.Next(20, Main.maxTilesX - 20);
			int y = Main.rand.Next((int)GenVars.worldSurfaceHigh, Main.maxTilesY - 20);

			WorldMethods.FindGround(x, ref y);

			if (CreateUncommon(x, y - 1) && ++pots >= maxPots)
				break;
		}

		PotteryTracker.Remaining = (ushort)Main.rand.Next(pots / 2);
	}

	/// <summary> Picks a relevant biome pot style and places it (<see cref="BiomePots"/>). </summary>
	private static bool CreateUncommon(int x, int y)
	{
		int tile = Main.tile[x, y + 1].TileType;
		int wall = Main.tile[x, y].WallType;

		int style = -1;

		if (wall is WallID.Dirt or WallID.GrassUnsafe || tile is TileID.Dirt or TileID.Stone or TileID.ClayBlock or TileID.WoodBlock or TileID.Granite && y > Main.worldSurface)
			style = GetRange(WorldGen.genRand.NextBool(100) ? BiomePots.Style.Gold : BiomePots.Style.Cavern);

		if (wall is WallID.SnowWallUnsafe || tile is TileID.SnowBlock or TileID.IceBlock or TileID.BreakableIce && y > Main.worldSurface)
			style = GetRange(BiomePots.Style.Ice);
		else if (wall is WallID.Sandstone or WallID.HardenedSand)
			style = GetRange(BiomePots.Style.Desert);
		else if (wall is WallID.MudUnsafe || tile is TileID.JungleGrass && y > Main.worldSurface)
			style = GetRange(BiomePots.Style.Jungle);
		else if (tile is TileID.CorruptGrass or TileID.Ebonstone or TileID.Demonite && y > Main.worldSurface)
			style = GetRange(BiomePots.Style.Corruption);
		else if (tile is TileID.CrimsonGrass or TileID.Crimstone or TileID.Crimtane && y > Main.worldSurface)
			style = GetRange(BiomePots.Style.Crimson);
		else if (tile is TileID.Marble)
			style = GetRange(BiomePots.Style.Marble);
		else if (tile is TileID.MushroomGrass)
			style = GetRange(BiomePots.Style.Mushroom);

		if (y > Main.UnderworldLayer)
			style = GetRange(BiomePots.Style.Hell);
		else if (tile is TileID.BlueDungeonBrick or TileID.GreenDungeonBrick or TileID.PinkDungeonBrick && Main.wallDungeon[wall])
			style = GetRange(BiomePots.Style.Dungeon);

		if (style != -1)
		{
			int type = ModContent.TileType<BiomePots>();
			WorldGen.PlaceTile(x, y, type, true, style: style);

			return Main.tile[x, y].TileType == type;
		}

		return false;

		static int GetRange(BiomePots.Style value)
		{
			int v = (int)value * 3;
			return WorldGen.genRand.Next(v, v + 3);
		}
	}

	private static bool CreateStack(int x, int y)
	{
		int tile = Main.tile[x, y + 1].TileType;
		int wall = Main.tile[x, y].WallType;

		if (wall is WallID.Dirt or WallID.GrassUnsafe || y > Main.worldSurface && y < Main.UnderworldLayer && tile is TileID.Dirt or TileID.Stone or TileID.ClayBlock or TileID.WoodBlock or TileID.Granite)
		{
			if (Main.rand.NextBool()) //Generate a stack of 3 in a pyramid
			{
				if (!WorldMethods.AreaClear(x - 1, y - 3, 4, 4))
					return false;

				WorldGen.PlaceTile(x - 1, y, ModContent.TileType<StackablePots>(), true, style: GetRandomStyle());
				WorldGen.PlaceTile(x + 1, y, ModContent.TileType<StackablePots>(), true, style: GetRandomStyle());
				WorldGen.PlaceTile(x, y - 2, ModContent.TileType<StackablePots>(), true, style: GetRandomStyle());
			}
			else //Generate a stack of 2 in a tower
			{
				if (!WorldMethods.AreaClear(x, y - 5, 2, 4))
					return false;

				for (int s = 0; s < 2; s++)
					WorldGen.PlaceTile(x, y - s * 2, ModContent.TileType<StackablePots>(), true, style: GetRandomStyle());
			}

			return true;
		}

		return false;

		static int GetRandomStyle() => WorldGen.genRand.Next(12);
	}
}
