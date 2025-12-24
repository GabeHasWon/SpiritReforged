using Humanizer;
using SpiritReforged.Common;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.Tree;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Common.WorldGeneration.Ecotones;
using SpiritReforged.Common.WorldGeneration.Noise;
using SpiritReforged.Common.WorldGeneration.SecretSeeds;
using SpiritReforged.Common.WorldGeneration.SecretSeeds.Seeds;
using SpiritReforged.Content.Desert.Tiles;
using SpiritReforged.Content.Forest.Cartography.Maps;
using SpiritReforged.Content.SaltFlats.Items;
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
	private readonly record struct FeatureInfo(int X, int Y, int Radius)
	{
		public readonly Rectangle Area => new(X - Radius / 2, Y - Radius / 2, Radius, Radius);
	}

	[WorldBound]
	public static Rectangle SaltArea;

	//public static int AverageY { get; private set; }
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
		else if (EcotoneSurfaceMapping.FindWhere(x => x.SurroundedBy("Desert", "Snow") && !EcotoneSurfaceMapping.OverSpawn(x) && EcotoneSurfaceMapping.OnSurface(x)) is EcotoneSurfaceMapping.EcotoneEntry entry)
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

		//The strength of the sine for dull salt padding
		const float baseCurveStrength = 5;
		//The base depth of reflective salt padding
		const int baseDepth = 30;

		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.SaltFlats");

		int xLeft = bounds.Item1;
		int xRight = bounds.Item2;

		int yLeft = EcotoneSurfaceMapping.TotalSurfaceY[(short)xLeft];
		int yRight = EcotoneSurfaceMapping.TotalSurfaceY[(short)xRight];

		Noise = new FastNoiseLite(WorldGen.genRand.Next());
		Noise.SetFrequency(0.03f);

		SaltArea = new Rectangle(xLeft, Math.Min(yLeft, yRight) - 5, Math.Abs(xRight - xLeft), Math.Abs(yRight - yLeft) + baseDepth + 20);
		SaltFlatsSystem.SurfaceHeight = (int)MathHelper.Lerp(yLeft, yRight, 0.5f) - WorldGen.genRand.Next(-3, 9);

		int steps = Math.Min(SaltArea.Width / 200, 3);
		int finalLength = (xRight - xLeft) / steps;

		for (int i = 0; i < steps; i++)
		{
			Point stretch = new(xLeft + finalLength * i, xLeft + finalLength * (i + 1));

			if (stretch.Y < xRight)
				stretch.Y += 10;

			FillArea(stretch.X, stretch.Y, baseCurveStrength, baseDepth);
		}

		Decorate();

		WorldDetours.Regions.Add(new(SaltArea, WorldDetours.Context.Piles));
	}

	private static Rectangle FillArea(int left, int right, float baseCurveStrength, int baseDepth)
	{
		List<FeatureInfo> caves = [];
		List<FeatureInfo> lakes = [];

		int top = EcotoneSurfaceMapping.TotalSurfaceY[(short)left];
		int bottom = EcotoneSurfaceMapping.TotalSurfaceY[(short)right];
		int fullWidth = right - left;
		int averageHeight = (int)MathHelper.Lerp(top, bottom, 0.5f);

		Rectangle area = new(left, Math.Min(top, bottom), right - left, Math.Max(top - bottom, bottom - top));

		for (int x = left; x < right; x++)
		{
			float xProgress = (float)(x - left) / fullWidth;
			float ease = EaseFunction.EaseSine.Ease(xProgress); //Causes tapering around the edges of the biome

			int depthNoise = (int)(Noise.GetNoise(x, 600) * 8);
			int reflectiveDepth = Math.Min((int)(ease * (baseCurveStrength * baseDepth)), baseDepth + depthNoise);
			int liningDepth = 8 + (int)(Noise.GetNoise(x, 500) * 6);

			int y = (int)(Main.worldSurface * 0.35); //Sky height
			int yMax = averageHeight + reflectiveDepth + liningDepth;

			while (y < yMax)
			{
				int surfaceLine = FindSurfaceLine(x, y, area, averageHeight, (float)(y - averageHeight) / yMax, out bool isLining);
				int type = (!isLining && y >= averageHeight && y < averageHeight + reflectiveDepth) ? ModContent.TileType<SaltBlockReflective>() : ModContent.TileType<SaltBlockDull>();

				if (WorldMethods.CloudsBelow(x, y, out int addY))
				{
					y += addY;
					continue;
				}

				if (y == surfaceLine && isLining && WorldGen.genRand.NextBool(50))
				{
					MapFeature(new(x, y), WorldGen.genRand.Next(6, 12), ref lakes); //Occasionally map lakes on the surface
				}

				if (y == yMax - 1) //The final vertical coordinates - fill
				{
					if (depthNoise < 0 && xProgress > 0.05f && xProgress < 0.95f && WorldGen.genRand.NextBool(10))
					{
						MapFeature(new(x, y), WorldGen.genRand.Next(3, 9), ref caves); //Occasionally map caves in crests
					}

					if (!Main.tile[x, y].HasTile)
					{
						const int fillLimit = 30;
						WorldMethods.ApplyOpenArea((i, j) =>
						{
							if (j > surfaceLine && Vector2.DistanceSquared(new Vector2(x, y), new Vector2(i, j)) < fillLimit * fillLimit * 0.1f) //Do a distance check for a naturally rounded fill shape
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
					}
				}

				int wallType = (type == ModContent.TileType<SaltBlockDull>() && !WorldGen.TileIsExposedToAir(x, y)) ? SaltWall.UnsafeType : WallID.None;
				SetTile(x, y++, surfaceLine, type, wallType);
			}
		}

		foreach (var c in caves)
			GenerateCave(c);

		foreach (var l in lakes)
			GenerateLake(l);

		return area;
	}

	private static int FindSurfaceLine(int x, int y, Rectangle bounds, int surface, float depthProgress, out bool isLining)
	{
		//The number of tiles around the biome that can ease into surrounding elevation
		const int mergeDistance = 30;

		float surfaceNoise = Noise.GetNoise(x, 100) * 2;
		int xStart = x - bounds.Left;
		int leftHeight = EcotoneSurfaceMapping.TotalSurfaceY[(short)bounds.Left];
		int rightHeight = EcotoneSurfaceMapping.TotalSurfaceY[(short)bounds.Right];

		isLining = IsLining(x, y, bounds.Left, bounds.Right, depthProgress);

		if (xStart < mergeDistance || xStart > bounds.Width - mergeDistance) //Merging
		{
			isLining = true;
			float floatingLine;

			if (xStart < mergeDistance)
				floatingLine = MathHelper.Lerp(leftHeight, surface, (float)xStart / mergeDistance);
			else
				floatingLine = MathHelper.Lerp(surface, rightHeight, (float)(xStart - (bounds.Width - mergeDistance)) / mergeDistance);

			return (int)floatingLine;
		}
		else if (isLining) //Lining
		{
			return (int)(surface + surfaceNoise) - 1;
		}
		else //Center fill
		{
			float doubleProgress = Math.Max(EaseFunction.EaseSine.Ease(xStart / bounds.Width * 3), 0);
			return (int)Math.Min(surface + surfaceNoise * 2f * doubleProgress, surface);
		}
	}

	/// <summary> Whether the provided coordinates are included in the horizontal or vertical biome lining. </summary>
	private static bool IsLining(int x, int y, int left, int right, float depthProgress)
	{
		//The percentage of space surrounding the biome that will be considered 'lining'
		float width = Math.Clamp(EaseFunction.EaseSine.Ease(Noise.GetNoise(1, 1)), 0.1f, 0.4f);

		float progress = (float)(x - left) / (right - left);
		float sine = EaseFunction.EaseSine.Ease((progress - width) / (1 - width * 2));
		bool pastLiningWidth = progress < width || progress > 1 - width;
		bool pastEaseHeight = depthProgress > sine / 2;

		return pastLiningWidth || pastEaseHeight;
	}

	#region features
	private static void Decorate()
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
		}, out _, SaltArea);

		new Decorator(SaltArea)
			.Enqueue(PlaceReliquary, Math.Max(SaltArea.Width / 150, 1))
			.Enqueue(PlaceSaltwortPatch, Math.Max(SaltArea.Width / 80, 1))
			.Run();
	}

	private static bool PlaceReliquary(int i, int j)
	{
		Tile tile = Main.tile[i, j];
		Tile belowTile = Main.tile[i, j + 1];

		if (!WorldGen.SolidTile(tile) && tile.WallType == WallID.None && belowTile.HasTileType(ModContent.TileType<SaltBlockDull>()))
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
			Tile belowTile = Main.tile[x, j + 1];

			if (!WorldGen.SolidTile(tile) && tile.WallType == WallID.None && tile.LiquidAmount < 20 && belowTile.HasTileType(ModContent.TileType<SaltBlockDull>()))
			{
				int type = WorldGen.genRand.NextBool(10) ? ModContent.TileType<SaltwortTall>() : ModContent.TileType<Saltwort>();
				anySuccess |= Placer.PlaceTile(x, j, type).success;
			}
		}

		return anySuccess;
	}

	private static void MapFeature(Point coordinates, int radius, ref List<FeatureInfo> features)
	{
		int x = coordinates.X;
		int y = coordinates.Y;
		FeatureInfo info = new(x, y, radius);

		if (AreaSafe(info.Area))
			features.Add(info);
	}

	private static void GenerateCave(FeatureInfo info)
	{
		Point origin = new(info.X, info.Y);

		ShapeData data = new();
		ShapeData outlineData = new();

		WorldUtils.Gen(origin, new Shapes.Slime(info.Radius, WorldGen.genRand.NextFloat(0.5f, 1), WorldGen.genRand.NextFloat(0.5f, 1)), new Actions.ClearTile().Output(data));

		WorldUtils.Gen(origin, new ModShapes.OuterOutline(data), Actions.Chain(
			new Modifiers.Blotches(),
			new Modifiers.SkipWalls((ushort)SaltWall.UnsafeType),
			new Actions.SetTileKeepWall((ushort)ModContent.TileType<SaltBlockDull>())
		).Output(outlineData));

		WorldUtils.Gen(origin, new ModShapes.All(data), Actions.Chain(
			new Modifiers.Expand(1),
			new Actions.PlaceWall((ushort)SaltWall.UnsafeType)
		));
	}

	private static void GenerateLake(FeatureInfo info)
	{
		Point origin = new(info.X, info.Y);
		ShapeData data = new();

		float widthScale = WorldGen.genRand.NextFloat(0.5f, 1);
		float heightScale = WorldGen.genRand.NextFloat(0.7f, 1);

		WorldUtils.Gen(origin - new Point(0, 4), new Shapes.Circle((int)(info.Radius * Math.Min(widthScale, heightScale))), Actions.Chain(
			new Actions.ClearTile(),
			new Actions.ClearWall()
		));

		WorldUtils.Gen(origin, new Shapes.Slime(info.Radius, widthScale, heightScale), Actions.Chain(
			new Modifiers.Flip(false, true),
			new Actions.ClearTile(),
			new Modifiers.Blotches()
		).Output(data));

		WorldUtils.Gen(origin, new ModShapes.All(data), Actions.Chain(
			new Modifiers.Expand(2),
			new Actions.ClearWall()
		));

		WorldUtils.Gen(origin, new ModShapes.All(data), Actions.Chain(
			new Modifiers.Offset(0, 2),
			new Actions.SetLiquid()
		));

		WorldUtils.Gen(origin, new ModShapes.OuterOutline(data), Actions.Chain(
			new Modifiers.Blotches(),
			new Modifiers.RectangleMask(-(info.Radius + 4), info.Radius + 4, 0, info.Radius + 4),
			new Modifiers.Expand(1),
			new Actions.SetTileKeepWall((ushort)ModContent.TileType<SaltBlockDull>())
		));
	}
	#endregion

	private static void SetTile(int x, int y, int baseLine, int type, int wallType = -1)
	{
		var t = Main.tile[x, y];

		if (!IsSafe(t))
			return;

		if (y < baseLine)
		{
			t.ClearEverything();
		}
		else
		{
			t.HasTile = true;
			t.TileType = (ushort)type;
			t.Slope = SlopeType.Solid;

			if (wallType != -1)
			{
				t.WallType = (ushort)wallType;
			}
		}
	}

	private static bool AreaSafe(Rectangle area)
	{
		for (int x = area.Left; x < area.Right; x++)
		{
			for (int y = area.Top; y < area.Bottom; y++)
			{
				if (!WorldGen.InWorld(x, y, 8) || !IsSafe(Main.tile[x, y]))
					return false;
			}
		}

		return true;
	}

	private static bool IsSafe(Tile t)
	{
		int type = t.TileType;
		return (TileID.Sets.GeneralPlacementTiles[type] || type == TileID.Ebonstone || type == TileID.Crimstone) && !SpiritSets.DungeonWall[t.WallType];
	}
}