using SpiritReforged.Content.Underground.NPCs;
using SpiritReforged.Content.Underground.Tiles;
using System.Linq;
using Terraria.WorldBuilding;
using SpiritReforged.Content.Underground.Tiles.Potion;
using SpiritReforged.Common.TileCommon;
using Terraria.DataStructures;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class PotsMicropass : Micropass
{
	private delegate bool GenDelegate(int x, int y);
	private static readonly int[] CommonBlacklist = [TileID.LihzahrdBrick, TileID.BlueDungeonBrick, TileID.GreenDungeonBrick, TileID.PinkDungeonBrick,
		TileID.Spikes, TileID.WoodenSpikes, TileID.CrackedBlueDungeonBrick, TileID.CrackedGreenDungeonBrick, TileID.CrackedPinkDungeonBrick];

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
		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.Caves");

		Generate(CreatePotion, Main.maxTilesX / WorldGen.WorldSizeSmallX * 10, out _);
		Generate(CreateScrying, Main.maxTilesX / WorldGen.WorldSizeSmallX * 9, out _);
        Generate(CreateStuffed, Main.maxTilesX / WorldGen.WorldSizeSmallX * 9, out _);
		Generate(CreateWorm, Main.maxTilesX / WorldGen.WorldSizeSmallX * 24, out _);
		Generate(CreatePlatter, Main.maxTilesX / WorldGen.WorldSizeSmallX * 28, out _);
		Generate(CreateAether, Main.maxTilesX / WorldGen.WorldSizeSmallX * 3, out _);
		Generate(CreateUpsideDown, Main.maxTilesX / WorldGen.WorldSizeSmallX * 4, out _);

		Generate(CreateStack, (int)(Main.maxTilesX * Main.maxTilesY * 0.0005), out _); //Normal pot generation weight is 0.0008
		Generate(CreateUncommon, (int)(Main.maxTilesX * Main.maxTilesY * 0.00055), out int pots);

		PotteryTracker.Remaining = (ushort)Main.rand.Next(pots / 2);
	}

	/// <param name="count"> The target number of successes. </param>
	/// <param name="generated"> The actual number of successes. </param>
	private static void Generate(GenDelegate del, int count, out int generated)
	{
		const int maxTries = 5000; //Failsafe
		int pots = 0;

		for (int t = 0; t < maxTries; t++) //Generate uncommon pots
		{
			int x = Main.rand.Next(20, Main.maxTilesX - 20);
			int y = Main.rand.Next((int)GenVars.worldSurfaceHigh, Main.maxTilesY - 20);

			WorldMethods.FindGround(x, ref y);

			if (del(x, y - 1) && ++pots >= count)
				break;
		}

		generated = pots;
	}

	private static bool CreatePotion(int x, int y)
	{
		if (y < Main.worldSurface || y > Main.UnderworldLayer || !CommonSurface(x, y))
			return false;

		int type = ModContent.TileType<PotionVats>();
		WorldGen.PlaceTile(x, y, type, true, style: Main.rand.Next([0, 3, 6]));

		if (Main.tile[x, y].TileType == type)
		{
			TileExtensions.GetTopLeft(ref x, ref y);
			TileEntity.PlaceEntityNet(x, y, ModContent.TileEntityType<VatSlot>());

			if (TileEntity.ByPosition.TryGetValue(new Point16(x, y), out var value) && value is VatSlot slot)
				slot.item = new Item(VatSlot.GetRandomPotion());

			return true;
		}

		return false;
	}

	private static bool CreateScrying(int x, int y)
	{
		if (y < Main.worldSurface || y > Main.UnderworldLayer || !CommonSurface(x, y))
			return false;

		int type = ModContent.TileType<ScryingPot>();
		WorldGen.PlaceTile(x, y, type, true);

		return Main.tile[x, y].TileType == type;
	}

    private static bool CreateStuffed(int x, int y)
    {
		if (y < Main.worldSurface || y > Main.UnderworldLayer || Main.tile[x, y].LiquidAmount > 100 || !CommonSurface(x, y))
			return false;

		int type = ModContent.TileType<StuffedPots>();
		WorldGen.PlaceTile(x, y, type, true, style: Main.rand.Next(3));

		return Main.tile[x, y].TileType == type;
	}

	private static bool CreateWorm(int x, int y)
	{
		int wall = Main.tile[x, y].WallType;

		if (y < Main.worldSurface && wall == WallID.None || y > Main.UnderworldLayer || !CommonSurface(x, y))
			return false;

		int type = ModContent.TileType<WormPot>();
		WorldGen.PlaceTile(x, y, type, true);

		return Main.tile[x, y].TileType == type;
	}

	private static bool CreatePlatter(int x, int y)
	{
		if (y < Main.worldSurface || y > Main.UnderworldLayer || !CommonSurface(x, y))
			return false;

		int type = ModContent.TileType<SilverPlatters>();
		WorldGen.PlaceTile(x, y, type, true, style: Main.rand.Next(3));

		return Main.tile[x, y].TileType == type;
	}

	private static bool CreateAether(int x, int y)
	{
		if (y < Main.worldSurface || y > Main.UnderworldLayer || !NearShimmer() || !CommonSurface(x, y))
			return false;

		int type = ModContent.TileType<AetherShipment>();
		WorldGen.PlaceTile(x, y, type, true);

		return Main.tile[x, y].TileType == type;

		bool NearShimmer() => Math.Abs(x - GenVars.shimmerPosition.X) < Main.maxTilesX * .2f && Math.Abs(y - GenVars.shimmerPosition.Y) < Main.maxTilesY * .2f;
	}

	private static bool CreateUpsideDown(int x, int y)
	{
		if (y < Main.worldSurface || y > Main.UnderworldLayer || !CommonSurface(x, y))
			return false;

		int type = ModContent.TileType<UpsideDownPot>();
		WorldGen.PlaceTile(x, y, type, true);

		return Main.tile[x, y].TileType == type;
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

	/// <summary> Checks whether the below tile is contained in <see cref="CommonBlacklist"/>. </summary>
	private static bool CommonSurface(int x, int y)
	{
		var t = Main.tile[x, y + 1];
		return !CommonBlacklist.Contains(t.TileType);
	}
}