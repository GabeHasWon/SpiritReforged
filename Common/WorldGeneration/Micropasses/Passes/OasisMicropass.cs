using ReLogic.Utilities;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.WorldGeneration.GenConfiguration;
using SpiritReforged.Common.WorldGeneration.Microbiomes;
using SpiritReforged.Common.WorldGeneration.Micropasses.Passes.Ziggurat;
using SpiritReforged.Content.Desert;
using SpiritReforged.Content.Desert.Tiles;
using SpiritReforged.Content.Jungle.Pineapple;
using SpiritReforged.Content.Ziggurat.Tiles;
using SpiritReforged.Content.Ziggurat.Walls;
using System.Linq;
using Terraria.DataStructures;
using Terraria.ModLoader.Config;
using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class OasisMicropass : Micropass, IGenerationPage
{
	#nullable enable
	public class UndergroundOasisBiome : MicrobiomeSystem.Microbiome
	{
		public Rectangle Rectangle => new(Position.X - Size.X / 2, Position.Y - Size.Y / 2, Size.X, Size.Y);

		public static bool InUndergroundOasis(Player p)
		{
			const string flagType = "UndergroundOasis";

			if (p.CheckFlag(flagType) is bool flag)
				return flag;

			//Preface with basic relevant checks so linq isn't constantly running in the background
			bool result = p.Center.Y / 16 > Main.worldSurface && p.ZoneDesert && OasisAreas.Any(x => x.Contains(p.Center.ToTileCoordinates()));
			p.SetFlag(flagType, result); //Cache the result to avoid checking against this logic more than once per tick

			return result;
		}

		#region detours
		public override void Load()
		{
			NPCEvents.OnEditSpawnRate += ReduceSpawns;
			PlayerEvents.OnPostUpdateEquips += HealInSprings;
			PlayerEvents.OnPostUpdateEquips += HappyInOasis;

			MicrobiomeSystem.PopulateMicrobiomes += static () =>
			{
				OasisAreas.Clear(); //Refresh biome area cache
				foreach (var b in MicrobiomeSystem.Microbiomes)
				{
					if (b is UndergroundOasisBiome oasis)
						OasisAreas.Add(oasis.Rectangle);
				}
			};
		}

		private static void HappyInOasis(Player player)
		{
			if (InUndergroundOasis(player))
				player.AddBuff(BuffID.Sunflower, 2);
		}

		private static void ReduceSpawns(Player player, ref int spawnRate, ref int maxSpawns)
		{
			if (InUndergroundOasis(player))
			{
				spawnRate *= 5;
				maxSpawns = 0;
			}
		}

		private static void HealInSprings(Player player)
		{
			if (player.wet && InUndergroundOasis(player))
				player.AddBuff(BuffID.Regeneration, 180);
		}
		#endregion
	}

	public override string WorldGenName => "Underground Oasis";
	private static WeightedRandom<int> MainWaterItem = null!;
	private static WeightedRandom<(int type, Range stackRange, Func<bool>? canPlace)> RandomItem = null!;

	public static readonly Point16 Size = new(50, 40);
	public static readonly HashSet<Rectangle> OasisAreas = [];

	[GenConfigurable(1, 50)]
	[ReverseMinMax]
	[Slider]
	[Denominator]
	private static int PalmChanceLow = 15;

	[GenConfigurable(1, 25)]
	[ReverseMinMax]
	[Slider]
	[Denominator]
	[PriorityModifier(nameof(PalmChanceLow))]
	private static int PalmChanceHigh = 5;

	[GenConfigurable(1, 25)]
	[ReverseMinMax]
	[Slider]
	[Denominator]
	private static int CattailChance = 3;

	[GenConfigurable(1, 30)]
	[ReverseMinMax]
	[Slider]
	[Denominator]
	private static int OasisPlantChance = 3;

	[GenConfigurable(1, 30)]
	[ReverseMinMax]
	[Slider]
	[Denominator]
	private static int GlowflowerChance = 4;

	[GenConfigurable(1, 30)]
	[ReverseMinMax]
	[Slider]
	[Denominator]
	private static int PineappleChance = 3;

	[GenConfigurable(1, 30)]
	[ReverseMinMax]
	[Slider]
	[Denominator]
	private static int SeaOatsChance = 2;

	[GenConfigurable("0 0", "10 15")]
	private static GenRange LightRange = new GenRange(1, 3);

	[GenConfigurable("1 0", "15 8")]
	private static GenRange PoolWidth = new GenRange(6, 5);

	[GenConfigurable("1 0", "10 15")]
	private static GenRange PoolDepth = new GenRange(3, 3);

	[GenConfigurable(0f, 1f, 0.01f)]
	[Slider]
	private static float MainNormalization = 0;

	[GenConfigurable(0f, 1f, 0.01f)]
	[Slider]
	private static float ItemNormalization = 0;

	[GenConfigurable("1 0", "10 15")]
	private static GenRange ChestItemRange = new GenRange(6, 2);

	[GenConfigurable("1 0", "20 30")]
	internal static GenRange RuinSegments = new GenRange(2, 3);

	[GenConfigurable("0 0", "50 25")]
	internal static GenRange RuinCount = new GenRange(0, 4);

	PageInfo IGenerationPage.Info => new("Desert", DrawHelpers.RequestLocal(GetType(), "DesertPage", false), DrawHelpers.RequestLocal(GetType(), "DesertPageButton", false))
	{
		Presets =
		[
			new("Overgrown",
				[
					new IndividualPreset(nameof(PalmChanceHigh), 2),
					new IndividualPreset(nameof(PalmChanceLow), 20),
					new IndividualPreset(nameof(CattailChance), 1),
					new IndividualPreset(nameof(OasisPlantChance), 2),
					new IndividualPreset(nameof(GlowflowerChance), 2),
					new IndividualPreset(nameof(GlowflowerChance), 2),
					new IndividualPreset(nameof(LightRange), GenRange.Empty),
					new IndividualPreset(nameof(PoolWidth), new GenRange(10, 5)),
					new IndividualPreset(nameof(PoolDepth), new GenRange(7, 10)),
				]),

			new("Petrified",
				[
					new IndividualPreset(nameof(DesertMicropass.FossilCount), 45),
					new IndividualPreset(nameof(DesertMicropass.FossilMultiplier), 5f),
					new IndividualPreset(nameof(DesertMicropass.PatchScale), 22)
				]),

			new("Ruined",
				[
					new IndividualPreset(nameof(OasisMicropass.RuinCount), new GenRange(3, 7)),
					new IndividualPreset(nameof(OasisMicropass.RuinSegments), new GenRange(5, 11)),
				]),
		]
	};

	Mod IGenerationPage.Mod => SpiritReforgedMod.Instance;

	public override int GetWorldGenIndexInsert(List<GenPass> passes, ref bool afterIndex)
	{
		afterIndex = true;
		return passes.FindIndex(genpass => genpass.Name.Equals("Micro Biomes"));
	}

	public override void Run(GenerationProgress progress, Terraria.IO.GameConfiguration config)
	{
		const int maxAttempts = 200;
		const int area = 50;

		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.DesertExtras");

		int attempts = 0;
		int amount = 3 * (WorldGen.GetWorldSize() + 1);
		Rectangle region = new(GenVars.desertHiveLeft, (int)Main.worldSurface + 40, GenVars.desertHiveRight - GenVars.desertHiveLeft, GenVars.desertHiveLow - GenVars.desertHiveHigh);

		if (CrossMod.Remnants.Enabled) // Remnants doesn't set the above values in the way we use them, use the below...which may be better anyway?
			region = GenVars.UndergroundDesertLocation;

		HashSet<Rectangle> biomesRectangles = [];

		for (int i = 0; i < amount; i++)
		{
			Point pt = WorldGen.genRand.NextVector2FromRectangle(region).ToPoint();
			if (!WorldGen.InWorld(pt.X, pt.Y))
				break;

			if (!GenVars.structures.CanPlace(new Rectangle(pt.X - area / 2, pt.Y - area / 2, area, area), 4) || biomesRectangles.Any(x => x.Contains(pt)))
			{
				if (++attempts < maxAttempts)
					i--;

				continue;
			}

			Dictionary<ushort, int> typeToCount = [];
			WorldUtils.Gen(pt - new Point(area / 2, area / 2), new Shapes.Rectangle(area, area), new Actions.TileScanner(TileID.Sand, TileID.Sandstone, TileID.HardenedSand).Output(typeToCount));

			if (typeToCount[TileID.Sand] + typeToCount[TileID.Sandstone] + typeToCount[TileID.HardenedSand] < area * area * 0.5f)
			{
				if (++attempts < maxAttempts)
					i--;

				continue;
			}

			UndergroundOasisBiome biome = MicrobiomeSystem.Microbiome.Create<UndergroundOasisBiome>(pt);
			Generate(new(pt));

			Rectangle rectangle = biome.Rectangle;
			rectangle.Inflate(100, 100);

			biomesRectangles.Add(rectangle);
			int ruinCount = RuinCount.RollRange();

			if (ruinCount > 0)
				WorldMethods.Generate(GenerateRuins, ruinCount, out _, rectangle, 50);
		}
	}

	#region worldgen
	public static void Generate(Point16 point)
	{
		var origin = point.ToPoint();
		Point radius = new(WorldGen.genRand.Next(30, 35), WorldGen.genRand.Next(45, 70));
		ShapeData shape = new();

		//Base material
		WorldUtils.Gen(new Point(origin.X, origin.Y), new Shapes.Circle(radius.X, 10), Actions.Chain(
			new Modifiers.Blotches(2, 0.4),
			new Modifiers.SkipTiles(TileID.Sand),
			new Actions.SetTileKeepWall(TileID.Sandstone)
		).Output(shape));

		WorldUtils.Gen(new Point(origin.X, origin.Y - 2), new ModShapes.All(shape), Actions.Chain(
			new Modifiers.OnlyTiles(TileID.Sandstone),
			new Actions.SetTileKeepWall(TileID.Sand)
		));

		//Clearing shape
		WorldUtils.Gen(origin, new Shapes.Mound(radius.X, radius.Y / 2), Actions.Chain(
			new Modifiers.RectangleMask(-(radius.X - 5), radius.X - 5, -radius.Y, radius.Y),
			new Modifiers.Blotches(),
			new Actions.ClearTile(frameNeighbors: true)
		).Output(shape));

		WorldUtils.Gen(origin, new ModShapes.All(shape), new Actions.Smooth());

		WorldUtils.Gen(new Point(origin.X, origin.Y - 12), new ModShapes.All(shape), Actions.Chain(
			new Modifiers.OnlyTiles(TileID.Sand),
			new Actions.SetTileKeepWall(TileID.Sandstone)
		));

		//Clearing walls
		ShapeData clearingShape = new();
		WorldUtils.Gen(new Point(origin.X, origin.Y + 2), new Shapes.HalfCircle((int)(radius.X * 0.75f)), Actions.Chain(
			new Modifiers.IsNotSolid(),
			new Modifiers.Blotches(3),
			new Actions.ClearWall()
		).Output(clearingShape));

		WorldUtils.Gen(new Point(origin.X, origin.Y + 2), new ModShapes.OuterOutline(clearingShape), Actions.Chain(
			new Modifiers.Blotches(),
			new Modifiers.OnlyWalls(WallID.Sandstone, WallID.HardenedSand),
			new Actions.PlaceWall((ushort)RedSandstoneBrickCrackedWall.UnsafeType)
		));

		int deviation = radius.X / 2;
		Point lakeOrigin = new(origin.X + WorldGen.genRand.Next(-deviation, deviation), origin.Y);
		CarveLake(lakeOrigin);

		PlaceStalactites(origin, radius.X, WorldGen.genRand.Next(4, 8));
		Decorate(origin, shape);
		PlaceLightShafts(origin);

		//GenVars.structures.AddProtectedStructure(new Rectangle(origin.X - Size.X / 2, origin.Y - Size.Y / 2, Size.X, Size.Y), 2);

		MainWaterItem = null!;
		RandomItem = null!;
	}

	private static void Decorate(Point origin, ShapeData clearingShape)
	{
		int palmCount = 0;

		WorldUtils.Gen(origin, new ModShapes.All(clearingShape), Actions.Chain(
			new Modifiers.OnlyTiles(TileID.Sand),
			new Actions.Custom((i, j, args) => {
				if (WorldGen.genRand.NextBool(palmCount == 0 ? PalmChanceHigh : PalmChanceLow) && Main.tile[i, j].Slope == SlopeType.Solid && !Main.tile[i, --j].HasTile)
					if (CreatePalmTree(i, j, WorldGen.genRand.Next(8, 16)))
						palmCount++;

				return true;
			})
		));

		WorldUtils.Gen(origin, new ModShapes.All(clearingShape), Actions.Chain(
			new Modifiers.OnlyTiles(TileID.Sand),
			new Actions.Custom((i, j, args) => {
				if (Main.tile[i, j].Slope != SlopeType.Solid || Main.tile[i, --j].HasTile)
					return false;

				if (Main.tile[i, j].LiquidAmount > 100)
				{
					if (WorldGen.genRand.NextBool(CattailChance))
					{
						WorldGen.PlaceCatTail(i, j);

						int height = WorldGen.genRand.Next(3, 6);
						for (int h = 0; h < height; h++)
							WorldGen.GrowCatTail(i, j);
					}
				}
				else
				{
					if (WorldGen.genRand.NextBool(OasisPlantChance))
						WorldGen.PlaceOasisPlant(i, j);

					if (WorldGen.genRand.NextBool(GlowflowerChance))
						Placer.PlaceTile(i, j, ModContent.TileType<Glowflower>());

					if (WorldGen.genRand.NextBool(PineappleChance))
						Placer.PlaceTile(i, j, ModContent.TileType<PineapplePlant>());

					if (WorldGen.genRand.NextBool(SeaOatsChance))
					{
						var t = Main.tile[i, j];

						if (!t.HasTile)
						{
							t.ResetToType(TileID.SeaOats);
							t.HasTile = true;
							t.TileFrameX = (short)(18 * WorldGen.genRand.Next(15));
						}
					}
				}

				return true;
			})
		));
	}

	private static void PlaceLightShafts(Point point)
	{
		int x = point.X;
		int y = point.Y;

		int count = LightRange.RollRange();// LightAmountMin + WorldGen.genRand.Next(LightAmountRange + 1);
		HashSet<int> lastX = [];

		for (int i = 0; i < count; i++)
		{
			while (WorldGen.InWorld(x, y, 2) && !WorldGen.SolidTile(x, y))
				y--;

			if (lastX.Add(x))
				Placer.PlaceTile<LightShaft>(x, y + 1);

			x = point.X + WorldGen.genRand.Next(-10, 10);
			y = point.Y;
		}
	}

	private static void PlaceStalactites(Point origin, int radius, int count)
	{
		int maxAttempts = 10 * count;
		int attempts = 0;
		HashSet<Point> points = [];

		for (int i = 0; i < count; i++)
		{
			var point = new Point(origin.X + WorldGen.genRand.Next(4, radius) * WorldGen.genRand.Next([-1, 1]), origin.Y);
			int x = point.X;
			int y = point.Y;

			while (WorldGen.InWorld(x, y, 2) && !WorldGen.SolidTile(x, y))
				y--;

			if (Main.tile[x, y].TileType != TileID.Sandstone)
			{
				if (++attempts < maxAttempts)
					i--;

				continue;
			}

			points.Add(new(x, y - 2));
		}

		foreach (var pt in points)
			WorldUtils.Gen(pt, new Shapes.Tail(WorldGen.genRand.Next(3, 6), new Vector2D(0, WorldGen.genRand.Next(4, 16))), new Actions.SetTileKeepWall(TileID.Sandstone));
	}

	private static void CarveLake(Point origin)
	{
		WorldMethods.FindGround(origin.X, ref origin.Y);
		ShapeData shape = new();

		WorldUtils.Gen(origin, new Shapes.Circle(PoolWidth.RollRange(), PoolDepth.RollRange()), Actions.Chain(
			new Modifiers.IsSolid(),
			new Actions.ClearTile(),
			new Actions.SetLiquid(LiquidID.Water)
		).Output(shape));

		WorldUtils.Gen(origin, new ModShapes.OuterOutline(shape), new Actions.Smooth());

		Vector2 size = new(50);
		WorldDetours.Regions.Add(new(new Rectangle(origin.X - (int)(size.X / 2), origin.Y - (int)(size.Y / 2), (int)size.X, (int)size.Y), WorldDetours.Context.Lava));

		if (WorldGen.genRand.NextBool(3))
			return;

		if (MainWaterItem is null)
		{
			MainWaterItem = new(WorldGen.genRand);
			MainWaterItem.Add(ItemID.FloatingTube, 1);
			MainWaterItem.Add(ItemID.BreathingReed, Normalization(0.8f));
			MainWaterItem.Add(ItemID.Flipper, 1);
			MainWaterItem.Add(ItemID.Trident, 1);
			MainWaterItem.Add(ItemID.WaterWalkingBoots, 1);
			MainWaterItem.Add(ItemID.MagicConch, Normalization(1.5f));
			MainWaterItem.Add(ItemID.AncientChisel, 1);
			MainWaterItem.Add(ItemID.MysticCoilSnake, 1);
			MainWaterItem.Add(ItemID.SandBoots, 1);

			static float Normalization(float input) => MathHelper.Lerp(input, 1, MainNormalization);
		}

		if (RandomItem is null)
		{
			RandomItem = new(WorldGen.genRand);
			RandomItem.Add((TileLoader.GetTile(ModContent.TileType<PolishedAmber>()).AutoItemType(), 3..6, null), 1);
			RandomItem.Add((ItemID.IronBar, 5..14, static () => GenVars.iron == TileID.Iron), 1);
			RandomItem.Add((ItemID.LeadBar, 5..14, static () => GenVars.iron == TileID.Lead), 1);
			RandomItem.Add((ItemID.SilverBar, 5..14, static () => GenVars.silver == TileID.Silver), 1);
			RandomItem.Add((ItemID.TungstenBar, 5..14, static () => GenVars.silver == TileID.Tungsten), 1);
			RandomItem.Add((ItemID.RegenerationPotion, 1..2, null), Normalization(0.33f));
			RandomItem.Add((ItemID.GillsPotion, 1..2, null), Normalization(0.33f));
			RandomItem.Add((ItemID.NightOwlPotion, 1..2, null), Normalization(0.33f));
			RandomItem.Add((ItemID.SwiftnessPotion, 1..2, null), Normalization(0.33f));
			RandomItem.Add((ItemID.ShinePotion, 1..2, null), Normalization(0.33f));
			RandomItem.Add((ItemID.ArcheryPotion, 1..2, null), Normalization(0.33f));
			RandomItem.Add((ItemID.HunterPotion, 1..2, null), Normalization(0.33f));
			RandomItem.Add((ItemID.MiningPotion, 1..2, null), Normalization(0.33f));
			RandomItem.Add((ItemID.TrapsightPotion, 1..2, null), Normalization(0.33f));
			RandomItem.Add((ItemID.RecallPotion, 2..4, null), 1);
			RandomItem.Add((ItemID.Extractinator, 1..1, null), 1);
			RandomItem.Add((ItemID.Bomb, 10..19, null), 1);
			RandomItem.Add((ItemID.ThrowingKnife, 8..15, null), Normalization(1.5f));
			RandomItem.Add((ItemID.Shuriken, 8..15, null), Normalization(1.5f));
			RandomItem.Add((ItemID.WoodenArrow, 5..12, null), Normalization(2f));

			static float Normalization(float input) => MathHelper.Lerp(input, 1, ItemNormalization);
		}

		// Try a few times
		for (int i = 0; i < 3; ++i)
		{
			int chestX = origin.X + WorldGen.genRand.Next(-2, 3);
			WorldMethods.FindGround(chestX, ref origin.Y);
			int chestIndex = WorldGen.PlaceChest(chestX, origin.Y - 1, TileID.Containers, false, 17);

			if (chestIndex != -1)
			{
				Chest chest = Main.chest[chestIndex];
				chest.item[0] = new(MainWaterItem.Get());
				chest.item[0].Prefix(-1);

				int miscLength = ChestItemRange.RollRange();
				HashSet<int> takenRandomIds = [];

				for (int j = 1; j < miscLength; ++j)
				{
					var (type, stackRange, canPlace) = RandomItem.Get();

					while (takenRandomIds.Contains(type) || canPlace?.Invoke() == false)
					{
						if (takenRandomIds.Count >= 18)
							return;

						(type, stackRange, canPlace) = RandomItem.Get();
					}

					chest.item[j] = new(type, WorldGen.genRand.Next(stackRange.Start.Value, stackRange.End.Value + 1));
					takenRandomIds.Add(type);
				}

				break;
			}
		}
	}

	/// <summary> Creates a palm tree of <paramref name="height"/> starting from the given coordinates and does <b>not</b> sync it. </summary>
	public static bool CreatePalmTree(int i, int j, int height)
	{
		if (!WorldGen.EmptyTileCheck(i - 1, i + 1, j - height - 1, j - 1, TileID.Saplings))
			return false;

		var r = WorldGen.genRand;
		Tile tile;

		int frameYNum = r.Next(-8, 9) * 2;
		short frameYCache = 0;

		for (int y = 0; y < height; y++)
		{
			tile = Main.tile[i, j - y];
			if (y == 0)
			{
				tile.HasTile = true;
				tile.TileType = TileID.PalmTree;
				tile.TileFrameX = 66;
				tile.TileFrameY = 0;

				continue;
			}

			if (y == height - 1)
			{
				tile.HasTile = true;
				tile.TileType = TileID.PalmTree;
				tile.TileFrameX = (short)(22 * r.Next(4, 7));
				tile.TileFrameY = frameYCache;

				continue;
			}

			if (frameYCache != frameYNum)
			{
				double num5 = (double)y / height;
				if (!(num5 < 0.25) && (num5 < 0.5 && r.NextBool(13) || num5 < 0.7 && r.NextBool(9) || !(num5 < 0.95) || !r.NextBool(5) || true))
				{
					short num6 = (short)Math.Sign(frameYNum);
					frameYCache = (short)(frameYCache + (short)(num6 * 2));
				}
			}

			tile.HasTile = true;
			tile.TileType = TileID.PalmTree;
			tile.TileFrameX = (short)(22 * r.Next(0, 3));
			tile.TileFrameY = frameYCache;
		}

		WorldGen.RangeFrame(i - 2, j - height - 1, i + 2, j + 1);
		return true;
	}
	#endregion

	//Generate oasis ruins, adapted from ZigguratMicropass
	#region ruins
	private static bool GenerateRuins(int x, int y)
	{
		const int suspension = 8; //Forces the structure to be suspended a minimum number of tiles
		if (WorldGen.SolidOrSlopedTile(x, y) || !WorldUtils.Find(new(x, y), new Searches.Down(10).Conditions(new Conditions.IsSolid()), out Point foundPos))
			return false;

		foundPos.Y -= suspension;
		Rectangle structureAreaEstimate = new(foundPos.X - 10, foundPos.Y - 20, 20, 20);

		if (!GenVars.structures.CanPlace(structureAreaEstimate) || !GenVars.UndergroundDesertLocation.Contains(foundPos))
			return false;

		Rectangle region = CreateRuin(foundPos.X, foundPos.Y, RuinSegments.RollRange());

		GenVars.structures.AddProtectedStructure(region);
		WorldDetours.Regions.Add(new(region, WorldDetours.Context.Walls | WorldDetours.Context.Piles));

		return true;
	}

	/// <summary> Generates a desert ruin at the provided location with <paramref name="segments"/> that disrupts tiles. </summary>
	/// <param name="x"> The X coordinate. </param>
	/// <param name="y"> The Y Coordinate. </param>
	/// <param name="segments"> The number of room segments to queue. </param>
	/// <returns> The total area occupied by the ruin. </returns>
	public static Rectangle CreateRuin(int x, int y, int segments)
	{
		ZigguratMicropass.CreateArray(new(x - 4, y - 4, 8, 8), GetUpwardDirections(segments), out List<Rectangle> areas);
		Rectangle result = ZigguratMicropass.Maximize(areas);

		segments = areas.Count; //Reassign segments to be consistent with our number of predetermined areas
		var shapeData = Enumerable.Repeat(new ShapeData(), segments).ToArray();

		for (int c = 0; c < segments; c++)
		{
			Rectangle a = areas[c];
			WorldUtils.Gen(a.Location, new Shapes.Rectangle(a.Width, a.Height), Actions.Chain(
				new Actions.ClearTile(), 
				new Actions.PlaceWall((ushort)PolishedSandstoneWall.UnsafeType), 
				new Modifiers.RectangleMask(2, a.Width - 2 - 1, 0, a.Height), 
				new Actions.PlaceWall((ushort)RedSandstoneBrickWall.UnsafeType)
			).Output(shapeData[c]));

			WorldUtils.Gen(a.Location, new ModShapes.All(shapeData[c]), Actions.Chain(
				new Modifiers.RectangleMask(3, a.Width - 3 - 1, 0, a.Height - 3),
				new Actions.PlaceWall((ushort)BronzeGrate.UnsafeType),
				new Modifiers.Dither(WorldGen.genRand.NextFloat(0.9f)),
				new Actions.ClearWall()
			)); //Add windows with dithering

			for (int p = 0; p < 2; p++)
			{
				Point pillarPosition = a.Location + new Point((a.Width - 1) * p, 0);
				WorldUtils.Gen(pillarPosition, new Shapes.Rectangle(1, a.Height), Actions.Chain(
					new Actions.PlaceTile((ushort)ModContent.TileType<RuinedSandstonePillar>())
				));
			}
		} //Generate all segment walls first and collect ShapeData

		ushort[] skipWallTypes = [(ushort)PolishedSandstoneWall.UnsafeType, (ushort)RedSandstoneBrickWall.UnsafeType];
		for (int c = 0; c < segments; c++)
		{
			Rectangle a = areas[c];
			WorldUtils.Gen(a.Location, new ModShapes.OuterOutline(shapeData[c]), Actions.Chain(
				new Modifiers.SkipWalls(skipWallTypes),
				new Actions.SetTile((ushort)ModContent.TileType<RedSandstoneBrick>()),
				new Modifiers.Dither(0.8),
				new Actions.SetTileKeepWall((ushort)ModContent.TileType<RedSandstoneBrickCracked>())
			)); //Add tile outlines that are non-invasive to rooms

			for (int p = -1; p < a.Width + 1; p++)
			{
				bool isTile = p < 1 || p >= a.Width - 1;
				int tileType = (p < 1 || p >= a.Width - 1) ? ModContent.TileType<RuinedSandstonePillar>() : -1;
				int wallType = (p < 0 || p >= a.Width) ? WallID.None : RedSandstoneBrickCrackedWall.UnsafeType;
				float ease = 1f - EaseFunction.EaseSine.Ease((p + 1f) / (float)(a.Width + 1f));
				int limitY = (tileType == -1) ? Math.Max((int)(ease * 15), 1) : 0;

				DropPillar(p + a.X, a.Bottom + 1, tileType, wallType, out int lowestY, limitY);

				Point basePosition = new(p + a.X, lowestY + 1);
				Tile tile = Framing.GetTileSafely(basePosition);

				if (!isTile && tile.WallType == WallID.None)
					tile.WallType = (ushort)BronzeGrate.UnsafeType;

				if (isTile && tile.Active(TileID.Sand) && WorldGen.TileIsExposedToAir(basePosition.X, basePosition.Y))
					tile.ResetToType((ushort)ModContent.TileType<GildedSandstone>());
			}
		}

		result.Inflate(2, 2);
		new Decorator(result)
			.Enqueue(ZigguratMicropass.PlacePot, segments * 2)
			.Enqueue(ModContent.TileType<AncientBanner>(), WorldGen.genRand.Next(1, 4))
			.Enqueue(ZigguratMicropass.PlaceDoor, 1)
			.Enqueue(PlaceTorch, WorldGen.genRand.Next(1, 4))
			.Run();

		return result;

		static Point[] GetUpwardDirections(int length)
		{
			var result = new Point[length];

			for (int c = 0; c < length; c++)
				result[c] = WorldGen.genRand.NextBool() ? new(WorldGen.genRand.NextFromList(-1, 1), 0) : new(0, -1);

			return result;
		}
	}

	private static bool PlaceTorch(int x, int y)
	{
		Tile below = Framing.GetTileSafely(x, y + 1);
		Tile farBelow = Framing.GetTileSafely(x, y + 2);

		return !WorldGen.SolidOrSlopedTile(below) && WorldGen.SolidOrSlopedTile(farBelow) && Placer.PlaceTile<ZigguratTorch>(x, y).success;
	}

	private static void DropPillar(int x, int y, int tileType, int wallType, out int lowestY, int length = 0)
	{
		lowestY = y;
		int currentLength = 0;

		while ((length == 0 || currentLength < length) && WorldGen.InWorld(x, y, 20) && !WorldGen.SolidOrSlopedTile(x, y) && Main.tile[x, y] is Tile tile && !Main.wallHouse[tile.WallType])
		{
			if (wallType != WallID.None)
				tile.WallType = (ushort)wallType;
			if (tileType != -1)
				WorldGen.PlaceTile(x, y, tileType, true);

			lowestY = y;
			currentLength++;
			y++;
		}
	}
	#endregion
}