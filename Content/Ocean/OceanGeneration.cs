using SpiritReforged.Common.ConfigurationCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.Ocean.Hydrothermal.Tiles;
using SpiritReforged.Content.Ocean.Items;
using SpiritReforged.Content.Ocean.Items.Reefhunter.OceanPendant;
using SpiritReforged.Content.Ocean.Tiles;
using Terraria.GameContent.Generation;
using Terraria.IO;
using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace SpiritReforged.Content.Ocean;

public partial class OceanGeneration : ModSystem
{
	public enum OceanShape
	{
		Default = 0, //vanilla worldgen
		SlantedSine, //Yuyu's initial sketch
		Piecewise, //Musicano's original sketch
		Piecewise_M, //Musicano's sketch with Sal/Yuyu's cubic modification
		Piecewise_V, //My heavily modified piecewise with variable height
	}

	private static float PiecewiseVScale = 1f;
	private static float PiecewiseVMountFactor = 1f;

	private static int _roughTimer = 0;
	private static float _rough = 0f;

	/// <summary> The approximate rectangle bounds of the left ocean side, cleared after worldgen. </summary>
	[WorldBound]
	public static Rectangle LeftOcean;
	/// <summary> The approximate rectangle bounds of the right ocean side, cleared after worldgen. </summary>
	[WorldBound]
	public static Rectangle RightOcean;

	public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
	{
        if (ModContent.GetInstance<ReforgedClientConfig>().OceanShape != OceanShape.Default)
        {
            int beachIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Beaches")); //Replace beach gen
            if (beachIndex != -1)
                tasks[beachIndex] = new PassLegacy("Beaches", GenerateOcean);

			int chestIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Water Chests")); //Populate the ocean
			if (chestIndex != -1)
				tasks.Insert(chestIndex + 1, new PassLegacy("Populate Ocean", GenerateOceanObjects));
		}
	}

	#region basic shape
	/// <summary>Generates the Ocean ("Beaches"). Heavily based on vanilla code.</summary>
	public static void GenerateOcean(GenerationProgress progress, GameConfiguration config)
	{
		progress.Message = Language.GetText("LegacyWorldGen.22").Value;

		LeftOcean = GenerateSide(true);
		RightOcean = GenerateSide(false);
	}

	private static Rectangle GenerateSide(bool leftSide)
	{
		int dungeonSide = (Main.dungeonX < Main.maxTilesX / 2) ? -1 : 1;
		int worldEdge = leftSide ? 0 : Main.maxTilesX - WorldGen.genRand.Next(125, 200) - 50;
		int initialWidth = leftSide ? WorldGen.genRand.Next(125, 200) + 50 : Main.maxTilesX; //num468
		int tilesFromInnerEdge = 0;

		PiecewiseVScale = 1f + WorldGen.genRand.Next(-1000, 2500) * 0.0001f;
		PiecewiseVMountFactor = WorldGen.genRand.Next(150, 750);

		if (leftSide)
		{
			if (dungeonSide == 1)
				initialWidth = 275;

			int oceanTop;
			for (oceanTop = 0; !Main.tile[initialWidth - 1, oceanTop].HasTile; oceanTop++)
			{ } //Get top of ocean

			GenVars.shellStartXLeft = GenVars.leftBeachEnd - 30 - WorldGen.genRand.Next(15, 30);
			GenVars.shellStartYLeft = oceanTop;

			oceanTop += WorldGen.genRand.Next(1, 5);
			for (int placeX = initialWidth - 1; placeX >= worldEdge; placeX--)
				GenSingleOceanSingleStep(oceanTop, placeX, ref tilesFromInnerEdge);

			return new Rectangle(worldEdge, oceanTop - 5, initialWidth, (int)GetOceanSlope(tilesFromInnerEdge) + 20);
		}
		else
		{
			if (dungeonSide == -1)
				worldEdge = Main.maxTilesX - 275;

			int oceanTop;
			for (oceanTop = 0; !Main.tile[worldEdge - 1, oceanTop].HasTile; oceanTop++)
			{ } //Get top of ocean

			GenVars.shellStartXRight = GenVars.rightBeachStart + 30 + WorldGen.genRand.Next(15, 30);
			GenVars.shellStartYRight = oceanTop;

			oceanTop += WorldGen.genRand.Next(1, 5);
			for (int placeX = worldEdge; placeX < initialWidth; placeX++) //repeat X loop
				GenSingleOceanSingleStep(oceanTop, placeX, ref tilesFromInnerEdge);

			return new Rectangle(worldEdge, oceanTop - 5, initialWidth - worldEdge, (int)GetOceanSlope(tilesFromInnerEdge) + 20);
		}
	}

	private static void GenSingleOceanSingleStep(int oceanTop, int placeX, ref int tilesFromInnerEdge)
	{
		tilesFromInnerEdge++;

		float depth = GetOceanSlope(tilesFromInnerEdge);
		depth += OceanSlopeRoughness();

		int thickness = WorldGen.genRand.Next(20, 28); //Sand lining is a bit thicker than vanilla
		bool passedTile = false;

		for (int placeY = 0; placeY < oceanTop + depth + thickness; placeY++)
		{
			bool liq = PlaceTileOrLiquid(placeX, placeY, oceanTop, depth);

			if (!passedTile && !Framing.GetTileSafely(placeX, placeY + 1).HasTile)
			{
				if (!liq)
					thickness++;
			}
			else
			{
				passedTile = true;
			}
		}
	}
	#endregion

	public static void GenerateOceanObjects(GenerationProgress progress, GameConfiguration config)
	{
		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.PopulateOcean");

		PlaceOceanPendant();

		if (LeftOcean == Rectangle.Empty || RightOcean == Rectangle.Empty)
		{
			LeftOcean = new Rectangle(40, (int)Main.worldSurface - 200, WorldGen.beachDistance, 200);
			RightOcean = new Rectangle(Main.maxTilesX - 40 - WorldGen.beachDistance, (int)Main.worldSurface - 200, WorldGen.beachDistance, 200);
		} //Fallback if the ocean shape didn't generate

		Rectangle[] regions = [LeftOcean, RightOcean];
		foreach (Rectangle region in regions)
		{
			WorldMethods.Generate(GenerateGravel, 5, out _, region);

			GenPirateChest(region); //See OceanGeneration.Classic

			WorldMethods.Generate(CreateWaterChest, WorldGen.genRand.Next(1, 4), out _, region, 50);
			WorldMethods.Generate(CreateSunkenTreasure, WorldGen.genRand.Next(2, 4), out _, region, 100);
			WorldMethods.GenerateSquared(CreateDeco, out _, region);
		}
	}

	/// <summary> Places additional water chests in the inner ocean. </summary>
	public static bool CreateWaterChest(int i, int j)
	{
		const int curveLength = 120;

		if (!FindSurface(i, ref j) || TilesFromEdge(i) is int inner && inner > curveLength)
			return false;

		if (WorldMethods.AreaClear(i, j - 1, 2, 2) && WorldMethods.Submerged(i, j - 1, 2, 2) && GenVars.structures.CanPlace(new Rectangle(i, j - 1, 2, 2)))
		{
			WorldGen.KillTile(i, j + 1, false, false, true);
			WorldGen.KillTile(i + 1, j + 1, false, false, true);

			WorldGen.PlaceTile(i, j + 1, TileID.Sand, true, false);
			WorldGen.PlaceTile(i + 1, j + 1, TileID.Sand, true, false);

			int contain = WorldGen.genRand.NextFromList(new short[5] { 863, 186, 277, 187, 4404 });
			WorldGen.AddBuriedChest(i + 1, j, contain, Style: (int)Common.WorldGeneration.Chests.VanillaChestID.Water);

			return true;
		}

		return false;
	}

	private static bool CreateSunkenTreasure(int i, int j)
	{
		if (!FindSurface(i, ref j) || TilesFromEdge(i) is int inner && inner < 100)
			return false;

		if (WorldMethods.AreaClear(i - 1, j - 1, 3, 2))
		{
			int type = ModContent.TileType<SunkenTreasureTile>();
			WorldGen.PlaceTile(i, j, type, true);

			if (Main.tile[i, j].TileType == type)
				return true;
		}

		return false;
	}

	private static bool CreateDeco(int i, int j)
	{
		if (!ValidCoords(i, j))
			return false;

		int tilesFromEdge = TilesFromEdge(i);

		//Coral multitiles
		int coralChance = (tilesFromEdge < 133) ? 15 : ((tilesFromEdge < 161) ? 27 : 0); //First slope (I hope)
		if (coralChance > 0)
		{
			if (WorldGen.genRand.NextBool((int)(coralChance * 1.25f)) && Placer.Check(i, j, ModContent.TileType<Coral3x3>()).IsClear().Place().success)
				return true;

			if (WorldGen.genRand.NextBool(coralChance) && Placer.Check(i, j, ModContent.TileType<Coral2x2>()).IsClear().Place().success)
				return true;

			if (WorldGen.genRand.NextBool((int)(coralChance * 1.75f)) && Placer.Check(i, j, ModContent.TileType<Coral1x2>()).IsClear().Place().success)
				return true;
		}

		//Decor multitiles
		int decorChance = (tilesFromEdge < 100) ? 14 : 35; //Higher on first slope, then less common
		if (decorChance > 0)
		{
			if (WorldGen.genRand.NextBool(decorChance) && Placer.Check(i, j, ModContent.TileType<OceanDecor2x3>()).IsClear().Place().success)
				return true;

			if (WorldGen.genRand.NextBool(decorChance) && Placer.Check(i, j, ModContent.TileType<OceanDecor2x2>()).IsClear().Place().success)
				return true;

			if (WorldGen.genRand.NextBool(decorChance) && Placer.Check(i, j, ModContent.TileType<OceanDecor1x2>()).IsClear().Place().success)
				return true;
		}

		//Kelp
		if (tilesFromEdge < 133 && WorldGen.genRand.NextBool(3, 6))
		{
			if (WorldGen.genRand.NextBool(3)) //Occasionally solidify ground slopes
			{
				Framing.GetTileSafely(i, j + 1).Clear(Terraria.DataStructures.TileDataType.Slope);
			}

			int height = WorldGen.genRand.Next(4, 14);
			for (int h = 0; h < height; h++)
			{
				Tile tile = Framing.GetTileSafely(i, j - h);
				int type = ModContent.TileType<OceanKelp>();

				WorldGen.PlaceTile(i, j - h, type);

				if (tile.HasTile && tile.TileType == type)
				{
					if (h < height / 2)
						tile.TileFrameY += 198;
				}
				else
				{
					break;
				}
			}
		}

		return true;
	}

	private static void PlaceOceanPendant()
	{
		const int attempts = 500;

		for (int a = 0; a < attempts; a++)
		{
			int x = WorldGen.genRand.Next(40, WorldGen.oceanDistance);
			if (WorldGen.genRand.NextBool())
				x = WorldGen.genRand.Next(Main.maxTilesX - WorldGen.oceanDistance, Main.maxTilesX - 40);

			int y = WorldGen.genRand.Next((int)(Main.maxTilesY * 0.35f / 16f), (int)WorldGen.oceanLevel);

			if (!FindSurface(x, ref y))
				continue;

			Tile tile = Framing.GetTileSafely(x, y);
			if (tile.LiquidType == LiquidID.Water && tile.LiquidAmount == 255 && Placer.PlaceTile(x, y, ModContent.TileType<OceanPendantTile>()).success)
				break;
		}
	}

	#region magmastone
	public static bool GenerateGravel(int x, int y)
	{
		if (!WorldGen.InWorld(x, y, 20))
			return false;

		WorldMethods.FindGround(x, ref y);
		Tile above = Main.tile[x, y - 1];

		if (above.LiquidType == LiquidID.Water && above.LiquidAmount >= 200)
		{
			ShapeData data = new();
			int radius = WorldGen.genRand.Next(2, 8);

			WorldUtils.Gen(new(x, y), new Shapes.Circle(radius, (int)(radius * 0.75f)), Actions.Chain(
				new Modifiers.OnlyTiles(TileID.Sand),
				new Modifiers.Blotches(),
				new Actions.SetTileKeepWall((ushort)ModContent.TileType<Gravel>())
			).Output(data));

			WorldUtils.Gen(new(x, y), new ModShapes.OuterOutline(data), Actions.Chain(
				new Modifiers.OnlyTiles((ushort)ModContent.TileType<Gravel>()),
				new Modifiers.Dither(),
				new Actions.SetTileKeepWall((ushort)ModContent.TileType<Magmastone>())
			));

			WorldUtils.Gen(new(x, y), new ModShapes.OuterOutline(data), Actions.Chain(
				new Modifiers.Blotches(),
				new Modifiers.OnlyTiles(TileID.Sand),
				new Actions.SetTileKeepWall(TileID.HardenedSand)
			));

			new Decorator(new(x - radius, y - radius, radius * 2, radius * 2))
				.Enqueue(CreateStack, radius / 2)
				.Enqueue(ModContent.TileType<GravelPile>(), radius / 2)
				.Enqueue(ModContent.TileType<GravelStalagmite>(), radius / 3)
				.Run();

			return true;
		}

		return false;
	}

	private static bool CreateStack(int x, int y)
	{
		WorldMethods.FindGround(x, ref y);
		PlaceAttempt attempt = Placer.Check(x, --y, ModContent.TileType<HydrothermalVent>()).IsClear();

		if (attempt.success)
		{
			int height = WorldGen.genRand.Next(0, 3);
			WorldUtils.Gen(new(x, y - height + 1), new Shapes.Rectangle(2, height), new Actions.SetTileKeepWall((ushort)ModContent.TileType<Gravel>()));

			attempt.data.yCoord -= height; //Offset by height
			attempt.Place();

			return true;
		}

		return false;
	}
	#endregion

	#region general helpers
	private static bool RightSide(int x) => x > Main.maxTilesX / 2;
	private static int TilesFromEdge(int x) => RightSide(x) ? (x - RightOcean.Left) : (LeftOcean.Right - x);

	/// <summary> Returns whether the provided coordinates are valid for sandy decoration tiles. </summary>
	private static bool ValidCoords(int i, int j)
	{
		Tile tile = Main.tile[i, j];
		Tile below = Framing.GetTileSafely(i, j + 1);

		return tile.LiquidAmount == 255 && !tile.HasTile && below.HasTile && below.TileType == TileID.Sand;
	}

	private static bool FindSurface(int i, ref int j)
	{
		WorldMethods.FindGround(i, ref j);
		Point pt = new(i, --j);

		if (!ValidCoords(i, j) || !LeftOcean.Contains(pt) && !RightOcean.Contains(pt))
			return false;

		return true;
	}

	public static bool PlaceChest(int x, int y, int type, (int, int)[] mainItems, (int, int)[] subItems, bool noTypeRepeat = true, UnifiedRandom r = null, int subItemLength = 6, int style = 0, bool overRide = false, int width = 2, int height = 2)
	{
		r ??= Main.rand;

		if (overRide)
			for (int i = x; i < x + width; ++i)
				for (int j = y; j < y + height; ++j)
					WorldGen.KillTile(i, j - 1, false, false, true);

		int ChestIndex = WorldGen.PlaceChest(x, y, (ushort)type, false, style);
		if (ChestIndex != -1 && Main.tile[x, y].TileType == type)
		{
			int main = r.Next(mainItems.Length);
			Main.chest[ChestIndex].item[0].SetDefaults(mainItems[main].Item1);
			Main.chest[ChestIndex].item[0].stack = mainItems[main].Item2;

			int reps = 0;
			var usedTypes = new List<int>();

			for (int i = 0; i < subItemLength; ++i)
			{
				repeat:
				if (reps > 50)
				{
					SpiritReforgedMod.Instance.Logger.Info("WARNING: Attempted to repeat item placement too often. Report to dev. [SpiritReforged]");
					break;
				}

				int sub = r.Next(subItems.Length);
				int itemType = subItems[sub].Item1;
				int itemStack = subItems[sub].Item2;

				if (noTypeRepeat && usedTypes.Contains(itemType))
					goto repeat;

				usedTypes.Add(itemType);

				Main.chest[ChestIndex].item[i + 1].SetDefaults(itemType);
				Main.chest[ChestIndex].item[i + 1].stack = itemStack;
			}

			return true;
		}

		return false;
	}
	#endregion

	private static float OceanSlopeRoughness()
	{
		_roughTimer--;

		if (_roughTimer <= 0)
		{
			_roughTimer = WorldGen.genRand.Next(5, 9);
			_rough += WorldGen.genRand.NextFloat(0.6f, 1f) * (WorldGen.genRand.NextBool(2) ? -1 : 1);
		}

		return _rough;
	}

	private static bool PlaceTileOrLiquid(int placeX, int placeY, int oceanTop, float depth)
	{
		if (placeY < oceanTop + depth - 3f)
		{
			Tile tile = Main.tile[placeX, placeY];
			tile.HasTile = false;

			if (placeY > oceanTop + 5)
				Main.tile[placeX, placeY].LiquidAmount = byte.MaxValue;
			else if (placeY == oceanTop + 5)
				Main.tile[placeX, placeY].LiquidAmount = 127;

			Main.tile[placeX, placeY].WallType = 0;
			return true;
		}
		else if (placeY > oceanTop)
		{
			Tile tile = Main.tile[placeX, placeY];
			if (placeY < oceanTop + depth + 8)
				Main.tile[placeX, placeY].TileType = TileID.Sand;
			else
				Main.tile[placeX, placeY].TileType = TileID.HardenedSand;
			tile.HasTile = true;
		}

		Main.tile[placeX, placeY].WallType = WallID.None;
		return false;
	}

	/// <summary>Gets the slope of the ocean. Reference: <seealso cref="https://www.desmos.com/calculator/xfnsmar79x"/></summary>
	/// <param name="tilesFromInnerEdge"></param>
	private static float GetOceanSlope(int tilesFromInnerEdge)
	{
		OceanShape shape = ModContent.GetInstance<ReforgedClientConfig>().OceanShape;

		if (shape == OceanShape.SlantedSine)
		{
			const int SlopeSize = 15;
			const float Steepness = 0.8f;

			//(s_0s_1)sin(1/s_0 x) + (s_1)x
			return tilesFromInnerEdge > 234
				? SlopeSize * Steepness * (float)Math.Sin(1f / SlopeSize * 234) + Steepness * 234
				: SlopeSize * Steepness * (float)Math.Sin(1f / SlopeSize * tilesFromInnerEdge) + Steepness * tilesFromInnerEdge;
		}
		else if (shape == OceanShape.Piecewise)
		{
			if (tilesFromInnerEdge < 75)
				return 1 / 75f * tilesFromInnerEdge * tilesFromInnerEdge;
			else if (tilesFromInnerEdge < 125)
				return 75;
			else 
				return tilesFromInnerEdge < 175 ? 1 / 50f * (float)Math.Pow(tilesFromInnerEdge - 125, 2) + 75 : 125;
		}
		else if (shape == OceanShape.Piecewise_M)
		{
			const float CubicMultiplier = 37.5f;
			const float CubicMultiplierSq = CubicMultiplier * CubicMultiplier;

			if (tilesFromInnerEdge < 75)
				return 1 / CubicMultiplierSq * (float)Math.Pow(tilesFromInnerEdge - CubicMultiplier, 3) + CubicMultiplier;
			else if (tilesFromInnerEdge < 125)
				return 75;
			else 
				return tilesFromInnerEdge < 175 ? 1 / 50f * (float)Math.Pow(tilesFromInnerEdge - 125, 2) + 75 : 125;
		}
		else
		{
			float Scale = PiecewiseVScale; //m_s
			const float Steepness = 25f; //m_c

			float FirstSlope(float x) => -Scale * (1 / (Steepness * Steepness)) * (float)Math.Pow(0.6f * x - Steepness, 3) - Scale * Steepness;
			float SecondSlope(float x) => -Scale * (1 / (2 * (Steepness * Steepness))) * (float)Math.Pow(x - 75 - Steepness, 3) + (float)Math.Pow(x - 80, 2) / PiecewiseVMountFactor + FirstSlope(83.33f);
			float LastSlope(int x) => Scale * (1 / Steepness) * (float)Math.Pow(x - 160, 2) + SecondSlope(141.7f);

			float returnValue;
			if (tilesFromInnerEdge < 75)
				returnValue = FirstSlope(tilesFromInnerEdge);
			else if (tilesFromInnerEdge < 133)
				returnValue = SecondSlope(tilesFromInnerEdge);
			else if (tilesFromInnerEdge < 161)
				returnValue = LastSlope(tilesFromInnerEdge);
			else
				returnValue = LastSlope(160);

			return -returnValue;
		}
	}
}