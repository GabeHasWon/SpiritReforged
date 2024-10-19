﻿using Terraria.Utilities;
using Terraria.WorldBuilding;
using Terraria.IO;
using Terraria.GameContent.Generation;
using SpiritReforged.Content.Ocean.Tiles;
using SpiritReforged.Common.ConfigurationCommon;
using SpiritReforged.Content.Ocean.Items;
using SpiritReforged.Common.WorldGeneration;

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
        if (ModContent.GetInstance<ReforgeClientConfig>().OceanShape != OceanShape.Default)
        {
            int beachIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Beaches")); //Replace beach gen
            if (beachIndex != -1)
                tasks[beachIndex] = new PassLegacy("Beaches", GenerateOcean);

			int cavesIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Create Ocean Caves")); //Replace ocean cave gen
			if (cavesIndex != -1)
				tasks[cavesIndex] = new PassLegacy("Create Ocean Caves", GenerateOceanCaves);

			int sandIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Remove Water From Sand")); //Populate the ocean
			if (sandIndex != -1)
				tasks.Insert(sandIndex + 1, new PassLegacy("Populate Ocean", GenerateOceanObjects));
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

		PopulateOcean(_oceanInfos.Item1, 0);
		PopulateOcean(_oceanInfos.Item2, 1);
	}

	private static void PopulateOcean(Rectangle bounds, int side)
	{
		static bool ValidGround(int i, int j, int width, int type = TileID.Sand)
		{
			for (int k = i; k < i + width; ++k)
			{
				Tile t = Framing.GetTileSafely(k, j);
				if (!t.HasTile || t.TileType != type || t.IsHalfBlock || t.TopSlope || !Main.tileSolid[t.TileType])
					return false;
			}

			return true;
		}

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
					coralChance = 10;
				else if (tilesFromInnerEdge < 161)
					coralChance = 22;

				//Coral multitiles
				if (coralChance > 0 && WorldGen.genRand.NextBool((int)(coralChance * 1.25f)))
				{
					WorldGen.PlaceObject(i, j - 1, ModContent.TileType<Coral3x3>(), true);
					continue;
				}

				if (coralChance > 0 && WorldGen.genRand.NextBool(coralChance))
				{
					WorldGen.PlaceObject(i, j - 1, ModContent.TileType<Coral2x2>(), true, WorldGen.genRand.Next(3));
					continue;
				}

				if (coralChance > 0 && WorldGen.genRand.NextBool((int)(coralChance * 1.75f)))
				{
					WorldGen.PlaceObject(i, j - 1, ModContent.TileType<Coral1x2>(), true);
					continue;
				}

				//Kelp multitiles
				int kelpChance = tilesFromInnerEdge < 100 ? 40 : 20; //Higher on first slope, then less common
				if (kelpChance > 0 && WorldGen.genRand.NextBool(kelpChance))
				{
					WorldGen.PlaceObject(i, j - 1, ModContent.TileType<Kelp2x3>(), true);
					continue;
				}

				if (kelpChance > 0 && WorldGen.genRand.NextBool(kelpChance))
				{
					WorldGen.PlaceObject(i, j - 1, ModContent.TileType<Kelp2x2>(), true);
					continue;
				}

				if (kelpChance > 0 && WorldGen.genRand.NextBool(kelpChance))
				{
					WorldGen.PlaceObject(i, j - 1, ModContent.TileType<Kelp1x2>(), true);
					continue;
				}

				//Growing kelp
				if (WorldGen.genRand.Next(5) < 2 && tilesFromInnerEdge < 133 && ValidGround(i, j, 1, TileID.Sand))
				{
					int height = WorldGen.genRand.Next(6, 23) + 2;
					int offset = 1;
					while (!Framing.GetTileSafely(i, j - offset).HasTile && Framing.GetTileSafely(i, j - offset).LiquidAmount == 255 && height > 0)
					{
						WorldGen.PlaceTile(i, j - offset++, ModContent.TileType<OceanKelp>(), true);
						height--;
					}
				}
			}
		}
	}

	private static void PlaceSunkenTreasure(int innerEdge, int side)
	{
		for (int i = 0; i < 2; ++i)
		{
			int sunkenX = innerEdge - WorldGen.genRand.Next(133, innerEdge - 40);
			if (side == 1)
				sunkenX = innerEdge + WorldGen.genRand.Next(133, Main.maxTilesX - innerEdge - 40);

			var pos = new Point(sunkenX, (int)(Main.maxTilesY * 0.35f / 16f));
			while (!WorldGen.SolidTile(pos.X, pos.Y))
				pos.Y++;

			for (int j = pos.X; j < pos.X + 3; ++j)
			{
				for (int k = pos.Y - 1; k < pos.Y; ++k)
					WorldGen.KillTile(j, k, false, false, true);
			}

			WorldGen.PlaceObject(pos.X, pos.Y - 1, ModContent.TileType<SunkenTreasureTile>());
		}
	}

	public static void PlacePirateChest(int innerEdge, int side)
	{
		int placementRetries = 0;

	retry:
		int guaranteeChestX = innerEdge - WorldGen.genRand.Next(100, innerEdge - 60);
		if (side == 1)
			guaranteeChestX = innerEdge + WorldGen.genRand.Next(100, Main.maxTilesX - innerEdge - 60);

		var chest = new Point(guaranteeChestX, (int)(Main.maxTilesY * 0.35f / 16f));
		while (!WorldGen.SolidTile(chest.X, chest.Y))
			chest.Y++;

		if (!WorldMethods.AreaClear(chest.X, chest.Y - 2, 2, 2))
			goto retry; //uh oh! goto! I'm a lazy programmer seethe & rage

		for (int i = 0; i < 2; ++i)
		{
			WorldGen.KillTile(chest.X + i, chest.Y, false, false, true);
			WorldGen.PlaceTile(chest.X + i, chest.Y , TileID.Sand, true, false);
			Framing.GetTileSafely(chest.X + i, chest.Y).Slope = 0;
		}

		static int BarStack() => WorldGen.genRand.Next(3, 7);

		placementRetries++;

		bool success = PlaceChest(chest.X, chest.Y - 1, ModContent.TileType<OceanPirateChest>(), 
			[
				(side == 0 ? ItemID.PirateStaff : ItemID.CoinGun, 1)
			], 
			[   
				(ItemID.GoldCoin, WorldGen.genRand.Next(12, 30)), (ItemID.Diamond, WorldGen.genRand.Next(12, 30)), (ItemID.GoldCrown, 1), (ItemID.GoldDust, WorldGen.genRand.Next(1, 3)),
				(ItemID.GoldChest, 1), (ItemID.GoldenChair, 1), (ItemID.GoldChandelier, 1), (ItemID.GoldenPlatform, WorldGen.genRand.Next(12, 18)), (ItemID.GoldenSink, 1), (ItemID.GoldenSofa, 1),
				(ItemID.GoldenTable, 1), (ItemID.GoldenToilet, 1), (ItemID.GoldenWorkbench, 1), (ItemID.GoldenPiano, 1), (ItemID.GoldenLantern, 1), (ItemID.GoldenLamp, 1), (ItemID.GoldenDresser, 1),
				(ItemID.GoldenDoor, 1), (ItemID.GoldenCrate, 1), (ItemID.GoldenClock, 1), (ItemID.GoldenChest, 1), (ItemID.GoldenCandle, WorldGen.genRand.Next(2, 4)), (ItemID.GoldenBookcase, 1),
				(ItemID.GoldenBed, 1), (ItemID.GoldenBathtub, 1), (ItemID.MythrilBar, BarStack()), (ItemID.AdamantiteBar, BarStack()), (ItemID.CobaltBar, BarStack()),
				(ItemID.TitaniumBar, BarStack()), (ItemID.PalladiumBar, BarStack()), (ItemID.OrichalcumBar, BarStack())
			],
			true, WorldGen.genRand, WorldGen.genRand.Next(15, 21), 1, true, 2, 2);

		if (!success && placementRetries < 200)
			goto retry;
		else
		{
			for (int i = 0; i < 2; ++i)
			{
				Tile tile = Main.tile[i, chest.Y];
				tile.TileType = TileID.HardenedSand;
				tile.HasTile = true;
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
		static void Decorate(int i, int j)
		{
			for (int x = 0; x < 300; x++)
			{
				var on = Framing.GetTileSafely(i, j);
				var below = Framing.GetTileSafely(i, j + 1);

				for (int a = 0; a < 50; a++) //Position vertically
				{
					on = Framing.GetTileSafely(i, j);
					below = Framing.GetTileSafely(i, j + 1);

					if (on.HasTile && Main.tileSolid[on.TileType])
						j--;
					else if (!below.HasTile || !Main.tileSolid[below.TileType])
						j++;
					else
						break;
				}

				//Hydrothermal vents
				if (on.LiquidAmount > 0 && on.LiquidType == LiquidID.Water && below.TileType == TileID.Sand && !below.TopSlope && WorldGen.genRand.NextBool(25))
				{
					int type = WorldGen.genRand.NextBool(3) ? ModContent.TileType<HydrothermalVent1x3>() : ModContent.TileType<HydrothermalVent1x2>();

					WorldGen.PlaceObject(i, j, type, true, WorldGen.genRand.Next(2));
					NetMessage.SendObjectPlacement(-1, i, j, type, 0, 0, -1, -1);
				}

				i -= GenVars.dungeonSide; //Position horizontally
			}
		}

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
				Decorate(i, j);
			}
		}
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