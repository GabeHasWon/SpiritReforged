using Microsoft.CodeAnalysis;
using ReLogic.Utilities;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.WorldGeneration.Micropasses.Passes;
using SpiritReforged.Common.WorldGeneration.Noise;
using SpiritReforged.Content.Desert;
using SpiritReforged.Content.Desert.Tiles;
using SpiritReforged.Content.Desert.Tiles.Furniture;
using SpiritReforged.Content.Desert.Walls;
using SpiritReforged.Content.Forest.Cartography.Maps;
using SpiritReforged.Content.Underground.Tiles;
using System.Linq;
using Terraria.DataStructures;
using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes.Ziggurat;

public partial class ZigguratBiome : Microbiome
{
	/// <summary> The maximum width of the biome. </summary>
	public const int Width = 180;
	/// <summary> The maximum height of the biome. </summary>
	public const int Height = 90;

	public const int HallwayWidth = 4;

	private static HashSet<Rectangle> TotalBounds;
	private static HashSet<GenRoom> TotalRooms;

	protected override void OnPlace(Point16 point)
	{
		Rectangle area = new(point.X - Width / 2, point.Y - Height / 2, Width, Height);

		CreateShape(area, 4, out var bounds);
		TotalBounds = [.. bounds];

		CreateAltar(bounds[0]);
		AddRooms(bounds, out var rooms);
		TotalRooms = [.. rooms];

		CreateHallways(rooms, AddHallway);

		foreach (var r in rooms)
			r.Create();

		CreateHallways(rooms, AddPassageway);
		Sandify(bounds);
		SwitchWalls(bounds);
		AddNeutralDecorations(rooms);

		WorldDetours.Regions.Add(new(bounds[0], WorldDetours.Context.Walls));
		foreach (var b in bounds)
		{
			WorldDetours.Regions.Add(new(b, WorldDetours.Context.Pots));
			WorldDetours.Regions.Add(new(b, WorldDetours.Context.Piles));
		}

		TotalBounds = null;
		TotalRooms = null;
	}

	/// <summary> Creates the basic shape of the ziggurat. </summary>
	/// <param name="area"> The total area the structure can occupy. </param>
	/// <param name="layers"> The number of distinct layers in the structure. </param>
	/// <param name="bounds"></param>
	private static void CreateShape(Rectangle area, int layers, out List<Rectangle> bounds)
	{
		//A gradual width multiplier starting from the top layer
		const float minWidth = 0.25f;
		//A height multiplier strictly for the bottom layer
		const float finalHeight = 1.3f;

		int finalLayerHeight = (int)(area.Height / layers * finalHeight);
		int commonLayerHeight = (area.Height - finalLayerHeight) / (layers - 1);
		bounds = [];

		Point center = new(area.Center.X, area.Y + commonLayerHeight / 2);

		for (int y = 0; y < layers; y++)
		{
			bool finalLayer = y == layers - 1;

			int width = (int)MathHelper.Lerp(area.Width * minWidth, area.Width, y / (layers - 1f));
			int height = finalLayer ? finalLayerHeight : commonLayerHeight;

			if (y == 0) //Force starting height
				width = height + 10;

			var topLeft = center - new Point(width / 2, commonLayerHeight / 2);

			WorldUtils.Gen(topLeft, new Shapes.Rectangle(width, height), Actions.Chain(
				new Actions.ClearTile(),
				new Actions.PlaceTile((ushort)ModContent.TileType<RedSandstoneBrick>())
			));

			WorldUtils.Gen(topLeft + new Point(1, 1), new Shapes.Rectangle(width - 2, height), new Actions.PlaceWall(WallID.Sandstone));
			Rectangle bound = new(topLeft.X, topLeft.Y, width, height);

			bounds.Add(bound);
			center.Y += height;

			CreateHalo(bound);
		}

		//Add weathering around some edges of the shape
		ShapeData shape = new();
		ushort brick = (ushort)ModContent.TileType<RedSandstoneBrick>();

		WorldUtils.Gen(area.Location, new Shapes.Rectangle(area.Width, area.Height), Actions.Chain(
			new Modifiers.OnlyTiles(brick),
			new Actions.Blank()
		).Output(shape));

		WorldUtils.Gen(area.Location, new ModShapes.InnerOutline(shape), Actions.Chain(
			new Modifiers.Dither(),
			new Modifiers.Blotches(2, 0.1),
			new Modifiers.IsTouching(false, TileID.Sand),
			new Actions.SetTileKeepWall(TileID.Sand)
		));

		static void CreateHalo(Rectangle bounds)
		{
			const int width = 5;
			ushort brick = (ushort)ModContent.TileType<RedSandstoneBrick>();

			WorldUtils.Gen(new(bounds.X, bounds.Y - 2), new Shapes.Rectangle(width, 2), Actions.Chain(
				new Actions.ClearTile(),
				new Actions.PlaceTile(brick)
			));

			WorldUtils.Gen(new(bounds.X + bounds.Width - width, bounds.Y - 2), new Shapes.Rectangle(width, 2), Actions.Chain(
				new Actions.ClearTile(),
				new Actions.PlaceTile(brick)
			));

			WorldUtils.Gen(new(bounds.X + 10, bounds.Y - 2), new Shapes.Rectangle(width, 2), Actions.Chain(
				new Actions.ClearTile(),
				new Actions.PlaceTile(brick)
			));

			WorldUtils.Gen(new(bounds.X + bounds.Width - width - 10, bounds.Y - 2), new Shapes.Rectangle(width, 2), Actions.Chain(
				new Actions.ClearTile(),
				new Actions.PlaceTile(brick)
			));
		}
	}

	private static void CreateAltar(Rectangle topBound)
	{
		var origin = (topBound.Top() - new Vector2(0, 1)).ToPoint();
		int i = origin.X;
		int j = origin.Y;

		WorldMethods.FindGround(i, ref j);
		WorldGen.PlaceTile(i, j - 1, ModContent.TileType<ScarabAltar>(), true);
	}

	/// <summary> Randomly adds weathering. </summary>
	private static void Sandify(List<Rectangle> bounds)
	{
		foreach (Rectangle b in bounds)
		{
			WorldMethods.GenerateSquared(static (i, j) =>
			{
				//Floor indentations
				var tile = Main.tile[i, j];
				if (WorldGen.genRand.NextBool(50) && tile.HasTile && tile.TileType == ModContent.TileType<RedSandstoneBrick>() && !WorldGen.SolidTile3(i, j - 1))
					CreateFloorDivot(i, j);

				//Large sand splatters
				if (WorldGen.genRand.NextBool(80) && tile.HasTile && tile.TileType == TileID.Sand)
				{
					WorldUtils.Gen(new(i, j), new GenTypes.Splatter(8, 10, 20), Actions.Chain(
						new Modifiers.Blotches(),
						new Modifiers.OnlyTiles((ushort)ModContent.TileType<RedSandstoneBrick>(), (ushort)ModContent.TileType<RedSandstoneBrickCracked>()),
						new Actions.Custom(static (x, y, args) =>
						{
							ushort type = (WorldGen.TileIsExposedToAir(x, y) || NoiseSystem.PerlinStatic(x, y) < 0f) ? TileID.Sandstone : TileID.Sand;
							Main.tile[x, y].ResetToType(type);

							if (type == TileID.Sandstone)
							{
								if (WorldGen.genRand.NextBool(12))
								{
									WorldUtils.Gen(new(x, y), new Shapes.Tail(WorldGen.genRand.Next(4, 8), new Vector2D(0, WorldGen.genRand.Next(4, 9))), Actions.Chain(
										new Modifiers.IsNotSolid(),
										new Actions.SetTileKeepWall(TileID.Sandstone),
										new Modifiers.Expand(2),
										new Modifiers.OnlyTiles((ushort)ModContent.TileType<RuinedSandstonePillar>()),
										new Actions.ClearTile()
									));
								}
								else if (WorldGen.genRand.NextBool(10))
								{
									WorldGen.PlaceTile(x, y + 1, ModContent.TileType<LightShaft>(), true);
								}

								WorldUtils.Gen(new(x, y), new Shapes.Circle(WorldGen.genRand.Next(5, 12)), Actions.Chain(
									new Modifiers.RadialDither(3, 6),
									new Modifiers.OnlyWalls(WallID.Sandstone, (ushort)RedSandstoneBrickWall.UnsafeType),
									new Actions.PlaceWall(WallID.HardenedSand)
								));
							}

							return true;
						}),
						new Actions.PlaceWall(WallID.HardenedSand)
					));
				}

				return false;
			}, out _, b);
		}

		static void CreateFloorDivot(int i, int j) //Creates a decorative sandy indentation
		{
			ShapeData shape = new();
			Point coords = new(i, j);
			int size = WorldGen.genRand.Next(2, 5);

			WorldUtils.Gen(coords, new Shapes.Mound(size, size), Actions.Chain(
				new Modifiers.Flip(false, true),
				new Modifiers.OnlyTiles((ushort)ModContent.TileType<RedSandstoneBrick>()),
				new Actions.Custom((x, y, args) =>
				{
					if (y == j && !Framing.GetTileSafely(x, y - 1).HasTile)
					{
						Main.tile[x, y].ClearTile(); //Clear indentation surface tiles if there's no tile above
					}
					else
					{
						ushort type = WorldGen.SolidTile3(x, y + 1) ? TileID.Sand : TileID.HardenedSand;
						Main.tile[x, y].ResetToType(type);
					}

					return false;
				})
			).Output(shape));

			WorldUtils.Gen(coords, new ModShapes.OuterOutline(shape), Actions.Chain(
				new Modifiers.OnlyTiles((ushort)ModContent.TileType<RedSandstoneBrick>()),
				new Modifiers.Dither(),
				new Actions.SetTileKeepWall((ushort)ModContent.TileType<RedSandstoneBrickCracked>())
			));
		}
	}

	private static void SwitchWalls(List<Rectangle> areas)
	{
		foreach (Rectangle area in areas)
		{
			WorldMethods.GenerateSquared((i, j) =>
			{
				if (!WorldGen.SolidTile(i, j))
				{
					Tile tile = Main.tile[i, j];

					if (tile.WallType == WallID.Sandstone && !TotalRooms.Any(x => x.Intersects(new Point(i, j), 1)))
						tile.WallType = (ushort)RedSandstoneBrickWall.UnsafeType; //Add unsafe walls to hallways
				}

				return false;
			}, out _, area);
		}
	}

	private static void AddNeutralDecorations(List<GenRoom> rooms)
	{
		WeightedRandom<int> potWeight = new();
		potWeight.Add(ModContent.TileType<BronzePots>());
		potWeight.Add(ModContent.TileType<LapisPots>(), 0.1f);
		potWeight.Add(ModContent.TileType<BiomePots>(), 0.2f);
		potWeight.Add(TileID.Pots);

		int maxChestCount = Main.maxTilesX / 2100;
		PriorityQueue<Point16, float> furniturePositions = new();

		foreach (var room in rooms)
		{
			Rectangle bounds = room.Bounds;
			bounds.Inflate(2, 2);

			var decorator = new Decorator(bounds)
				.Enqueue(ModContent.TileType<AncientBanner>(), 1 / 20f)
				.Enqueue(TileID.Banners, 1 / 20f, WorldGen.genRand.Next(4, 8))
				.Enqueue(ModContent.TileType<ScarabTablet>(), 1 / 80f, WorldGen.genRand.Next(0, 2))
				.Enqueue((i, j) =>
				{
					if (WorldGen.SolidTile(i, j + 1) && WorldGen.genRand.NextBool(10)) //Place pots
					{
						int type = potWeight;
						int style = -1;

						if (type == ModContent.TileType<BiomePots>())
							style = PotsMicropass.GetStyleRange(BiomePots.Style.Desert);

						if (type == TileID.Pots)
							return WorldGen.PlacePot(i, j, style: (ushort)WorldGen.genRand.Next(34, 37));

						return Placer.PlaceTile(i, j, type, style).success;
					}

					return false;
				}, 0);

			if (room is ZigguratRooms.LibraryRoom)
			{
				decorator.Enqueue(ModContent.TileType<TatteredMapWall>(), 1);
				decorator.Enqueue(ModContent.TileType<TatteredMapWallSmall>(), 1);
			}
			else if (room is ZigguratRooms.TreasureRoom)
			{
				decorator.Enqueue(TileID.CatBast, 1);
			}
			else
			{
				decorator.Enqueue((i, j) =>
				{
					if ((WorldGen.SolidTile(i, j + 1) || WorldGen.SolidTile(i, j - 1)) && WorldGen.genRand.NextBool(7))
					{
						furniturePositions.Enqueue(new Point16(i, j), WorldGen.genRand.NextFloat());
						return true;
					}

					return false;
				}, 0)
				.Enqueue((i, j) =>
				{
					if (WorldGen.genRand.NextBool(40))
					{
						LaySpikeStrip(new(i, j), WorldGen.genRand.Next(3, 6));
						return true;
					}

					return false;
				}, 0);
			}

			decorator.Run();
		}

		while (furniturePositions.Count > 0)
		{
			Point16 pos = furniturePositions.Dequeue();

			if (PlaceFurniture(pos.X, pos.Y, maxChestCount > 0 ? FurnitureSet.Types.Chest : FurnitureSet.Types.None))
				maxChestCount--;
		}
	}

	public static void LaySpikeStrip(Point origin, int width)
	{
		int halfWidth = width / 2;
		int y = origin.Y;

		for (int x = origin.X - halfWidth; x < origin.X + halfWidth; x++)
		{
			if (!WorldGen.SolidOrSlopedTile(x, y - 1) && Framing.GetTileSafely(x, y).HasTileType(ModContent.TileType<RedSandstoneBrick>()))
				Framing.GetTileSafely(x, y).ResetToType((ushort)ModContent.TileType<NeedleTrap>());
		}
	}

	private static bool PlaceFurniture(int i, int j, FurnitureSet.Types forceType = FurnitureSet.Types.None)
	{
		LapisSet set = ModContent.GetInstance<LapisSet>();
		FurnitureSet.Types type;

		if (forceType == FurnitureSet.Types.None)
		{
			do
			{
				type = WorldGen.genRand.Next(Enum.GetValues<FurnitureSet.Types>());
			} while (type is FurnitureSet.Types.Chest or FurnitureSet.Types.None);
		}
		else
			type = forceType;

		if (set.TryGetTileType(type, out int tileType))
		{
			int style = -1;

			if (type is FurnitureSet.Types.Candle or FurnitureSet.Types.Chandelier or FurnitureSet.Types.Lamp or FurnitureSet.Types.Lantern or FurnitureSet.Types.Candelabra)
				style = 1; //Off states

			if (type is FurnitureSet.Types.Table or FurnitureSet.Types.Chair) //Place an organized table and chair set
			{
				int tableType = set.GetTileType(FurnitureSet.Types.Table);

				if (Placer.Check(i, j, tableType, style).IsClear().Place().success)
				{
					int chairType = set.GetTileType(FurnitureSet.Types.Chair);
					FurnitureSet.Types lightSetType = WorldGen.genRand.NextFromList(FurnitureSet.Types.Candle, FurnitureSet.Types.Candelabra);
					int lightType = set.GetTileType(lightSetType);

					Placer.Check(i - 2, j, chairType, 1).IsClear().Place();
					Placer.Check(i + 2, j, chairType, 0).IsClear().Place();
					
					if (Placer.Check(i, j - 2, lightType, 1).IsClear().Place().success && lightSetType is FurnitureSet.Types.Candle) //Candle style fix
					{
						Main.tile[i, j - 2].TileFrameX = 18;
						Main.tile[i, j - 2].TileFrameY = 0;
					}

					return true;
				}
			}
			else
			{
				bool success = Placer.Check(i, j, tileType, style).IsClear().Place().success;

				if (type == FurnitureSet.Types.Chest)
				{
					int chest = Chest.CreateChest(i, j - 1);

					if (chest != -1)
						PopulateChest(Main.chest[chest]);
				}

				return success;
			}
		}

		return false;
	}

	private static void PopulateChest(Chest chest)
	{
		int[] main = [ItemID.AncientChisel, ItemID.SandBoots];
		(int type, Range stack)[] secondary = [(ItemID.Amethyst, 6..12), (ItemID.Topaz, 5..11), (ItemID.Sapphire, 3..8), 
			(ModContent.GetInstance<CarvedLapis>().AutoItemType(), 15..25), (ModContent.ItemType<TornMapPiece>(), 1..1)];
		
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

	private static void AddRooms(IEnumerable<Rectangle> bounds, out List<GenRoom> rooms)
	{
		const int paddedWidth = HallwayWidth + 10;
		const int paddedHeight = 2;

		rooms = [];

		Rectangle[] orderedBounds = [.. bounds.OrderBy(static x => x.Location.Y)];
		for (int i = 0; i < orderedBounds.Length; i++)
		{
			var bound = orderedBounds[i];
			Rectangle innerBounds = new(bound.X + paddedWidth, bound.Y + paddedHeight, bound.Width - paddedWidth * 2, bound.Height - paddedHeight * 2);

			int totalCount = i + 1;
			int numSkips = 0;

			for (int x = 0; x <= totalCount; x++)
			{
				float progress = (float)x / totalCount;

				if (totalCount == 1)
					progress = 0.5f;

				ZigguratRooms.BasicRoom r = SelectRoom(i + 1, bound, ref progress, numSkips);

				if (r == null)
				{
					numSkips++;
					continue; //Skip
				}

				int originX = (int)MathHelper.Lerp(innerBounds.Left + r.Bounds.Width / 2, innerBounds.Right - r.Bounds.Width / 2, progress);
				Point origin = new(originX, innerBounds.Bottom - r.Bounds.Height / 2);
				r.SetOrigin(origin);

				if (!rooms.Any(x => x.Intersects(r.Bounds, 2))) //Check if rooms would intersect
					rooms.Add(r);
			}
		}
	}

	/// <summary> Chooses the type of room to generate. </summary>
	/// <param name="layer"> The layer this room is being selected for, indexed by one. </param>
	/// <param name="bound"> The bounds of the layer to generate within. </param>
	/// <param name="progress"> Indicates the room number relative to the total that generate on a particular layer. This value influences the final vertical coordinate. </param>
	/// <param name="skips"> The number of skips that have happened in this layer. Returning null here causes a skip. </param>
	private static ZigguratRooms.BasicRoom SelectRoom(int layer, Rectangle bound, ref float progress, int skips)
	{
		int numLayers = TotalBounds.Count;
		ZigguratRooms.RoomNoise noise = new(3);
		ZigguratRooms.BasicRoom selection;

		if (WorldGen.genRand.NextBool(4))
		{
			selection = WorldGen.genRand.NextFromList<ZigguratRooms.BasicRoom>(new ZigguratRooms.StorageRoom(bound, noise), new ZigguratRooms.LibraryRoom(bound, noise));
		}
		else
		{
			selection = new ZigguratRooms.BasicRoom(bound, noise);
		}

		if (layer == numLayers && progress == 0) //Final layer
		{
			selection = new ZigguratRooms.TreasureRoom(bound, noise);
			progress = WorldGen.genRand.NextFloat();
		}
		else if (layer == 1)
		{
			ZigguratRooms.EntranceRoom.StyleID style = WorldGen.genRand.NextFromList(ZigguratRooms.EntranceRoom.StyleID.Large, ZigguratRooms.EntranceRoom.StyleID.Split);

			if (WorldGen.genRand.NextBool())
				style = ZigguratRooms.EntranceRoom.StyleID.Blank;

			selection = new ZigguratRooms.EntranceRoom(bound, style, noise);
		}
		else if (skips == 0 && WorldGen.genRand.NextBool(4))
		{
			return null; //Randomly cause a skip
		}

		return selection;
	}

	private static void CreateHallways(IEnumerable<GenRoom> rooms, Func<GenRoom.Link, GenRoom.Link, bool> condition)
	{
		PairRooms(rooms.OrderBy(static x => x.Origin.Y));

		void PairRooms(IOrderedEnumerable<GenRoom> rooms)
		{
			foreach (var start in rooms)
			{
				foreach (var end in rooms)
				{
					if (start != end && PairLinks(start, end))
						break;
				}
			}
		}

		bool PairLinks(GenRoom a, GenRoom b)
		{
			bool value = false;

			foreach (var start in a.Links)
			{
				foreach (var end in b.Links)
				{
					if (condition.Invoke(start, end))
					{
						start.consumed = true;
						end.consumed = true;

						value |= true;
					}
				}
			}

			return value;
		}
	}

	/// <summary> Creates a ruined passageway outlining <paramref name="startLink"/> and <paramref name="endLink"/>. </summary>
	private static bool AddPassageway(GenRoom.Link startLink, GenRoom.Link endLink)
	{
		var start = startLink.Location;
		var end = endLink.Location;

		//A safe distance from the starting link
		var entrance = new Point(start.X + startLink.Direction.X * HallwayWidth, start.Y + startLink.Direction.Y * HallwayWidth);
		//A safe distance from the ending link
		var exit = new Point(end.X + endLink.Direction.X * HallwayWidth, end.Y + endLink.Direction.Y * HallwayWidth);

		if (!startLink.consumed)
			CrunchOut(start, entrance, 3, true);
		if (!endLink.consumed)
			CrunchOut(end, exit, 3, true);

		return true;
	}

	/// <summary> Attempts to create a hallway spanning <paramref name="startLink"/> and <paramref name="endLink"/>. </summary>
	private static bool AddHallway(GenRoom.Link startLink, GenRoom.Link endLink)
	{
		var start = startLink.Location;
		var end = endLink.Location;

		for (int a = 0; a < 2; a++)
		{
			//A safe distance from the starting link
			Point entrance = new(start.X + startLink.Direction.X * HallwayWidth, start.Y + startLink.Direction.Y * HallwayWidth);
			//A safe distance from the ending link
			Point exit = new(end.X + endLink.Direction.X * HallwayWidth, end.Y + endLink.Direction.Y * HallwayWidth);

			bool sloped = a == 0;
			var down = sloped ? new Point(entrance.X + Math.Abs(end.Y - start.Y) * startLink.Direction.X, end.Y) : new Point(entrance.X, end.Y); //Straight or diagonal down given enough space

			if (!Intersecting(entrance, down, exit) && Contains(entrance) && Contains(down))
			{
				BlockOut(start, entrance, HallwayWidth);
				BlockOut(entrance, down, HallwayWidth);

				BlockOut(down, exit, HallwayWidth);
				BlockOut(exit, end, HallwayWidth);

				if (!sloped) //Avoid placing platforms at sloped entrances
				{
					for (int i = 0; i < 2; i++)
					{
						var shelf = ((i == 0) ? entrance : down) + new Point(-2, 3);
						WorldUtils.Gen(shelf, new Shapes.Rectangle(HallwayWidth, 1), new Actions.PlaceTile((ushort)ModContent.TileType<BronzePlatform>()));
					}
				}

				return true;
			}
		}

		return false;

		static bool Intersecting(params Point[] points)
		{
			const int padding = 2;
			float collisionPoint = 0;

			for (int i = 0; i < points.Length - 1; i++)
			{
				var startVector = points[i].ToVector2();
				var endVector = points[i + 1].ToVector2();

				foreach (var r in TotalRooms)
				{
					if (Collision.CheckAABBvLineCollision(r.Bounds.TopLeft() - new Vector2(padding), r.Bounds.Size() + new Vector2(padding * 2), startVector, endVector, HallwayWidth, ref collisionPoint))
						return true;
				}
			}

			return false;
		}
	}

	/// <summary> Clears and smooths tiles between <paramref name="start"/> and <paramref name="end"/> based on <paramref name="width"/>. </summary>
	public static void BlockOut(Point start, Point end, int width)
	{
		Vector2 startCoords = new(start.X, start.Y);
		Vector2 endCoords = new(end.X, end.Y);

		int length = (int)Math.Round(startCoords.Distance(endCoords));

		if (length <= 1)
			return;

		for (int i = 0; i <= length; i++)
		{
			var intermediate = Vector2.Lerp(startCoords, endCoords, (float)i / length);
			if (intermediate.HasNaNs())
				break;

			Point origin = new((int)(intermediate.X - width / 2), (int)(intermediate.Y - width / 2));
			ShapeData shape = new();

			WorldUtils.Gen(origin, new Shapes.Rectangle(width, width), Actions.Chain(
				new Actions.ClearTile(true).Output(shape),
				new Actions.SetLiquid(0, 0)
			));

			WorldUtils.Gen(origin, new ModShapes.OuterOutline(shape), new Actions.Smooth());
		}
	}

	/// <summary> Clears red sandstone bricks between <paramref name="start"/> and <paramref name="end"/> according to <paramref name="carve"/> and <paramref name="width"/>. </summary>
	public static void CrunchOut(Point start, Point end, int width, bool carve)
	{
		ShapeData shape = new();
		Point direction = end - start;

		if (direction == Point.Zero)
			return;

		if (carve)
		{
			WorldUtils.Gen(start, new Shapes.Tail(width, direction.ToVector2D()), Actions.Chain(
				new Modifiers.OnlyTiles((ushort)ModContent.TileType<RedSandstoneBrick>()),
				new Modifiers.Dither(),
				new Actions.ClearTile()
			).Output(shape));

			WorldUtils.Gen(start, new ModShapes.OuterOutline(shape), Actions.Chain(
				new Modifiers.OnlyTiles((ushort)ModContent.TileType<RedSandstoneBrick>()),
				new Modifiers.Dither(),
				new Actions.SetTileKeepWall((ushort)ModContent.TileType<RedSandstoneBrickCracked>())
			));
		}
		else
		{
			WorldUtils.Gen(start, new Shapes.Tail(width, direction.ToVector2D()), Actions.Chain(
				new Modifiers.OnlyTiles((ushort)ModContent.TileType<RedSandstoneBrick>()),
				new Modifiers.Dither(),
				new Actions.SetTileKeepWall((ushort)ModContent.TileType<RedSandstoneBrickCracked>())
			).Output(shape));
		}
	}

	/// <summary> Checks whether <paramref name="point"/> is contained within the structure. </summary>
	public static bool Contains(Point point)
	{
		foreach (var bound in TotalBounds)
		{
			if (bound.Contains(point))
				return true;
		}

		return false;
	}
}