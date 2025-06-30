using Terraria.WorldBuilding;
using Terraria.IO;
using Terraria.GameContent.Generation;
using SpiritReforged.Content.Ocean.Tiles;
using SpiritReforged.Common.ConfigurationCommon;
using SpiritReforged.Content.Ocean.Items;
using SpiritReforged.Common.WorldGeneration;
using Terraria.Utilities;
using SpiritReforged.Common.ModCompat;

namespace SpiritReforged.Content.Ocean;

public class OceanGeneration : ModSystem
{
	private static float PiecewiseVScale = 1f;
	private static float PiecewiseVMountFactor = 1f;

	private static int _roughTimer = 0;
	private static float _rough = 0f;
	private static (Rectangle, Rectangle) _oceanInfos = (new Rectangle(), new Rectangle());

	public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
	{
        if (ModContent.GetInstance<ReforgedClientConfig>().OceanShape != OceanShape.Default)
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

		void GenSingleOceanSingleStep(int oceanTop, int placeX, ref int tilesFromInnerEdge)
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
					passedTile = true;
			}
		}

		void CheckOceanHeight(ref int height)
		{
			float depth = GetOceanSlope(250);

			do
			{
				height--;
			} while (height + depth + 20 > Main.worldSurface + 50);
		}

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
				CheckOceanHeight(ref oceanTop);

				oceanTop += WorldGen.genRand.Next(1, 5);
				for (int placeX = initialWidth - 1; placeX >= worldEdge; placeX--)
					GenSingleOceanSingleStep(oceanTop, placeX, ref tilesFromInnerEdge);

				_oceanInfos.Item1 = new Rectangle(worldEdge, oceanTop - 5, initialWidth, (int)GetOceanSlope(tilesFromInnerEdge) + 20);
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
				CheckOceanHeight(ref oceanTop);

				oceanTop += WorldGen.genRand.Next(1, 5);
				for (int placeX = worldEdge; placeX < initialWidth; placeX++) //repeat X loop
					GenSingleOceanSingleStep(oceanTop, placeX, ref tilesFromInnerEdge);

				_oceanInfos.Item2 = new Rectangle(worldEdge, oceanTop - 5, initialWidth - worldEdge, (int)GetOceanSlope(tilesFromInnerEdge) + 20);
			}
		}
	}

	public static void GenerateOceanObjects(GenerationProgress progress, GameConfiguration config)
	{
		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.PopulateOcean");

		PlaceOceanPendant();

		if (_oceanInfos.Item1 == Rectangle.Empty || _oceanInfos.Item2 == Rectangle.Empty)
		{
			_oceanInfos.Item1 = new Rectangle(40, (int)Main.worldSurface - 200, WorldGen.beachDistance, 200);
			_oceanInfos.Item2 = new Rectangle(Main.maxTilesX - 40 - WorldGen.beachDistance, (int)Main.worldSurface - 200, WorldGen.beachDistance, 200);
		}

		PlaceWaterChests(_oceanInfos.Item1, _oceanInfos.Item2);

		PopulateOcean(_oceanInfos.Item1, 0);
		PopulateOcean(_oceanInfos.Item2, 1);
	}

	private static void PopulateOcean(Rectangle bounds, int side)
	{
		if (CrossMod.Classic.Enabled)
			PlacePirateChest(side == 0 ? bounds.Right : bounds.Left, side);

		PlaceSunkenTreasure(side == 0 ? bounds.Right : bounds.Left, side);

		for (int i = bounds.Left; i < bounds.Right; ++i)
		{
			for (int j = bounds.Top; j < bounds.Bottom; ++j)
			{
				if (Framing.GetTileSafely(i, j - 1).LiquidAmount < 155 || Framing.GetTileSafely(i, j).LiquidAmount > 0)
					continue; //Quick validity check

				int tilesFromInnerEdge = bounds.Right - i;
				if (side == 1)
					tilesFromInnerEdge = i - bounds.Left;

				int coralChance = 0;
				if (tilesFromInnerEdge < 133) //First slope (I hope)
					coralChance = 15;
				else if (tilesFromInnerEdge < 161)
					coralChance = 27;

				//Coral multitiles
				if (coralChance > 0 && WorldGen.genRand.NextBool((int)(coralChance * 1.25f)) && TryPlaceSubmerged(i, j, ModContent.TileType<Coral3x3>()))
					continue;

				if (coralChance > 0 && WorldGen.genRand.NextBool(coralChance) && TryPlaceSubmerged(i, j, ModContent.TileType<Coral2x2>(), WorldGen.genRand.Next(3)))
					continue;

				if (coralChance > 0 && WorldGen.genRand.NextBool((int)(coralChance * 1.75f)) && TryPlaceSubmerged(i, j, ModContent.TileType<Coral1x2>()))
					continue;

				//Decor multitiles
				int kelpChance = tilesFromInnerEdge < 100 ? 14 : 35; //Higher on first slope, then less common
				if (kelpChance > 0 && WorldGen.genRand.NextBool(kelpChance) && TryPlaceSubmerged(i, j, ModContent.TileType<OceanDecor2x3>(), WorldGen.genRand.Next(2)))
					continue;

				if (kelpChance > 0 && WorldGen.genRand.NextBool(kelpChance) && TryPlaceSubmerged(i, j, ModContent.TileType<OceanDecor2x2>(), WorldGen.genRand.Next(4)))
					continue;

				if (kelpChance > 0 && WorldGen.genRand.NextBool(kelpChance) && TryPlaceSubmerged(i, j, ModContent.TileType<OceanDecor1x2>(), WorldGen.genRand.Next(3)))
					continue;

				//Growing kelp
				if (WorldGen.genRand.NextBool(3, 6) && tilesFromInnerEdge < 133 && Main.tile[i, j].TileType == TileID.Sand && !Main.tile[i, j - 1].HasTile)
					GrowKelp(i, j - 1);
			}
		}
	}

	/// <summary> Places <paramref name="type"/> above the given coordinates if completely submerged. </summary>
	private static bool TryPlaceSubmerged(int i, int j, int type, int style = 0)
	{
		var data = TileObjectData.GetTileData(type, style);

		if (data != null && WorldMethods.Submerged(i, j - data.Height, data.Width, data.Height))
		{
			WorldGen.PlaceObject(i, j - 1, type, true, style);
			return true;
		}

		return false;
	}

	private static void GrowKelp(int i, int j)
	{
		//Occasionally solidify ground slopes
		if (WorldGen.genRand.NextBool(3))
		{
			Framing.GetTileSafely(i, j + 1).Slope = SlopeType.Solid;
			Framing.GetTileSafely(i, j + 1).IsHalfBlock = false;
		}

		int height = WorldGen.genRand.Next(4, 14);

		for (int y = j; y > j - height; --y)
		{
			if (Main.tile[i, y].LiquidAmount < 255)
				break;

			WorldGen.PlaceTile(i, y, ModContent.TileType<OceanKelp>());

			if (y > j - height / 2)
			{
				Tile tile = Main.tile[i, y];
				tile.TileFrameY += 198;
			}
		}
	}

	private static void PlaceSunkenTreasure(int innerEdge, int side)
	{
		const int maxTries = 100;
		int countMax = WorldGen.genRand.Next(2, 4);
		int count = 0;

		for (int t = 0; t < maxTries; t++)
		{
			int sunkenX = innerEdge - WorldGen.genRand.Next(133, innerEdge - 40);
			if (side == 1)
				sunkenX = innerEdge + WorldGen.genRand.Next(133, Main.maxTilesX - innerEdge - 40);

			var pos = new Point(sunkenX, (int)(Main.maxTilesY * 0.35f / 16f));

			if (!ScanDown(pos.X, ref pos.Y))
				continue;

			if (WorldMethods.AreaClear(pos.X - 1, pos.Y - 2, 3, 2))
			{
				int type = ModContent.TileType<SunkenTreasureTile>();
				WorldGen.PlaceTile(pos.X, pos.Y - 1, type, true);

				if (Main.tile[pos.X, pos.Y - 1].TileType == type && ++count >= countMax)
					break;
			}
		}
	}

	public static void PlacePirateChest(int innerEdge, int side)
	{
		const int maxTries = 200;

		if (!CrossMod.Classic.TryFind("DuelistLegacy", out ModItem duelist) || !CrossMod.Classic.TryFind("LadyLuck", out ModItem ladyLuck))
			return;

		for (int t = 0; t < maxTries; t++)
		{
			int x = innerEdge - WorldGen.genRand.Next(100, innerEdge - 60);
			if (side == 1)
				x = innerEdge + WorldGen.genRand.Next(100, Main.maxTilesX - innerEdge - 60);

			int y = (int)(Main.maxTilesY * 0.35f / 16f);

			if (!ScanDown(x, ref y))
				continue;

			if (WorldMethods.AreaClear(x, y - 2, 2, 2) && GenVars.structures.CanPlace(new Rectangle(x, y - 2, 2, 2)))
			{
				for (int w = 0; w < 4; w++)
				{
					int i = x + w % 2;
					int j = y + w / 2;

					WorldGen.KillTile(i, j, false, noItem: true);
					WorldGen.PlaceTile(i, j, TileID.HardenedSand, true);
				}

				PlaceChest(x, y - 1, ModContent.TileType<PirateChest>(),
					[
						(side == 0 ? ladyLuck.Type : duelist.Type, 1)
					],
					[
						(ItemID.GoldCoin, WorldGen.genRand.Next(12, 30)), (ItemID.Diamond, WorldGen.genRand.Next(12, 30)), (ItemID.GoldCrown, 1), (ItemID.GoldDust, WorldGen.genRand.Next(1, 3)),
						(ItemID.GoldChest, 1), (ItemID.GoldenChair, 1), (ItemID.GoldChandelier, 1), (ItemID.GoldenPlatform, WorldGen.genRand.Next(12, 18)), (ItemID.GoldenSink, 1), (ItemID.GoldenSofa, 1),
						(ItemID.GoldenTable, 1), (ItemID.GoldenToilet, 1), (ItemID.GoldenWorkbench, 1), (ItemID.GoldenPiano, 1), (ItemID.GoldenLantern, 1), (ItemID.GoldenLamp, 1), (ItemID.GoldenDresser, 1),
						(ItemID.GoldenDoor, 1), (ItemID.GoldenCrate, 1), (ItemID.GoldenClock, 1), (ItemID.GoldenChest, 1), (ItemID.GoldenCandle, WorldGen.genRand.Next(2, 4)), (ItemID.GoldenBookcase, 1),
						(ItemID.TitaniumBar, BarStack()), (ItemID.PalladiumBar, BarStack()), (ItemID.OrichalcumBar, BarStack())
					],
					true, WorldGen.genRand, WorldGen.genRand.Next(15, 21), 1, true, 2, 2);

				break;
			}
		}

		static int BarStack() => WorldGen.genRand.Next(3, 7);
	}

	/// <summary> Places additional water chests in the inner ocean. </summary>
	/// <param name="leftBounds"> The left ocean bounds. </param>
	/// <param name="rightBounds"> The right ocean bounds. </param>
	public static void PlaceWaterChests(Rectangle leftBounds, Rectangle rightBounds)
	{
		const int maxTries = 100;

		int count = 0;
		int countMax = WorldGen.genRand.Next(3, 6);

		for (int i = 0; i < maxTries; i++)
		{
			int x = GetBound();
			int y = (int)(Main.maxTilesY * 0.35f / 16f);

			if (!ScanDown(x, ref y))
				continue;

			if (WorldMethods.AreaClear(x, y - 2, 2, 2) && WorldMethods.Submerged(x, y - 2, 2, 2) && GenVars.structures.CanPlace(new Rectangle(x, y - 2, 2, 2)))
			{
				for (int w = 0; w < 2; w++)
				{
					WorldGen.KillTile(x + w, y, false, false, true);
					WorldGen.PlaceTile(x + w, y, TileID.Sand, true, false);
				}

				int contain = WorldGen.genRand.NextFromList(new short[5] { 863, 186, 277, 187, 4404 });
				WorldGen.AddBuriedChest(x, y - 1, contain, Style: (int)Common.WorldGeneration.Chests.VanillaChestID.Water);

				if (++count >= countMax)
					break;
			}
		}

		int GetBound()
		{
			const int length = 120;

			if (WorldGen.genRand.NextBool())
			{
				int start = rightBounds.Left;
				return WorldGen.genRand.Next(start, Math.Min(start + length, Main.maxTilesX - 20));
			}
			else
			{
				int end = leftBounds.Right;
				return WorldGen.genRand.Next(Math.Max(end - length, 20), end);
			}
		}
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
				{
				}

				WorldGen.oceanCave(i, j);
			}
		}
	}

	private static void PlaceOceanPendant()
	{
		while (true)
		{
			int x = WorldGen.genRand.Next(40, WorldGen.oceanDistance);
			if (WorldGen.genRand.NextBool())
				x = WorldGen.genRand.Next(Main.maxTilesX - WorldGen.oceanDistance, Main.maxTilesX - 40);

			int y = WorldGen.genRand.Next((int)(Main.maxTilesY * 0.35f / 16f), (int)WorldGen.oceanLevel);

			if (!ScanDown(x, ref y))
				continue;

			y--;
			if (Framing.GetTileSafely(x, y).LiquidType == LiquidID.Water && Framing.GetTileSafely(x, y).LiquidAmount >= 255)
			{
				int type = ModContent.TileType<Items.Reefhunter.OceanPendant.OceanPendantTile>();

				WorldGen.PlaceObject(x, y, type);
				if (Framing.GetTileSafely(x, y).TileType == type)
					break;
			}
		}
	}

	/// <summary> Moves down to the next solid tile. </summary>
	/// <returns> Whether the final coordinates are safely in world bounds. </returns>
	private static bool ScanDown(int x, ref int y)
	{
		const int fluff = 10;

		while (WorldGen.InWorld(x, y, fluff) && !WorldGen.SolidTile(x, y))
			y++;

		return WorldGen.InWorld(x, y, fluff);
	}

	public enum OceanShape
	{
		Default = 0, //vanilla worldgen
		SlantedSine, //Yuyu's initial sketch
		Piecewise, //Musicano's original sketch
		Piecewise_M, //Musicano's sketch with Sal/Yuyu's cubic modification
		Piecewise_V, //My heavily modified piecewise with variable height
	}
}