using SpiritReforged.Content.Underground.NPCs;
using SpiritReforged.Content.Underground.Tiles;
using System.Linq;
using Terraria.WorldBuilding;
using SpiritReforged.Content.Underground.Tiles.Potion;
using SpiritReforged.Common.TileCommon;
using static SpiritReforged.Common.WorldGeneration.WorldMethods;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class PotsMicropass : Micropass
{
	private delegate bool GenDelegate(int x, int y);
	private static readonly int[] CommonBlacklist = [TileID.LihzahrdBrick, TileID.BlueDungeonBrick, TileID.GreenDungeonBrick, TileID.PinkDungeonBrick,
		TileID.Spikes, TileID.WoodenSpikes, TileID.CrackedBlueDungeonBrick, TileID.CrackedGreenDungeonBrick, TileID.CrackedPinkDungeonBrick];

	public override string WorldGenName => "Pots";

	public override void Load(Mod mod) => On_WorldGen.PlacePot += PotConversion;
	/// <summary> 50% chance to replace regular pots placed on mushroom grass.<br/>
	/// 100% chance to replace regular pots placed on granite. </summary>
	private static bool PotConversion(On_WorldGen.orig_PlacePot orig, int x, int y, ushort type, int style)
	{
		if (WorldGen.generatingWorld)
		{
			var ground = Main.tile[x, y + 1];

			if (ground.HasTile && ground.TileType == TileID.MushroomGrass)
			{
				if (WorldGen.genRand.NextBool())
				{
					WorldGen.PlaceTile(x, y, ModContent.TileType<CommonPots>(), true, style: Main.rand.Next(3));
					return false; //Skips orig
				}
			}
			else if (ground.HasTile && ground.TileType is TileID.Granite or TileID.GraniteBlock) // Add smooth granite for Remnants compatibility
			{
				WorldGen.PlaceTile(x, y, ModContent.TileType<CommonPots>(), true, style: Main.rand.Next([3, 4, 5]));
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
		RunMultipliedTask(1);
	}

	public static void RunMultipliedTask(float multiplier)
	{
		float worldScale = Main.maxTilesX / (float)WorldGen.WorldSizeSmallX;
		float scale = (worldScale + (worldScale - 1)) * multiplier;

		Generate(CreateOrnate, (int)(scale * 5), out _);
		Generate(CreatePotion, (int)(scale * 46), out _);
		Generate(CreateScrying, (int)(scale * 20), out _);
		Generate(CreateStuffed, (int)(scale * 12), out _);
		Generate(CreateWorm, (int)(scale * 18), out _);
		Generate(CreatePlatter, (int)(scale * 24), out _);
		Generate(CreateAether, (int)(scale * 3), out _);
		Generate(CreateUpsideDown, (int)(scale * 4), out _);

		Generate(CreateStack, (int)(Main.maxTilesX * Main.maxTilesY * 0.0005 * multiplier), out _, maxTries: 4000); //Normal pot generation weight is 0.0008
		Generate(CreateUncommon, (int)(Main.maxTilesX * Main.maxTilesY * 0.00055 * multiplier), out int pots, maxTries: 4000);

		if (Main.zenithWorld)
			Generate(CreateZenith, (int)(scale * 200), out _);

		PotteryTracker.Remaining = (ushort)Main.rand.Next(pots / 2);
	}

	public static bool CreateOrnate(int x, int y)
	{
		FindGround(x, ref y);
		y--;

		if (y < Main.worldSurface || y > Main.UnderworldLayer || !CommonSurface(x, y))
			return false;

		int type = ModContent.TileType<OrnatePots>();
		Placer.Check(x, y, type).IsClear().Place();

		return Main.tile[x, y].TileType == type;
	}

	public static bool CreatePotion(int x, int y)
	{
		FindGround(x, ref y);
		y--;

		if (y < Main.worldSurface || y > Main.UnderworldLayer || !CommonSurface(x, y))
			return false;

		int type = ModContent.TileType<PotionVats>();
		var attempt = Placer.Check(x, y, type, style: Main.rand.Next([0, 3, 6])).IsClear().Place().PostPlacement<VatSlot>(out var slot);

		if (attempt.success)
		{
			slot.item = new Item(VatSlot.GetRandomNaturalPotion());
			return true;
		}

		return false;
	}

	public static bool CreateScrying(int x, int y)
	{
		FindGround(x, ref y);
		y--;

		if (y < Main.worldSurface || y > Main.UnderworldLayer || !CommonSurface(x, y))
			return false;

		int type = ModContent.TileType<ScryingPot>();
		Placer.Check(x, y, type).IsClear().Place();

		return Main.tile[x, y].TileType == type;
	}

	public static bool CreateStuffed(int x, int y)
    {
		FindGround(x, ref y);
		y--;

		if (y < Main.worldSurface || y > Main.UnderworldLayer || Main.tile[x, y].LiquidAmount > 100 || !CommonSurface(x, y))
			return false;

		int type = ModContent.TileType<StuffedPots>();
		Placer.Check(x, y, type).IsClear().Place();

		return Main.tile[x, y].TileType == type;
	}

	public static bool CreateWorm(int x, int y)
	{
		FindGround(x, ref y);
		y--;

		int wall = Main.tile[x, y].WallType;

		if (y < Main.worldSurface && wall == WallID.None || y > Main.UnderworldLayer || !CommonSurface(x, y))
			return false;

		int type = ModContent.TileType<WormPot>();
		Placer.Check(x, y, type).IsClear().Place();

		return Main.tile[x, y].TileType == type;
	}

	public static bool CreatePlatter(int x, int y)
	{
		FindGround(x, ref y);
		y--;

		if (y < Main.worldSurface || y > Main.UnderworldLayer || !CommonSurface(x, y))
			return false;

		int type = ModContent.TileType<SilverPlatters>();
		Placer.Check(x, y, type).IsClear().Place();

		return Main.tile[x, y].TileType == type;
	}

	public static bool CreateAether(int x, int y)
	{
		FindGround(x, ref y);
		y--;

		if (y < Main.worldSurface || y > Main.UnderworldLayer || !NearShimmer() || !CommonSurface(x, y))
			return false;

		int type = ModContent.TileType<AetherShipment>();
		Placer.Check(x, y, type).IsClear().Place();

		return Main.tile[x, y].TileType == type;

		bool NearShimmer() => Math.Abs(x - GenVars.shimmerPosition.X) < Main.maxTilesX * .2f && Math.Abs(y - GenVars.shimmerPosition.Y) < Main.maxTilesY * .2f;
	}

	public static bool CreateUpsideDown(int x, int y)
	{
		FindGround(x, ref y);
		y--;

		if (y < Main.worldSurface || y > Main.UnderworldLayer || !CommonSurface(x, y))
			return false;

		int type = ModContent.TileType<UpsideDownPot>();
		Placer.Check(x, y, type).IsClear().Place();

		return Main.tile[x, y].TileType == type;
	}

	public static bool CreateZenith(int x, int y)
	{
		FindGround(x, ref y);
		y--;

		if (y < Main.worldSurface || y > Main.UnderworldLayer || !CommonSurface(x, y))
			return false;

		int type = ModContent.TileType<CommonPots>();
		int style = (WorldGen.SavedOreTiers.Gold == TileID.Gold) ? WorldGen.genRand.Next(6, 9) : WorldGen.genRand.Next(9, 12);

		Placer.Check(x, y, type, style).IsClear().Place();

		return Main.tile[x, y].TileType == type;
	}

	/// <summary> Picks a relevant biome pot style and places it (<see cref="BiomePots"/>). </summary>
	public static bool CreateUncommon(int x, int y)
	{
		FindGround(x, ref y);
		y--;

		int tile = Main.tile[x, y + 1].TileType;
		int wall = Main.tile[x, y].WallType;

		if (Main.tile[x, y].LiquidType is LiquidID.Shimmer || !AreaClear(x, y - 1, 2, 2, true))
			return false; //Never generate in shimmer or over other tiles

		int style = -1;

		if (wall is WallID.Dirt or WallID.GrassUnsafe || tile is TileID.Dirt or TileID.Stone or TileID.ClayBlock or TileID.WoodBlock or TileID.Granite && y > Main.worldSurface)
			style = GetRange(BiomePots.Style.Cavern);

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
		else if (tile is TileID.Granite)
			style = GetRange(BiomePots.Style.Granite);

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

	public static bool CreateStack(int x, int y)
	{
		FindGround(x, ref y);
		y--;

		if (!CommonSurface(x, y))
			return false;

		int tile = Main.tile[x, y + 1].TileType;
		int wall = Main.tile[x, y].WallType;

		if (wall is WallID.Dirt or WallID.GrassUnsafe || y > Main.worldSurface && y < Main.UnderworldLayer && (tile is TileID.Dirt or TileID.Stone or TileID.ClayBlock or TileID.WoodBlock or TileID.Granite || WoodenPlatform(Main.tile[x, y + 1])))
		{
			if (Main.rand.NextBool()) //Generate a stack of 3 in a pyramid
			{
				if (!AreaClear(x - 1, y - 3, 4, 4, true))
					return false;

				WorldGen.PlaceTile(x - 1, y, ModContent.TileType<StackablePots>(), true, style: GetRandomStyle());
				WorldGen.PlaceTile(x + 1, y, ModContent.TileType<StackablePots>(), true, style: GetRandomStyle());
				WorldGen.PlaceTile(x, y - 2, ModContent.TileType<StackablePots>(), true, style: GetRandomStyle());
			}
			else //Generate a stack of 2 in a tower
			{
				if (!AreaClear(x, y - 5, 2, 4, true))
					return false;

				for (int s = 0; s < 2; s++)
					WorldGen.PlaceTile(x, y - s * 2, ModContent.TileType<StackablePots>(), true, style: GetRandomStyle());
			}

			return true;
		}

		return false;

		static int GetRandomStyle() => WorldGen.genRand.Next(12);
		static bool WoodenPlatform(Tile t) => t.TileType == TileID.Platforms && t.TileFrameY == 0;
	}

	/// <summary> Checks whether the below tile is contained in <see cref="CommonBlacklist"/>. </summary>
	private static bool CommonSurface(int x, int y) => !CommonBlacklist.Contains(Main.tile[x, y + 1].TileType) && Main.tile[x, y].LiquidType != LiquidID.Shimmer;
}