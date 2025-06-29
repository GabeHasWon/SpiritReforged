using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.TileCommon.Tree;
using SpiritReforged.Common.WallCommon;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Common.WorldGeneration.Ecotones;
using SpiritReforged.Common.WorldGeneration.SecretSeeds;
using SpiritReforged.Common.WorldGeneration.SecretSeeds.Seeds;
using SpiritReforged.Content.Savanna.Items;
using SpiritReforged.Content.Savanna.Tiles;
using SpiritReforged.Content.Savanna.Tiles.AcaciaTree;
using SpiritReforged.Content.Savanna.Walls;
using System.Linq;
using Terraria.DataStructures;
using Terraria.GameContent.Generation;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Content.Savanna.Ecotone;

internal class SavannaEcotone : EcotoneBase
{
	private delegate bool OnAttempt(int i, int j);

	[WorldBound]
	public static Rectangle SavannaArea;
	private static int Steps = 0;

	protected override void InternalLoad()
	{
		On_WorldGen.GrowPalmTree += PreventPalmTreeGrowth;
		On_WorldGen.PlacePot += ConvertPot;
	}

	private static bool PreventPalmTreeGrowth(On_WorldGen.orig_GrowPalmTree orig, int i, int y)
	{
		if (WorldGen.generatingWorld && SavannaArea.Contains(new Point(i, y)))
			return false; //Skips orig

		return orig(i, y);
	}

	private static bool ConvertPot(On_WorldGen.orig_PlacePot orig, int x, int y, ushort type, int style)
	{
		if (WorldGen.generatingWorld && SavannaArea.Contains(new Point(x, y)))
			style = WorldGen.genRand.Next(7, 10); //Jungle pots

		return orig(x, y, type, style);
	}

	public override void AddTasks(List<GenPass> tasks, List<EcotoneSurfaceMapping.EcotoneEntry> entries)
	{
		int pyramidIndex = tasks.FindIndex(x => x.Name == "Pyramids");
		int grassIndex = tasks.FindIndex(x => x.Name == "Spreading Grass") + 2;

		if (pyramidIndex == -1 || grassIndex == -1)
			return;

		tasks.Insert(pyramidIndex, new PassLegacy("Savanna", BaseGeneration(entries)));
		tasks.Insert(grassIndex, new PassLegacy("Populate Savanna", PopulateSavanna));
	}

	private static bool CanGenerate(List<EcotoneSurfaceMapping.EcotoneEntry> entries, out (int, int) bounds)
	{
		static bool NotOcean(EcotoneSurfaceMapping.EcotoneEntry e) => e.Start.X > GenVars.leftBeachEnd
			&& e.End.X > GenVars.leftBeachEnd && e.Start.X < GenVars.rightBeachStart && e.End.X < GenVars.rightBeachStart; //Don't generate next to the ocean

		const int offX = EcotoneSurfaceMapping.TransitionLength + 1; //Removes forest patches on the left side
		bounds = (0, 0);

		if (SecretSeedSystem.WorldSecretSeed == SecretSeedSystem.GetSeed<SavannaSeed>())
		{
			int spawn = Main.maxTilesX / 2;
			var valid = entries.Where(x => x.Start.X < spawn && x.End.X > spawn);

			if (valid.Any())
			{
				var e = valid.First();
				bounds = (e.Start.X - offX, e.End.X);

				return true;
			}

			return false;
		}
		else
		{
			var validEntries = entries.Where(x => x.SurroundedBy("Desert", "Jungle") && Math.Abs(x.Start.Y - x.End.Y) < 120 && NotOcean(x));
			if (!validEntries.Any())
				return false;

			var entry = validEntries.ElementAt(WorldGen.genRand.Next(validEntries.Count()));
			if (entry is null)
				return false;

			bounds = (entry.Start.X - offX, entry.End.X);
			return true;
		}
	}

	private static WorldGenLegacyMethod BaseGeneration(List<EcotoneSurfaceMapping.EcotoneEntry> entries) => (progress, _) =>
	{
		if (!CanGenerate(entries, out var bounds))
			return;

		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.SavannaTerrain");

		int startX = bounds.Item1;
		int endX = bounds.Item2;
		short startY = EcotoneSurfaceMapping.TotalSurfaceY[(short)startX];
		short endY = EcotoneSurfaceMapping.TotalSurfaceY[(short)endX];

		//A hash of tile types which can be replaced
		HashSet<int> validIds = [TileID.Dirt, TileID.Grass, TileID.ClayBlock, TileID.CrimsonGrass, TileID.CorruptGrass, TileID.Stone];

		var topBottomY = new Point(Math.Min(startY, endY), Math.Max(startY, endY));

		Steps = WorldGen.genRand.Next(2, 5);

		var sandNoise = new Common.WorldGeneration.Noise.FastNoiseLite(WorldGen.genRand.Next());
		sandNoise.SetFrequency(0.04f);
		int xOffsetForFactor = -1;
		float curve = 0;

		for (int x = startX; x < endX; ++x)
		{
			float factor = GetBaseLerpFactorForX(startX, endX, xOffsetForFactor, x); //Step height

			int addY = (int)MathHelper.Lerp(startY, endY, curve);
			int stepY = addY - (int)(sandNoise.GetNoise(x, 600) * 2);

			int maxDepth = WorldGen.genRand.Next(20);
			int minDepth = (int)Main.worldSurface - stepY;

			if (curve < factor) //easing (hills)
			{
				int fullHeight = (startY - endY) / Steps; //The average height of each step
				const float steepness = .05f;

				//Control hill shape using a lazy sine that remains similar between steps
				float amount = fullHeight == 0 ? 0 : (float)Math.Sin(1f + stepY % fullHeight / (float)fullHeight * 2) * steepness;
				curve += Math.Max(amount, .008f);
			}

			float taper = Math.Clamp((float)Math.Sin((float)(x - startX) / (endX - startX) * Math.PI) * 1.75f, 0, 1);
			int startHeight = Math.Min(HighestSurfacePoint(x) - stepY, 0);

			for (int depth = startHeight; depth < (30 + maxDepth + minDepth) * taper; ++depth)
			{
				int y = stepY + depth;
				var tile = Main.tile[x, y];

				if (depth >= 0)
				{
					if (depth < 15 || tile.WallType == WallID.None)
						tile.HasTile = true;

					if (tile.HasTile && !validIds.Contains(tile.TileType) && !TileID.Sets.Ore[tile.TileType])
						continue; //Can this tile be replaced by type?

					if (y < topBottomY.X)
						topBottomY.X = y;

					if (y > topBottomY.Y)
						topBottomY.Y = y;

					tile.TileType = (ushort)GetType();

					if (depth > 1) //Convert walls
					{
						if (tile.TileType is TileID.Sand or TileID.HardenedSand)
							tile.WallType = WallID.HardenedSand;
						else if (tile.TileType is TileID.Sandstone || TileID.Sets.Ore[tile.TileType])
							tile.WallType = WallID.Sandstone;
						else if (tile.WallType is WallID.None or WallID.DirtUnsafe)
							tile.WallType = (ushort)AutoloadedWallExtensions.UnsafeWallType<SavannaDirtWall>();
					}
					else
						tile.Clear(TileDataType.Wall); //Clear walls above the Savanna surface
				}
				else
					tile.Clear(TileDataType.All);

				int GetType()
				{
					float depthNoise = (sandNoise.GetNoise(x, 0) + 1) * 5 + 6;
					float spotNoise = sandNoise.GetNoise(x, y);

					int type = ModContent.TileType<SavannaDirt>();

					if (depth > depthNoise && TileID.Sets.Ore[tile.TileType])
						return tile.TileType; //Keep below-surface ores

					if (depth > (sandNoise.GetNoise(x, 0) + 1) * 3 + 15)
						type = TileID.HardenedSand; //Add hardened sand below a certain depth for ease of navigation
					else if (depth > depthNoise)
						type = (spotNoise < .25f || !WorldGen.SolidTile(x, y + 1)) ? TileID.HardenedSand : TileID.Sand;

					if (depth > 90 + depthNoise)
						type = TileID.Sandstone;
					else if (depth > 50 && spotNoise * ((depth - 50f) / 30f) > .25f)
						type = TileID.Sandstone;

					return type;
				}
			}

			xOffsetForFactor += (int)Math.Round(Math.Max(sandNoise.GetNoise(x, 0), 0) * 5);
		}

		SavannaArea = new Rectangle(startX, topBottomY.X, endX - startX, topBottomY.Y - topBottomY.X);
		SavannaArea.Inflate(2, 2);

		WorldDetours.Regions.Add(new(SavannaArea, WorldDetours.Context.Lava));
		WorldDetours.Regions.Add(new(SavannaArea, WorldDetours.Context.Piles));

		static int HighestSurfacePoint(int x)
		{
			int y = (int)(Main.worldSurface * 0.35); //Sky height
			while (!Main.tile[x, y].HasTile && Main.tile[x, y].WallType == WallID.None && Main.tile[x, y].LiquidAmount == 0 || WorldMethods.CloudsBelow(x, y, out int addY))
				y++;

			return y;
		}
	};

	private void PopulateSavanna(GenerationProgress progress, GameConfiguration configuration)
	{
		if (SavannaArea.IsEmpty)
			return;

		const int minimumTreeSpace = 7;

		int chanceMax = CrossMod.Remnants.Enabled ? 40 : 90; // Maximum odds to generate a tree - lower in Remnants

		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.SavannaObjects");
		HashSet<int> treeSpacing = [];
		HashSet<Point16> grassTop = [];

		var baobabArea = Rectangle.Empty;
		bool genWateringHole = false;
		bool genBaobabTree = false;

		if (SavannaArea.Width > 150 && Main.rand.NextBool(3)) //Choose objects to gen
			genWateringHole = genBaobabTree = true;
		else if (SavannaArea.Width > 50)
		{
			if (Main.rand.NextBool())
				genWateringHole = true;
			else
				genBaobabTree = true;
		}

		GrowStones();

		if (genWateringHole)
		{
			if (IterateGen(200, HoleGen, out int i, out int j, "Watering Hole"))
				WateringHoleGen.GenerateWateringHole(i, j);

			static bool HoleGen(int i, int j)
			{
				HashSet<int> soft = [TileID.Dirt, TileID.CorruptGrass, TileID.CrimsonGrass, TileID.Sand, ModContent.TileType<SavannaDirt>()];

				return soft.Contains(Main.tile[i, j].TileType) && Collision.SolidCollision(new Vector2((i - 28) * 16, j * 16), 32, 64)
					&& Collision.SolidCollision(new Vector2((i + 26) * 16, j * 16), 32, 32);
			}
		}

		if (genBaobabTree)
		{
			if (IterateGen(50, BaobabTreeGen, out int i, out int j, "Great Baobab"))
			{
				baobabArea = BaobabGen.GenerateBaobab(i, j);
				baobabArea.Inflate(40, 20);
			}

			static bool BaobabTreeGen(int i, int j) => Main.tile[i, j].TileType == ModContent.TileType<SavannaDirt>() 
				&& Main.tile[i, j - 1].LiquidAmount < 50 && WorldMethods.IsFlat(new Point16(i, j), 20, out _, out _);
		}

		for (int i = SavannaArea.Left; i < SavannaArea.Right; ++i)
			for (int j = SavannaArea.Top - 1; j < SavannaArea.Bottom; ++j)
			{
				OpenFlags flags = OpenTools.GetOpenings(i, j, false, false, true);
				var tile = Main.tile[i, j];

				if (flags == OpenFlags.None)
					continue;

				if (tile.HasTile)
				{
					if (tile.TileType == TileID.Stone && WorldGen.genRand.NextBool()) //Place stone piles on stone
					{
						if (WorldGen.genRand.NextBool(3))
							WorldGen.PlaceTile(i, j - 1, ModContent.TileType<SavannaRockLarge>(), true, style: WorldGen.genRand.Next(3));
						else
							WorldGen.PlaceTile(i, j - 1, ModContent.TileType<SavannaRockSmall>(), true, style: WorldGen.genRand.Next(3));
					}

					if (tile.TileType == ModContent.TileType<SavannaDirt>())
					{
						tile.TileType = (ushort)ModContent.TileType<SavannaGrass>(); //Grow grass on dirt

						if (flags.HasFlag(OpenFlags.Above))
							grassTop.Add(new Point16(i, j));

						if (tile.WallType == ModContent.WallType<SavannaDirtWall>())
							tile.Clear(TileDataType.Wall); //Clear walls again, but specifically for savanna dirt
					}
				}

				if (WorldGen.genRand.NextBool(120)) //Rare bones
					WorldGen.PlaceTile(i, j - 1, TileID.LargePiles2, true, style: WorldGen.genRand.Next(52, 55));
			}

		if (WorldGen.genRand.NextBool(3))
			Campsite();

		int treeOdds = chanceMax;

		foreach (var p in grassTop)
		{
			(int i, int j) = (p.X, p.Y - 1);

			PlaceStuffOnGrass(i, j);

			int treeDistance = Math.Abs(i - treeSpacing.OrderBy(x => Math.Abs(i - x)).FirstOrDefault());
			if (treeDistance > minimumTreeSpace)
			{
				if (WorldGen.genRand.NextBool(treeOdds) && !baobabArea.Contains(i, j) && GrowTree(i, j))
				{
					treeSpacing.Add(i);
					treeOdds = chanceMax;
				}
				else
					treeOdds = Math.Max(treeOdds - 1, 2); //Decrease the odds every time a tree fails to generate
			}
		}
	}

	/// <summary> Places tiles on grass. </summary>
	/// <param name="i"> The grass tile's X coordinate. </param>
	/// <param name="j"> Above the grass tile's Y coordinate. </param>
	private static void PlaceStuffOnGrass(int i, int j)
	{
		if (WorldGen.genRand.NextBool(8)) //Surface pots
			WorldGen.PlaceTile(i, j, TileID.Pots, true, true, style: 7);

		if (WorldGen.genRand.NextBool(13)) //Elephant grass patch
			CreatePatch(WorldGen.genRand.Next(5, 11), 0, WorldGen.genRand.Next([0, 5]), ModContent.TileType<ElephantGrass>());

		if (WorldGen.genRand.NextBool(9)) //Foliage patch
			CreatePatch(WorldGen.genRand.Next(6, 13), 2, types: ModContent.TileType<SavannaFoliage>());

		if (WorldGen.genRand.NextBool(45)) //Termite mound
		{
			int type = WorldGen.genRand.NextFromList(ModContent.TileType<TermiteMoundSmall>(),
				ModContent.TileType<TermiteMoundMedium>(), ModContent.TileType<TermiteMoundLarge>());
			int style = WorldGen.genRand.Next(TileObjectData.GetTileData(type, 0).RandomStyleRange);

			WorldGen.PlaceTile(i, j, type, true, true, style: style);
		}

		void CreatePatch(int size, int chance, int alt = 0, params int[] types)
		{
			for (int x = i - size / 2; x < i + size / 2; x++)
			{
				if (chance > 1 && !WorldGen.genRand.NextBool(chance))
					continue;

				int y = j;
				WorldMethods.FindGround(x, ref y);

				int type = types[WorldGen.genRand.Next(types.Length)];
				int style = alt + WorldGen.genRand.Next(TileObjectData.GetTileData(type, 0, alt).RandomStyleRange);

				WorldGen.PlaceTile(x, y - 1, type, true, style: style);
			}
		}
	}

	private static bool GrowTree(int i, int j)
	{
		const int shrubSpread = 16;
		const int rootSpread = 3;

		bool success = CustomTree.GrowTree<AcaciaTree>(i, j);
		if (success) //Place shrubs and roots around trees
		{
			for (int x = i - rootSpread; x < i + rootSpread; x++)
			{
				if (!WorldGen.genRand.NextBool(3))
					continue;

				int y = SavannaArea.Top;
				WorldMethods.FindGround(x, ref y);

				if (WorldGen.genRand.NextBool(3))
					WorldGen.PlaceTile(x, y - 1, ModContent.TileType<SavannaShrubs>(), true, style: WorldGen.genRand.Next(11));

				int type = WorldGen.genRand.NextBool() ? ModContent.TileType<AcaciaRootsLarge>() : ModContent.TileType<AcaciaRootsSmall>();
				int styleRange = TileObjectData.GetTileData(type, 0).RandomStyleRange;

				WorldGen.PlaceTile(x, y - 1, type, true, style: WorldGen.genRand.Next(styleRange));
			}

			for (int x = i - shrubSpread; x < i + shrubSpread; x++)
			{
				int y = SavannaArea.Top;
				WorldMethods.FindGround(x, ref y);

				if (WorldGen.genRand.NextBool(4))
					WorldGen.PlaceTile(x, y - 1, ModContent.TileType<SavannaShrubs>(), true, style: WorldGen.genRand.Next(11));
			}
		}

		return success;
	}

	private static void GrowStones()
	{
		const int maxAttempts = 200;

		int numMax = SavannaArea.Width / 150;
		int num = 0;

		int margin = (int)(SavannaArea.Width * .22f);

		for (int a = 0; a < maxAttempts; a++)
		{
			var pos = WorldGen.genRand.NextVector2FromRectangle(SavannaArea with { Height = 2 }).ToPoint16();
			int x = pos.X;
			int y = pos.Y;

			if (pos.X > SavannaArea.Left + margin && pos.X < SavannaArea.Right - margin)
				continue;

			WorldMethods.FindGround(x, ref y);

			int halfWidth = WorldGen.genRand.Next(2, 5);
			ShapeData data = new();

			WorldUtils.Gen(new Point(x, y + 1), new Shapes.Mound(halfWidth, 3), Actions.Chain(new Modifiers.Blotches(), new Actions.SetTile(TileID.Stone).Output(data)));

			if (WorldGen.genRand.NextBool())
				WorldUtils.Gen(new Point(x, y + 1), new ModShapes.All(data), Actions.Chain(new Modifiers.OnlyTiles(TileID.Stone), new Modifiers.IsTouchingAir(), new Actions.SetTile(TileID.BrownMoss)));

			WorldUtils.Gen(new Point(x, y + 1), new ModShapes.All(data), new Actions.Smooth());
			WorldUtils.Gen(new Point(x, y + 1), new ModShapes.All(data), Actions.Chain(new Modifiers.Offset(0, 4), new Modifiers.Blotches(), 
				new Modifiers.OnlyTiles((ushort)ModContent.TileType<SavannaDirt>()), new Actions.SetTile(TileID.Sand)));

			if (++num > numMax)
				break;
		}
	}

	private static void Campsite()
	{
		IterateGen(200, CampsiteGen, out int i, out int j, "Savanna Campsite");

		static bool CampsiteGen(int i, int j)
		{
			const int halfCampfireDistance = 8;

			if (Main.tile[i, j].TileType != ModContent.TileType<SavannaGrass>() || !TileObject.CanPlace(i, j - 1, TileID.LargePiles2, 26, 0, out _, true))
				return false;
			//Can we place the tent here? If so, try placing the campfire nearby

			int y = j;
			for (int x = i - halfCampfireDistance; x < i + halfCampfireDistance; x++)
			{
				WorldMethods.FindGround(x, ref y);

				//Don't overlap the tent position. This assumes tile widths are 3 each
				//Place the campfire, and if successful, place the tent in our predetermined location
				if (Math.Abs(x - i) > 2 && CampfireSlot.GenerateCampfire(x, y - 1))
				{
					WorldGen.PlaceTile(i, j - 1, TileID.LargePiles2, true, style: 26);
					return true; //Success!
				}
			}

			return false;
		}
	}

	private static bool OnBaobab(int i, int j)
	{
		var t = Framing.GetTileSafely(i, j + 1);

		if (!t.HasTile)
			return false;

		int belowType = t.TileType;
		return belowType == ModContent.TileType<LivingBaobab>() || belowType == ModContent.TileType<LivingBaobabLeaf>();
	}

	private static float GetBaseLerpFactorForX(int startX, int endX, int xOffsetForFactor, int x)
	{
		float factor = (MathF.Min(x + xOffsetForFactor, endX) - startX) / (endX - startX);
		factor = ModifyLerpFactor(factor);
		return factor;
	}

	private static float ModifyLerpFactor(float factor)
	{
		float adj = Steps;
		factor = (int)((factor + 0.1f) * adj) / adj;
		return factor;
	}

	private static bool IterateGen(int tries, OnAttempt isValid, out int x, out int y, string structureName = default)
	{
		for (int t = 0; t < tries; t++)
		{
			int i = WorldGen.genRand.Next(SavannaArea.Left, SavannaArea.Right);
			int j = SavannaArea.Top;

			WorldMethods.FindGround(i, ref j);

			if (isValid.Invoke(i, j))
			{
				(x, y) = (i, j);
				return true;
			}

			if (t == tries - 1 && structureName != default)
				SpiritReforgedMod.Instance.Logger.Info("Generator exceeded maximum tries for structure: " + structureName);
		}

		(x, y) = (0, 0);
		return false;
	}
}
