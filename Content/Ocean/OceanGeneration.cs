using SpiritReforged.Common.ConfigurationCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.Ocean.Items;
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

	private static Rectangle _leftOcean;
	private static Rectangle _rightOcean;

	public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
	{
        if (ModContent.GetInstance<ReforgeClientConfig>().OceanShape != OceanShape.Default)
        {
            int beachIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Beaches")); //Replace beach gen
            if (beachIndex != -1)
                tasks[beachIndex] = new PassLegacy("Beaches", GenerateOcean);

			int cavesIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Create Ocean Caves")); //Replace ocean cave gen
			if (cavesIndex != -1)
				tasks[cavesIndex] = new PassLegacy("Create Ocean Caves", GenerateOceanCaves);

			int chestIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Water Chests")); //Populate the ocean
			if (chestIndex != -1)
				tasks.Insert(chestIndex + 1, new PassLegacy("Populate Ocean", GenerateOceanObjects));
		}
	}

	/// <summary>Generates the Ocean ("Beaches"). Heavily based on vanilla code.</summary>
	/// <param name="progress"></param>
	public static void GenerateOcean(GenerationProgress progress, GameConfiguration config)
	{
		//Basic Shape
		progress.Message = Language.GetText("LegacyWorldGen.22").Value;
		int dungeonSide = Main.dungeonX < Main.maxTilesX / 2 ? -1 : 1;

		for (int side = 0; side < 2; side++)
		{
			PiecewiseVScale = 1f + WorldGen.genRand.Next(-1000, 2500) * 0.0001f;
			PiecewiseVMountFactor = WorldGen.genRand.Next(150, 750);

			int worldEdge = side == 0 ? 0 : Main.maxTilesX - WorldGen.genRand.Next(125, 200) - 50;
			int initialWidth = side == 0 ? WorldGen.genRand.Next(125, 200) + 50 : Main.maxTilesX; //num468
			int tilesFromInnerEdge = 0;

			if (side == 0)
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

				_leftOcean = new Rectangle(worldEdge, oceanTop - 5, initialWidth, (int)GetOceanSlope(tilesFromInnerEdge) + 20);
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

				_rightOcean = new Rectangle(worldEdge, oceanTop - 5, initialWidth - worldEdge, (int)GetOceanSlope(tilesFromInnerEdge) + 20);
			}
		}

		static void GenSingleOceanSingleStep(int oceanTop, int placeX, ref int tilesFromInnerEdge)
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
	}

	public static void GenerateOceanObjects(GenerationProgress progress, GameConfiguration config)
	{
		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.PopulateOcean");

		PlaceOceanPendant();

		if (_leftOcean == Rectangle.Empty || _rightOcean == Rectangle.Empty)
		{
			_leftOcean = new Rectangle(40, (int)Main.worldSurface - 200, WorldGen.beachDistance, 200);
			_rightOcean = new Rectangle(Main.maxTilesX - 40 - WorldGen.beachDistance, (int)Main.worldSurface - 200, WorldGen.beachDistance, 200);
		}

		for (int i = 0; i < 2; i++)
		{
			Rectangle area = (i == 0) ? _leftOcean : _rightOcean;

			GenPirateChest(area); //See OceanGeneration.Classic

			WorldMethods.Generate(CreateWaterChests, WorldGen.genRand.Next(1, 4), out _, area, 50);
			WorldMethods.Generate(CreateSunkenTreasure, WorldGen.genRand.Next(2, 4), out _, area, 100);
			WorldMethods.GenerateSquared(CreateDeco, out _, area);
		}
	}

	/// <summary> Places additional water chests in the inner ocean. </summary>
	public static bool CreateWaterChests(int i, int j)
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
				var t = Main.tile[i, j - h];

				if (t.HasTile || t.LiquidAmount < 255)
					break;

				int type = ModContent.TileType<OceanKelp>();
				WorldGen.PlaceTile(i, j - h, type);

				if (h < height / 2 && t.TileType == type)
					t.TileFrameY += 198;
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

			if (Framing.GetTileSafely(x, y).LiquidType == LiquidID.Water && Framing.GetTileSafely(x, y).LiquidAmount == 255)
			{
				int type = ModContent.TileType<Items.Reefhunter.OceanPendant.OceanPendantTile>();

				WorldGen.PlaceObject(x, y, type);
				if (Framing.GetTileSafely(x, y).TileType == type)
					break;
			}
		}
	}

	#region general helpers
	private static bool RightSide(int x) => x > Main.maxTilesX / 2;
	private static int TilesFromEdge(int x) => RightSide(x) ? (x - _rightOcean.Left) : (_leftOcean.Right - x);
	private static bool ValidCoords(int i, int j) => Main.tile[i, j].LiquidAmount == 255 && !Main.tile[i, j].HasTile && Main.tile[i, j + 1].HasTile && Main.tile[i, j + 1].TileType == TileID.Sand;

	private static bool FindSurface(int i, ref int j)
	{
		WorldMethods.FindGround(i, ref j);
		Point pt = new(i, --j);

		if (!ValidCoords(i, j) || !_leftOcean.Contains(pt) && !_rightOcean.Contains(pt))
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

		Main.tile[placeX, placeY].WallType = 0;
		return false;
	}

	/// <summary>Gets the slope of the ocean. Reference: <seealso cref="https://www.desmos.com/calculator/xfnsmar79x"/></summary>
	/// <param name="tilesFromInnerEdge"></param>
	private static float GetOceanSlope(int tilesFromInnerEdge)
	{
		OceanShape shape = ModContent.GetInstance<ReforgeClientConfig>().OceanShape;

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

	/// <summary>Generates ocean caves like vanilla does but with a guaranteed chance.</summary>
	public static void GenerateOceanCaves(GenerationProgress progress, GameConfiguration config)
	{
		for (int attempt = 0; attempt < 2; attempt++)
		{
			if ((attempt != 0 || GenVars.dungeonSide <= 0) && (attempt != 1 || GenVars.dungeonSide >= 0))
			{
				progress.Message = Lang.gen[90].Value;
				int i = WorldGen.genRand.Next(55, 95);
				if (attempt == 1)
					i = WorldGen.genRand.Next(Main.maxTilesX - 95, Main.maxTilesX - 55);

				int j;
				for (j = 0; !Main.tile[i, j].HasTile; j++)
				{ }

				WorldGen.oceanCave(i, j);
			}
		}
	}
}