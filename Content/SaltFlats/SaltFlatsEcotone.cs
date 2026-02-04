using SpiritReforged.Common;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.Tree;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Common.WorldGeneration.Ecotones;
using SpiritReforged.Common.WorldGeneration.Noise;
using SpiritReforged.Common.WorldGeneration.SecretSeeds;
using SpiritReforged.Common.WorldGeneration.SecretSeeds.Seeds;
using SpiritReforged.Content.Forest.Cartography.Maps;
using SpiritReforged.Content.SaltFlats.Items;
using SpiritReforged.Content.SaltFlats.Items.Crates;
using SpiritReforged.Content.SaltFlats.Tiles;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using SpiritReforged.Content.SaltFlats.Walls;
using System.Linq;
using Terraria.DataStructures;
using Terraria.GameContent.Generation;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Content.SaltFlats;

internal class SaltFlatsEcotone : EcotoneBase
{
	/// <summary>Contains info for generating the biome surface.</summary>
	/// <param name="Left">The left merge coordinates.</param>
	/// <param name="Right">The right merge coordinates.</param>
	/// <param name="Depth">The base depth of reflective salt padding.</param>
	/// <param name="CurveStrength">The strength of the sine for dull salt padding.</param>
	/// <param name="OuterLength">The length of dull salt padding on the biome outskirts.</param>
	/// <param name="TransitionLength">The length of smoothing to neighboring elevations.</param>
	private readonly record struct SurfaceInfo(Point Left, Point Right, int Depth, float CurveStrength, int OuterLength, int TransitionLength)
	{
		public Rectangle Area
		{
			get
			{
				int minX = Math.Min(Left.X, Right.X);
				int minY = Math.Min(Left.Y, Right.Y);

				int maxX = Math.Max(Left.X, Right.X);
				int maxY = Math.Max(Left.Y, Right.Y);

				return new(minX, minY, maxX - minX, maxY - minY);
			}
		}
	}

	private static FastNoiseLite Noise;

	protected override void Load() => TileEvents.OnPlacePot += ConvertPot;

	/// <summary> Converts pots placed atop dull salt into stone stupas. </summary>
	private static bool ConvertPot(int x, int y, ushort type, int style)
	{
		if (WorldGen.generatingWorld)
		{
			var ground = Main.tile[x, y + 1];

			if (ground.HasTile && ground.TileType == ModContent.TileType<SaltBlockDull>())
			{
				WorldGen.PlaceTile(x, y, ModContent.TileType<StoneStupas>(), true, style: WorldGen.genRand.Next(0, 3));
				return false;
			}
		}

		return true;
	}

	public override void AddTasks(List<GenPass> tasks, List<EcotoneSurfaceMapping.EcotoneEntry> entries)
	{
		if (tasks.FindIndex(x => x.Name == "Beaches") is int index && index != -1)
			tasks.Insert(index, new PassLegacy("Salt Flats", Generation));
	}

	private static bool CanGenerate(out (int, int) bounds)
	{
		const int offX = EcotoneSurfaceMapping.TransitionLength + 2; //Removes forest patches on the left side
		bounds = (0, 0);

		if (SecretSeedSystem.WorldSecretSeed is SaltSeed)
		{
			if (EcotoneSurfaceMapping.FindWhere(EcotoneSurfaceMapping.OverSpawn) is EcotoneSurfaceMapping.EcotoneEntry entry)
			{
				bounds = (entry.Start.X - offX, entry.End.X);
				return true;
			}
		}
		else if (EcotoneSurfaceMapping.FindWhere(x => x.SurroundedBy("Desert", "Snow") && !EcotoneSurfaceMapping.OverSpawn(x) 
			&& EcotoneSurfaceMapping.OnSurface(x)) is EcotoneSurfaceMapping.EcotoneEntry entry && (WorldGen.getGoodWorldGen || entry.Width < 420))
		{
			bounds = (entry.Start.X - offX, entry.End.X);
			return true; //Uniquely, salt flats cannot normally generate over spawn
		}

		return false;
	}

	private static void Generation(GenerationProgress progress, GameConfiguration configuration)
	{
		if (!CanGenerate(out var bounds))
			return;

		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.SaltFlats");

		int leftBound = bounds.Item1;
		int rightBound = bounds.Item2;

		Noise = new FastNoiseLite(WorldGen.genRand.Next());
		Noise.SetFrequency(0.03f);

		int steps = Math.Clamp((rightBound - leftBound) / 200, 1, 3);
		int finalLength = (rightBound - leftBound) / steps;
		int y = EcotoneSurfaceMapping.TotalSurfaceY[(short)leftBound];
		Rectangle area = Rectangle.Empty;
		SaltFlatsSystem.SurfaceHeight = Main.maxTilesY;

		for (int i = 0; i < steps; i++)
		{
			Point left = new(leftBound + finalLength * i, y);

			int rightX = leftBound + finalLength * (i + 1);
			Point right = new(rightX, y = EcotoneSurfaceMapping.TotalSurfaceY[(short)rightX]);

			int width = right.X - left.X;
			SurfaceInfo info = new(left, right, 30, 5, (int)(width / 5f), (int)(width / 8.5f));

			FillSurface(info);

			int surfaceHeight = (int)MathHelper.Lerp(info.Left.Y, info.Right.Y, 0.5f);

			if (SaltFlatsSystem.SurfaceHeight > surfaceHeight + 22)
				SaltFlatsSystem.SurfaceHeight = surfaceHeight + WorldGen.genRand.Next(22, 25);

			area = (area == Rectangle.Empty) ? info.Area : new(Math.Min(area.X, info.Area.X), Math.Min(area.Y, info.Area.Y), Math.Max(area.Width, info.Area.Right - area.Left), Math.Max(area.Height, info.Area.Bottom - area.Top + info.Depth + 20));
		}

		Decorate(area);

		WorldDetours.Regions.Add(new(area, WorldDetours.Context.Piles));
	}

	private static void FillSurface(SurfaceInfo info)
	{
		List<Point> caveOrigins = [];
		List<Point> lakeOrigins = [];
		int surfaceHeight = (int)MathHelper.Lerp(info.Left.Y, info.Right.Y, 0.5f);

		for (int x = info.Left.X; x < info.Right.X; x++)
		{
			float surfaceNoise = Noise.GetNoise(x, 0);
			int progress = x - info.Left.X;
			float ease = EaseFunction.EaseSine.Ease((float)progress / info.Area.Width); //Causes tapering around the edges of the biome

			int reflectiveDepth = Math.Min((int)(ease * (info.CurveStrength * info.Depth)), info.Depth + (int)(surfaceNoise * 8));
			int liningDepth = 8 + (int)(surfaceNoise * 6);

			int y = (int)(Main.worldSurface * 0.35); //Sky height
			int yMax = surfaceHeight + reflectiveDepth + liningDepth;
			
			if (progress < info.TransitionLength || progress > info.Area.Width - info.TransitionLength)
				yMax = Math.Max(yMax, info.Area.Bottom + liningDepth);

			while (y < yMax)
			{
				#region elevation
				float doubleProgress = Math.Max(EaseFunction.EaseSine.Ease((float)progress / info.Area.Width * 3), 0);
				int horizon = (int)Math.Min(surfaceHeight + surfaceNoise * 5 * doubleProgress, surfaceHeight);

				if (progress < info.TransitionLength)
					horizon = (int)MathHelper.Lerp(info.Left.Y, horizon, (float)progress / info.TransitionLength);
				else if (progress > info.Area.Width - info.TransitionLength)
					horizon = (int)MathHelper.Lerp(horizon, info.Right.Y, (float)(progress - (info.Area.Width - info.TransitionLength)) / info.TransitionLength);
				#endregion

				int type = (y >= surfaceHeight && y < surfaceHeight + reflectiveDepth) ? ModContent.TileType<SaltBlockReflective>() : ModContent.TileType<SaltBlockDull>();

				if (y - horizon > EaseFunction.EaseSine.Ease((float)(progress - info.OuterLength) / (info.Area.Width - info.OuterLength * 2)) * reflectiveDepth * 2)
					type = ModContent.TileType<SaltBlockDull>();

				if (y < horizon)
					type = -1;

				if (WorldMethods.CloudsBelow(x, y, out int addY))
				{
					y += addY;
					continue;
				}

				int wallType = (type == ModContent.TileType<SaltBlockDull>() && !WorldGen.TileIsExposedToAir(x, y)) ? SaltWall.UnsafeType : WallID.None;
				SetTile(x, y++, type, wallType);
			}

			if (!Main.tile[x, y].HasTile) //The final vertical coordinates - fill
				FillBlob(x, y, 10);

			if (progress > info.TransitionLength && progress < info.Area.Width - info.TransitionLength)
			{
				AddObject(x, y - WorldGen.genRand.Next(liningDepth), WorldGen.genRand.NextBool(20), ref caveOrigins);
				AddObject(x, y - liningDepth - reflectiveDepth, (progress < info.OuterLength || progress > info.Area.Width - info.OuterLength) && WorldGen.genRand.NextBool(50), ref lakeOrigins);
			}
		}

		foreach (Point o in caveOrigins)
			CreateCave(o, WorldGen.genRand.Next(3, 10));

		foreach (Point o in lakeOrigins)
			CreateLake(o, WorldGen.genRand.Next(6, 10));

		static void AddObject(int x, int y, bool condition, ref List<Point> list) //Improves readability
		{
			if (condition && IsSafe(Main.tile[x, y]))
				list.Add(new(x, y));
		}
	}

	#region features
	private static void FillBlob(int x, int y, int fillLimit) => WorldMethods.ApplyOpenArea((i, j) =>
	{
		if (Vector2.DistanceSquared(new Vector2(x, y), new Vector2(i, j)) < fillLimit * fillLimit * 0.1f) //Do a distance check for a naturally rounded fill shape
		{
			var t = Main.tile[i, j];
			if (IsSafe(t))
			{
				t.HasTile = true;
				t.TileType = (ushort)ModContent.TileType<SaltBlockDull>();
				t.Slope = SlopeType.Solid;
			}
		}

		return false;
	}, x, y, new Rectangle(x - fillLimit / 2, y - fillLimit / 2, fillLimit, fillLimit));

	private static void Decorate(Rectangle area)
	{
		HashSet<Vector2> treePoints = [];
		WorldMethods.GenerateSquared((i, j) =>
		{
			Tile tile = Main.tile[i, j];
			if (tile.HasTile && tile.TileType == ModContent.TileType<SaltBlockDull>())
			{
				bool leftEmpty = !WorldGen.SolidTile3(i - 1, j);
				bool rightEmpty = !WorldGen.SolidTile3(i + 1, j);
				Tile aboveTile = Main.tile[i, j - 1];

				if (!WorldGen.SolidOrSlopedTile(aboveTile) && (leftEmpty || rightEmpty)) //Slopes
				{
					tile.Clear(TileDataType.Slope);

					if (WorldGen.genRand.NextBool(4))
						tile.IsHalfBlock = true;
					else
						tile.Slope = leftEmpty ? SlopeType.SlopeDownRight : SlopeType.SlopeDownLeft;

					return false;
				}

				if (!WorldGen.SolidTile(i, j - 1) && aboveTile.LiquidAmount < 20)
				{
					if (WorldGen.genRand.NextBool(12))
						Placer.PlaceTile<StoneStupas>(i - 1, j - 1, WorldGen.genRand.Next(0, 3));

					if (WorldGen.genRand.NextBool(24))
						Placer.PlaceTile<SaltDebrisTiny>(i, j - 1);

					if (WorldGen.genRand.NextBool(24))
						Placer.PlaceTile<SaltDebrisSmall>(i, j - 1);

					if (WorldGen.genRand.NextBool(30))
						Placer.PlaceTile<SaltDebrisMedium>(i, j - 1);

					if (WorldGen.genRand.NextBool(36))
						Placer.PlaceTile<SaltDebrisLarge>(i, j - 1);

					if (WorldGen.genRand.NextBool(48))
						Placer.PlaceTile<Rowboat>(i, j - 1);

					Vector2 pt = new(i, j - 1);

					if (aboveTile.WallType == WallID.None && WorldGen.genRand.NextBool(35) && !treePoints.Any(x => x.DistanceSQ(pt) < 8 * 8) && CustomTree.GrowTree<DeadTree>(i, j - 1))
						treePoints.Add(pt);
				}

				if (!WorldGen.SolidTile(i, j + 1) && WorldGen.genRand.NextBool(6))
					Placer.PlaceTile<SaltStalactite>(i, j + 1);

				if (!WorldGen.SolidTile(i, j + 1) && WorldGen.genRand.NextBool(30))
					Placer.PlaceTile(i, j + 1, TileID.DyePlants, 7);
			}

			return false;
		}, out _, area);

		int ruinCount = Math.Min(area.Width / 50, 2);
		Decorator decorator = new(area);
		decorator.Enqueue(PlaceReliquary, Math.Max(area.Width / 150, 1)).Enqueue(PlaceSaltwortPatch, Math.Max(area.Width / 80, 1));

		if (ruinCount > 0)
			decorator.Enqueue(CreateRuin, ruinCount);

		decorator.Run();
	}

	private static bool PlaceReliquary(int i, int j)
	{
		Tile tile = Main.tile[i, j];
		Tile belowTile = Main.tile[i, j + 1];

		if (!WorldGen.SolidTile(tile) && tile.WallType == WallID.None && tile.LiquidAmount < 120 && belowTile.HasTileType(ModContent.TileType<SaltBlockDull>()))
		{
			int type = ModContent.TileType<StoneReliquary>();
			bool result = Placer.PlaceTile(i, j, type).success;

			if (result)
			{
				TileExtensions.GetTopLeft(ref i, ref j);
				if (Chest.CreateChest(i, j) is int search && search != -1)
				{
					PopulateChest(Main.chest[search]);
					return true;
				}

				return false;
			}

			return false;
		}

		return false;
	}

	private static void PopulateChest(Chest chest)
	{
		int[] main = [ModContent.ItemType<MahakalaMaskBlue>(), ModContent.ItemType<MahakalaMaskRed>(), ModContent.ItemType<BoStaff>()];
		(int type, Range stack)[] secondary = [(ItemID.Amethyst, 6..12), (ItemID.Topaz, 5..11), (ItemID.Sapphire, 3..8), (ModContent.ItemType<TornMapPiece>(), 1..2)];

		PriorityQueue<(int, Range), float> miscQueue = new();
		miscQueue.Enqueue((ItemID.ThrowingKnife, 5..11), WorldGen.genRand.NextFloat());
		miscQueue.Enqueue((ItemID.TrapsightPotion, 1..2), WorldGen.genRand.NextFloat());
		miscQueue.Enqueue((ItemID.NightOwlPotion, 1..2), WorldGen.genRand.NextFloat());
		miscQueue.Enqueue((ItemID.SwiftnessPotion, 1..2), WorldGen.genRand.NextFloat());
		miscQueue.Enqueue((ItemID.IronskinPotion, 1..2), WorldGen.genRand.NextFloat());
		miscQueue.Enqueue((ItemID.Rope, 15..25), WorldGen.genRand.NextFloat());
		miscQueue.Enqueue((ItemID.GoldCoin, 1..4), WorldGen.genRand.NextFloat());
		miscQueue.Enqueue((ItemID.SilverCoin, 4..14), WorldGen.genRand.NextFloat());

		chest.item[0] = new Item(WorldGen.genRand.Next(main));

		var (type, stack) = WorldGen.genRand.Next(secondary);
		chest.item[1] = new Item(type, WorldGen.genRand.Next(stack.Start.Value, stack.End.Value + 1));

		int miscCount = WorldGen.genRand.Next(3, 5);

		for (int i = 0; i < miscCount; ++i)
		{
			var (miscType, miscStack) = miscQueue.Dequeue();
			chest.item[2 + i] = new Item(miscType, WorldGen.genRand.Next(miscStack.Start.Value, miscStack.End.Value + 1));
		}
	}

	private static bool PlaceSaltwortPatch(int i, int j)
	{
		int halfSize = WorldGen.genRand.Next(5, 16);
		bool anySuccess = false;

		for (int x = i - halfSize; x < i + halfSize; x++)
		{
			WorldMethods.FindGround(x, ref j);
			j--;

			Tile tile = Main.tile[x, j];
			Tile belowTile = Framing.GetTileSafely(x, j + 1);

			if (!WorldGen.SolidTile(tile) && tile.WallType == WallID.None && tile.LiquidAmount < 20 && belowTile.HasTileType(ModContent.TileType<SaltBlockDull>()) && (belowTile.BottomSlope || belowTile.Slope == SlopeType.Solid))
			{
				int type = WorldGen.genRand.NextBool(10) ? ModContent.TileType<SaltwortTall>() : ModContent.TileType<Saltwort>();
				anySuccess |= Placer.PlaceTile(x, j, type).success;
			}
			else if (anySuccess)
			{
				return true;
			}
		}

		return anySuccess;
	}

	private static void CreateCave(Point origin, int radius)
	{
		ShapeData data = new();
		WorldUtils.Gen(origin, new Shapes.Slime(radius, WorldGen.genRand.NextFloat(0.5f, 1), WorldGen.genRand.NextFloat(0.5f, 1)), new Actions.ClearTile().Output(data));

		WorldUtils.Gen(origin, new ModShapes.OuterOutline(data), Actions.Chain(
			new Modifiers.Blotches(),
			new Modifiers.SkipWalls((ushort)SaltWall.UnsafeType),
			new Actions.SetTileKeepWall((ushort)ModContent.TileType<SaltBlockDull>())
		));

		WorldUtils.Gen(origin, new ModShapes.All(data), Actions.Chain(
			new Modifiers.Expand(1),
			new Actions.PlaceWall((ushort)SaltWall.UnsafeType)
		));
	}

	private static void CreateLake(Point origin, int radius)
	{
		ShapeData data = new();

		const float widthScale = 1f;
		const float heightScale = 0.7f;

		WorldUtils.Gen(origin - new Point(0, 4), new Shapes.Circle((int)(radius * Math.Min(widthScale, heightScale))), Actions.Chain(
			new Actions.ClearTile(),
			new Actions.ClearWall()
		));

		WorldUtils.Gen(origin, new Shapes.Slime(radius, widthScale, heightScale), Actions.Chain(
			new Modifiers.Flip(false, true),
			new Actions.ClearTile(),
			new Modifiers.Blotches()
		).Output(data));

		WorldUtils.Gen(origin, new ModShapes.All(data), Actions.Chain(
			new Modifiers.Expand(2),
			new Actions.ClearWall()
		));

		WorldUtils.Gen(origin, new ModShapes.All(data), Actions.Chain(
			new Modifiers.Offset(0, 1),
			new Actions.SetLiquid()
		));

		WorldUtils.Gen(origin, new ModShapes.OuterOutline(data), Actions.Chain(
			new Modifiers.Blotches(),
			new Modifiers.RectangleMask(-(radius + 4), radius + 4, 0, radius + 4),
			new Modifiers.Expand(1),
			new Actions.SetTileKeepWall((ushort)ModContent.TileType<SaltBlockDull>())
		));
	}

	private static bool CreateRuin(int x, int y)
	{
		const int scanRadius = 5;

		if (WorldGen.SolidOrSlopedTile(x, y) || Main.tile[x, y].WallType != WallID.None || !WorldUtils.Find(new(x, y), new Searches.Down(30).Conditions(new Conditions.IsSolid()), out Point foundPos))
			return false;

		int width = WorldGen.genRand.Next(3, 5) * 2 - 1;
		int height = width;
		Rectangle area = new(foundPos.X - width / 2, foundPos.Y - height, width, height);

		if (!GenVars.structures.CanPlace(area, 0))
			return false;

		ushort[] saltTypes = [(ushort)ModContent.TileType<SaltBlockDull>(), (ushort)ModContent.TileType<SaltBlockReflective>()];
		Dictionary<ushort, int> typeToCount = [];
		WorldUtils.Gen(foundPos, new Shapes.Circle(scanRadius), new Actions.TileScanner(saltTypes).Output(typeToCount));

		if (typeToCount[saltTypes[1]] == 0 && typeToCount[saltTypes[0]] > scanRadius * scanRadius)
		{
			ShapeData data = new();
			bool hasRoof = false;
			ushort[] wallTypes = [WallID.BlueStainedGlass, WallID.OrangeStainedGlass, WallID.YellowStainedGlass];

			WorldUtils.Gen(foundPos, new Shapes.Rectangle(new(-(width / 2), -height, width, height)), Actions.Chain(
				new Actions.Clear(),
				new Actions.PlaceWall((ushort)CobbledBrickWall.UnsafeType),
				new Modifiers.RectangleMask(-(width / 2) + 2, width / 2 - 2, -height, -2),
				new Actions.PlaceWall(wallTypes[WorldGen.genRand.Next(wallTypes.Length)])
			).Output(data)); //Add stained glass windows

			WorldUtils.Gen(foundPos, new ModShapes.OuterOutline(data), new Actions.SetTileKeepWall((ushort)ModContent.TileType<CobbledBrick>()));

			if (WorldGen.genRand.NextBool())
			{
				int roofWidth = width + 3;
				int roofHeight = roofWidth / 2 - 1;

				CreateRoof(new(foundPos.X - roofWidth / 2, foundPos.Y - height - roofHeight, roofWidth, roofHeight), 4, 2);
				hasRoof = true;
			}

			WorldUtils.Gen(foundPos, new Shapes.Rectangle(new(-(width / 2), 1, width, 1)), Actions.Chain(
				new Actions.SetTileKeepWall((ushort)ModContent.TileType<CobbledBrick>()),
				new Modifiers.Expand(0, 5),
				new Modifiers.Offset(0, 5),
				new Actions.Custom(static (x, y, args) =>
				{
					if (!WorldGen.SolidTile(x, y))
					{
						Main.tile[x, y].ResetToType((ushort)ModContent.TileType<CobbledBrick>());
						return true;
					}

					return false;
				})
			)); //Add a foundation to merge into uneven terrain

			WorldUtils.Gen(foundPos, new Shapes.Rectangle(new(-(width / 2), -height, width, height)), Actions.Chain(
				new Modifiers.Expand(10, 4),
				new Modifiers.OnlyTiles(saltTypes[0]),
				new Modifiers.IsTouchingAir(),
				new Modifiers.Offset(0, -1),
				new Modifiers.RadialDither(5, 10),
				new Actions.PlaceWall(WallID.WroughtIronFence)
			)); //Add fences nearby

			List<GenAction> actions = hasRoof ? [new Modifiers.RectangleMask(-(width / 2), width / 2, -height, -2), new Modifiers.Expand(1), new Modifiers.Dither(0.9), new Modifiers.SkipWalls(wallTypes), new Actions.Clear()] 
				: [new Modifiers.RectangleMask(-(width / 2), width / 2, -height, -4), new Modifiers.Expand(1), new Actions.Clear(), new Modifiers.Dither(0.9), new Modifiers.SkipWalls(wallTypes), new Actions.Clear()];
			
			WorldUtils.Gen(foundPos, new ModShapes.All(data), Actions.Chain(actions.ToArray())); //Add weathering

			Decorator decorator = new(area);
			decorator.Enqueue(ModContent.TileType<SaltCrateRestored.SaltCrateRestoredTile>(), 0.1f).Enqueue(ModContent.TileType<StoneStupas>(), 0.1f);

			if (!hasRoof && WorldGen.genRand.NextBool())
				decorator.Enqueue(PlaceBell, 1);

			decorator.Run();

			GenVars.structures.AddProtectedStructure(area);
			WorldDetours.Regions.Add(new(area, WorldDetours.Context.Walls));

			return true;
		}

		return false;
	}

	/// <summary> Creates a peaked roof based on <paramref name="length"/>. </summary>
	/// <param name="bounds"> The total bounds the roof encompasses. </param>
	/// <param name="thickness"> The vertical thickness of the roof. </param>
	/// <param name="length"> The flat outer length of the roof. </param>
	private static void CreateRoof(Rectangle bounds, int thickness, int length)
	{
		bounds.Height -= length; //Automatic height calibration

		int halfThickness = thickness / 2;
		int halfWidth = bounds.Width / 2;

		//Add Cobbled brick foundation
		WorldUtils.Gen(new(bounds.Left + 1, bounds.Bottom), new Shapes.Rectangle(2, 4), new Actions.SetTileKeepWall((ushort)ModContent.TileType<CobbledBrick>()));
		WorldUtils.Gen(new(bounds.Right - 2, bounds.Bottom), new Shapes.Rectangle(2, 4), new Actions.SetTileKeepWall((ushort)ModContent.TileType<CobbledBrick>()));

		//Add Cobbled brick walls
		WorldUtils.Gen(new(bounds.Left + 2, bounds.Bottom), new Shapes.Rectangle(1, bounds.Height * 2), Actions.Chain(
			new Modifiers.Blotches(),
			new Modifiers.Dither(),
			new Actions.PlaceWall((ushort)CobbledBrickWall.UnsafeType)
		));
		WorldUtils.Gen(new(bounds.Right - 2, bounds.Bottom), new Shapes.Rectangle(1, bounds.Height * 2), Actions.Chain(
			new Modifiers.Blotches(),
			new Modifiers.Dither(),
			new Actions.PlaceWall((ushort)CobbledBrickWall.UnsafeType)
		));

		if (length >= bounds.Width)
		{
			for (int y = 0; y < thickness; y++)
			{
				Utils.PlotTileLine(bounds.BottomLeft(), bounds.BottomRight(), thickness, PlaceTile);
			}
		}
		else
		{
			for (int y = -halfThickness; y < halfThickness; y++)
			{
				Point top = new(bounds.Center.X, bounds.Top + y - 1);

				Point innerLeft = new(bounds.Left + length, Math.Min(bounds.Bottom + y, bounds.Bottom - 1));
				Point innerRight = new(bounds.Right - length, Math.Min(bounds.Bottom + y, bounds.Bottom - 1));
				Point outerLeft = new(bounds.Left, Math.Min(bounds.Bottom + y, bounds.Bottom - 1));
				Point outerRight = new(bounds.Right, Math.Min(bounds.Bottom + y, bounds.Bottom - 1));

				Utils.PlotLine(outerLeft, innerLeft, PlaceTile);
				Utils.PlotLine(innerLeft, top + new Point(1, -1), PlaceTile);
				Utils.PlotLine(outerRight, innerRight, PlaceTile);
				Utils.PlotLine(innerRight, top + new Point(-1, -1), PlaceTile);
			}
		}

		int padding = halfWidth;
		WorldUtils.Gen(bounds.Location - new Point(0, padding), new Shapes.Rectangle(bounds.Width, bounds.Height + padding * 2), new Actions.Custom(WeatherTop));
		Placer.PlaceTile(bounds.Center.X, bounds.Top + halfThickness - 1, ModContent.TileType<CalmingBell>());

		static bool PlaceTile(int x, int y)
		{
			Tile tile = Framing.GetTileSafely(x, y);
			tile.ResetToType((ushort)ModContent.TileType<BrownShingles>());

			return true;
		}

		static bool WeatherTop(int x, int y, object args)
		{
			Tile tile = Main.tile[x, y];
			ushort defaultType = (ushort)ModContent.TileType<BrownShingles>();
			ushort altType = (ushort)ModContent.TileType<WoodenShingles>();

			if (tile.HasTile && tile.TileType == defaultType)
			{
				if (!WorldGen.SolidTile(x, y - 1))
					tile.ResetToType(altType);

				Tile left = Framing.GetTileSafely(x - 1, y);
				Tile right = Framing.GetTileSafely(x + 1, y);
				Tile downLeft = Framing.GetTileSafely(x - 1, y + 1);
				Tile downRight = Framing.GetTileSafely(x + 1, y + 1);

				if (!left.HasTile && downLeft.HasTile && (downLeft.TileType == defaultType || downLeft.TileType == altType))
					tile.Slope = SlopeType.SlopeDownRight;
				else if (!right.HasTile && downRight.HasTile && (downRight.TileType == defaultType || downRight.TileType == altType))
					tile.Slope = SlopeType.SlopeDownLeft;

				return true;
			}

			return false;
		}
	}

	private static bool PlaceBell(int x, int y)
	{
		if (WorldMethods.AreaClear(x, y - 1, 1, 2, true))
		{
			PlaceWalls(x - 1, y);
			PlaceWalls(x + 1, y);

			for (int c = -1; c < 2; c++)
				WorldGen.PlaceTile(x + c, y, ModContent.TileType<CobbledBrick>(), true);

			return Placer.PlaceTile(x, y + 1, ModContent.TileType<CalmingBell>()).success;
		}

		return false;

		static void PlaceWalls(int x, int y)
		{
			for (int c = 1; c < 10; c++)
			{
				Tile tile = Framing.GetTileSafely(x, y + c);
				if (tile.WallType != WallID.None || WorldGen.SolidOrSlopedTile(tile))
					break;

				tile.WallType = WallID.WroughtIronFence;
			} //Place a series of connecting walls
		}
	}
	#endregion

	private static void SetTile(int x, int y, int type, int wallType)
	{
		Tile tile = Main.tile[x, y];

		if (!IsSafe(tile))
			return;

		if (type == -1)
		{
			tile.ClearEverything();
		}
		else
		{
			tile.HasTile = true;
			tile.TileType = (ushort)type;
			tile.Slope = SlopeType.Solid;
		}

		tile.WallType = (ushort)wallType;
	}

	private static bool IsSafe(Tile t)
	{
		int type = t.TileType;
		return (TileID.Sets.GeneralPlacementTiles[type] || type == TileID.Ebonstone || type == TileID.Crimstone) && !SpiritSets.DungeonWall[t.WallType];
	}
}