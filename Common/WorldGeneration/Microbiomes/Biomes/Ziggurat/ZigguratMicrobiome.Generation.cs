using Microsoft.CodeAnalysis;
using ReLogic.Utilities;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.WorldGeneration.Micropasses.Passes;
using SpiritReforged.Common.WorldGeneration.Noise;
using SpiritReforged.Content.Desert;
using SpiritReforged.Content.Forest.Cartography.Maps;
using SpiritReforged.Content.Forest.Cloud.Items;
using SpiritReforged.Content.Forest.Misc;
using SpiritReforged.Content.Savanna.Items.Gar;
using SpiritReforged.Content.Savanna.Tiles.Paintings;
using SpiritReforged.Content.Underground.Tiles;
using SpiritReforged.Content.Ziggurat;
using SpiritReforged.Content.Ziggurat.Scarab;
using SpiritReforged.Content.Ziggurat.Tiles;
using SpiritReforged.Content.Ziggurat.Tiles.Chains;
using SpiritReforged.Content.Ziggurat.Tiles.Furniture;
using SpiritReforged.Content.Ziggurat.Walls;
using SpiritReforged.Content.Ziggurat.Windshear;
using System.IO;
using System.Linq;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;
using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes.Ziggurat;

public partial class ZigguratMicrobiome : Microbiome
{
	/// <summary> The maximum width of the biome. </summary>
	public const int Width = 180;
	/// <summary> The maximum height of the biome. </summary>
	public const int Height = 90;

	public const int HallwayWidth = 4;

	/// <summary> The full rectangular area this biome encompasses. </summary>
	public Rectangle FullArea => new(Position.X - Width / 2, Position.Y - Height / 2, Width, Height);

	[WorldBound]
	public static readonly HashSet<Rectangle> TotalBounds = [];
	private static HashSet<GenRoom> TotalRooms;

	protected override void OnPlace(Point16 point)
	{
		List<Rectangle> bounds = GetBounds(FullArea);
		CreateShape(FullArea, bounds);

		foreach (Rectangle bound in bounds)
			TotalBounds.Add(bound);

		CreateAltar(bounds[0]);
		AddRooms(bounds, out var rooms);
		TotalRooms = [.. rooms];

		CreateHallways(rooms, AddHallway);

		foreach (var r in rooms)
			r.Create();

		CreateHallways(rooms, AddPassageway);
		Sandify(bounds);

		for (int i = 2; i < bounds.Count; i++)
			Infest(WorldGen.genRand.Next(3), bounds[i]);

		AddNeutralDecorations(rooms);

		WorldDetours.Regions.Add(new(bounds[0], WorldDetours.Context.Walls));
		foreach (var b in bounds)
			WorldDetours.Regions.Add(new(b, WorldDetours.Context.Pots | WorldDetours.Context.Piles));

		TotalRooms = null;
	}

	private static List<Rectangle> GetBounds(Rectangle fullArea)
	{
		//A gradual width multiplier starting from the top layer
		const float minWidth = 0.25f;
		//A height multiplier strictly for the bottom layer
		const float finalHeight = 1.3f;

		List<Rectangle> bounds = [];
		int layers = Math.Max((int)(Main.maxTilesY / 1200f * 2) + 2, 4);

		int finalLayerHeight = (int)(fullArea.Height / layers * finalHeight);
		int commonLayerHeight = (fullArea.Height - finalLayerHeight) / (layers - 1);

		Point center = new(fullArea.Center.X, fullArea.Y + commonLayerHeight / 2);

		for (int y = 0; y < layers; y++)
		{
			bool finalLayer = y == layers - 1;

			int width = (int)MathHelper.Lerp(fullArea.Width * minWidth, fullArea.Width, y / (layers - 1f));
			int height = finalLayer ? finalLayerHeight : commonLayerHeight;

			if (y == 0) //Force starting height
				width = height + 10;

			var topLeft = center - new Point(width / 2, commonLayerHeight / 2);
			Rectangle bound = new(topLeft.X, topLeft.Y, width, height);

			bounds.Add(bound);
			center.Y += height;
		}

		return bounds;
	}

	/// <summary> Creates the basic shape of the ziggurat. </summary>
	/// <param name="fullBounds"> The total area the structure is occupying. </param>
	/// <param name="bounds"></param>
	private static void CreateShape(Rectangle fullBounds, List<Rectangle> bounds)
	{
		for (int y = 0; y < bounds.Count; y++)
		{
			Rectangle bound = bounds[y];
			bool finalLayer = y == bounds.Count - 1;

			WorldUtils.Gen(bound.Location, new Shapes.Rectangle(bound.Width, bound.Height), Actions.Chain(
				new Actions.ClearTile(),
				new Actions.PlaceTile((ushort)ModContent.TileType<RedSandstoneBrick>())
			));

			WorldUtils.Gen(bound.Location + new Point(1, 1), new Shapes.Rectangle(bound.Width - 2, bound.Height), new Actions.PlaceWall((ushort)ModContent.WallType<SandyZigguratWall>()));

			CreateHalo(bound);
			CreateDithering(bound);

			WorldUtils.Gen(bound.Location - new Point(4, 4), new Shapes.Rectangle(bound.Width + 8, bound.Height + 8), new Actions.Custom((x, y, args) =>
			{
				if (x < bound.Left + 7 || x >= bound.Right - 7)
				{
					Tile tile = Main.tile[x, y];
					if (tile.HasTile && tile.TileType == ModContent.TileType<RedSandstoneBrick>() && !WorldGen.SolidTile(x, y - 1))
					{
						tile.ResetToType((ushort)ModContent.TileType<SandySandstone>());
						return true;
					}
				}

				return false;
			})); //Sandy outline
		}

		//Add weathering around some edges of the shape
		ShapeData shape = new();
		ushort brick = (ushort)ModContent.TileType<RedSandstoneBrick>();

		WorldUtils.Gen(fullBounds.Location, new Shapes.Rectangle(fullBounds.Width, fullBounds.Height), Actions.Chain(
			new Modifiers.OnlyTiles(brick),
			new Actions.Blank()
		).Output(shape));

		WorldUtils.Gen(fullBounds.Location, new ModShapes.InnerOutline(shape), Actions.Chain(
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

		static void CreateDithering(Rectangle bounds)
		{
			ushort brick = (ushort)ModContent.TileType<RedSandstoneBrick>();

			WorldUtils.Gen(new(bounds.Left, bounds.Bottom), new Shapes.Rectangle(bounds.Width, 2), Actions.Chain(
				new Modifiers.Dither(),
				new Actions.SetTileKeepWall(brick)
			));
		}
	}

	private static void CreateAltar(Rectangle topBound)
	{
		var origin = (topBound.Top() - new Vector2(0, 1)).ToPoint();
		int i = origin.X;
		int j = origin.Y;

		WorldMethods.FindGround(i, ref j);
		Placer.PlaceTile<ScarabAltar>(i, j -1).PostPlacement(out ScarabAltarEntity _);

		const int width = 8;
		WorldUtils.Gen(new(i - width / 2, j), new Shapes.Rectangle(width, 2), Actions.Chain(
			new Modifiers.OnlyTiles((ushort)ModContent.TileType<RedSandstoneBrick>(), (ushort)ModContent.TileType<SandySandstone>()),
			new Actions.SetTileKeepWall((ushort)ModContent.TileType<GildedRedSandstone>())
		));
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
									new Modifiers.OnlyWalls((ushort)ModContent.WallType<SandyZigguratWall>(), (ushort)RedSandstoneBrickWall.UnsafeType),
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

	private static void AddNeutralDecorations(List<GenRoom> rooms)
	{
		int minChestCount = Main.maxTilesX / 2100;
		PriorityQueue<Point16, float> furniturePositions = new();
		PriorityQueue<Point16, float> chestPositions = new();

		foreach (var room in rooms)
		{
			Rectangle bounds = room.Bounds;
			bounds.Inflate(2, 2);

			Decorator decorator = new Decorator(bounds)
				.Enqueue(ModContent.TileType<AncientBanner>(), 1 / 20f)
				.Enqueue(TileID.Banners, 1 / 20f, new(static () => WorldGen.genRand.Next(4, 7)))
				.Enqueue(PlacePot, 0);

			if (WorldGen.genRand.NextBool(3))
				decorator.Enqueue(PlaceCenser, 1);

			if (room is ZigguratRooms.LibraryRoom)
			{
				decorator.Enqueue(ModContent.TileType<TatteredMapWall>(), 1);
				decorator.Enqueue(ModContent.TileType<TatteredMapWallSmall>(), 1);
			}
			else if (room is ZigguratRooms.TreasureRoom)
			{
				decorator.Enqueue(ModContent.TileType<EnlilStatue>(), 1);
				decorator.Enqueue(ModContent.TileType<ScarabTablet>(), 1, new(static () => WorldGen.genRand.Next(2)));
			}
			else
			{
				decorator.Enqueue((i, j) =>
				{
					if ((WorldGen.SolidTile(i, j + 1) || WorldGen.SolidTile(i, j - 1)))
					{
						if (WorldGen.genRand.NextBool(7))
							furniturePositions.Enqueue(new Point16(i, j), WorldGen.genRand.NextFloat());

						chestPositions.Enqueue(new Point16(i, j), WorldGen.genRand.NextFloat());
						return true;
					}

					return false;
				}, 0);

				if (WorldGen.genRand.NextBool(3))
					decorator.Enqueue(LaySpikeStrip, 1);
			}

			if (room is not ZigguratRooms.TreasureRoom) // Low chance to place scarab tablet in any non-treasure room
				decorator.Enqueue(ModContent.TileType<ScarabTablet>(), 1 / 100f, new(static () => WorldGen.genRand.Next(2)));

			decorator.Run();
		}

		while (minChestCount > 0 && chestPositions.Count > 0)
		{
			Point16 pos = chestPositions.Dequeue();

			if (PlaceFurniture(pos.X, pos.Y, FurnitureSet.Types.Chest))
				minChestCount--;
		}

		while (furniturePositions.Count > 0)
		{
			Point16 pos = furniturePositions.Dequeue();
			PlaceRandomFurniture(pos.X, pos.Y);
		}
	}

	private static bool PlaceCenser(int i, int j)
	{
		int space = GetSpace(i, j, 8);
		if (Framing.GetTileSafely(i, j - 1).HasTileType(ModContent.TileType<RedSandstoneBrick>()) && space > 2 && Placer.PlaceTile<GoldChainLoop>(i, j).success)
		{
			byte segments = (byte)Math.Min(WorldGen.genRand.Next(3, 7), space - 2);
			ChainObjectSystem.AddObject(ModContent.GetInstance<GoldChainLoop>().Find(new(i, j), segments));
			return true;
		}

		return false;

		static int GetSpace(int x, int y, int limit = 0)
		{
			int result = 1;
			while (WorldGen.InWorld(x, y, 20) && !WorldGen.SolidOrSlopedTile(x, y) && (limit == 0 || result < limit))
			{
				y++;
				result++;
			}

			return result;
		}
	}

	private static bool PlacePot(int i, int j)
	{
		if (WorldGen.SolidTile(i, j + 1) && WorldGen.genRand.NextBool(10)) //Place pots
		{
			int type = WorldGen.genRand.NextFromList(ModContent.TileType<BronzePots>(), TileID.Pots);

			if (WorldGen.genRand.NextBool(10))
				type = ModContent.TileType<LapisPots>();
			else if (WorldGen.genRand.NextBool(5))
				type = ModContent.TileType<BiomePots>();

			int style = -1;

			if (type == ModContent.TileType<BiomePots>())
				style = PotsMicropass.GetStyleRange(BiomePots.Style.Desert);

			if (type == TileID.Pots)
				return WorldGen.PlacePot(i, j, style: (ushort)WorldGen.genRand.Next(34, 37));

			return Placer.PlaceTile(i, j, type, style).success;
		}

		return false;
	}

	private static bool LaySpikeStrip(int i, int j)
	{
		bool success = false;
		int width = WorldGen.genRand.Next(3, 6);
		int halfWidth = width / 2;
		int y = j;

		for (int x = i - halfWidth; x < i + halfWidth; x++)
		{
			if (!WorldGen.SolidOrSlopedTile(x, y - 1) && Framing.GetTileSafely(x, y).HasTileType(ModContent.TileType<RedSandstoneBrick>()))
			{
				Framing.GetTileSafely(x, y).ResetToType((ushort)ModContent.TileType<NeedleTrap>());
				success = true;
			}
		}

		return success;
	}

	/// <summary> Places a random lapis furniture tile, excluding chests. </summary>
	/// <param name="i"> The X coordinate. </param>
	/// <param name="j"> The Y coordinate. </param>
	/// <returns> Whether the tile was successfully placed. </returns>
	public static bool PlaceRandomFurniture(int i, int j)
	{
		LapisSet set = ModContent.GetInstance<LapisSet>();

		while (true)
		{
			FurnitureSet.Types type = WorldGen.genRand.Next(Enum.GetValues<FurnitureSet.Types>());

			if (type is not FurnitureSet.Types.Chest && set.TryGetTileType(type, out _))
				return PlaceFurniture(i, j, type);
		}
	}

	/// <summary> Places a lapis furniture item at the provided coordinates. </summary>
	/// <param name="i"> The X coordinate. </param>
	/// <param name="j"> The Y coordinate. </param>
	/// <param name="type"> The furniture type to place. </param>
	public static bool PlaceFurniture(int i, int j, FurnitureSet.Types type)
	{
		LapisSet set = ModContent.GetInstance<LapisSet>();

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

	internal static void PopulateChest(Chest chest)
	{
		List<int> main = [ModContent.ItemType<GildedScarab>(), ModContent.ItemType<CeremonialDagger>(), ModContent.ItemType<WindshearScepter>(), ModContent.ItemType<BangleOfStrength>()];

		// Secondary Slot
		WeightedRandom<(int, Range)> secondary = new();
		secondary.Add((ModContent.ItemType<TornMapPiece>(), 1..2));

		secondary.Add((ItemID.WhitePearl, 2..5));
		secondary.Add((ItemID.PinkPearl, 2..4), 0.5f);
		secondary.Add((ItemID.BlackPearl, 1..3), 0.33f);

		(string name, float weight)[] thoriumLoot =
		[
			("AmberRing", 0.75f),
			("AmethystRing", 0.5f),
			("AquamarineRing", 0.33f),
			("EmeraldRing", 0.33f),
			("OpalRing", 0.33f),
			("RubyRing", 0.33f),
			("SapphireRing", 0.33f),
			("TopazRing", 0.33f),
			("DiamondRing", 0.25f),
			("EighthPlagueStaff", 0.15f)
		];

		foreach (var (name, weight) in thoriumLoot)
		{
			if (CrossMod.Thorium.CheckFind(name, out ModItem thorium))
				secondary.Add((thorium.Type, 1..1), weight);
		}

		if (CrossMod.Redemption.CheckFind("CorpseWalkerStaff", out ModItem walkerStaff))
			secondary.Add((walkerStaff.Type, 1..1), 0.15f);

		// Gems
		WeightedRandom<(int, Range)> gemPool = new();
		gemPool.Add((ItemID.Amethyst, 6..12));
		gemPool.Add((ItemID.Topaz, 5..11));
		gemPool.Add((ItemID.Sapphire, 3..8));
		gemPool.Add((ModContent.GetInstance<CarvedLapis>().AutoItemType(), 15..25));

		if (CrossMod.Thorium.CheckFind("Opal", out ModItem opal))
			gemPool.Add((opal.Type, 4..8));

		if (CrossMod.Thorium.CheckFind("Aquamarine", out ModItem aquamarine))
			gemPool.Add((aquamarine.Type, 4..8));

		if (CrossMod.Verdant.CheckFind("AquamarineItem", out ModItem aquamarineVerdant))
			gemPool.Add((aquamarineVerdant.Type, 4..8));

		// Misc
		PriorityQueue<(int, Range), float> miscQueue = new();
		miscQueue.Enqueue((ItemID.ThrowingKnife, 25..50), WorldGen.genRand.NextFloat());
		miscQueue.Enqueue((ItemID.FlamingArrow, 25..50), WorldGen.genRand.NextFloat());
		miscQueue.Enqueue((ItemID.TrapsightPotion, 1..2), WorldGen.genRand.NextFloat());
		miscQueue.Enqueue((ItemID.NightOwlPotion, 1..2), WorldGen.genRand.NextFloat());
		miscQueue.Enqueue((ModContent.ItemType<QuenchPotion>(), 1..2), WorldGen.genRand.NextFloat());
		miscQueue.Enqueue((ModContent.ItemType<DoubleJumpPotion>(), 1..2), WorldGen.genRand.NextFloat());
		miscQueue.Enqueue((ModContent.ItemType<RemedyPotion>(), 1..2), WorldGen.genRand.NextFloat());
		miscQueue.Enqueue((ItemID.ThornsPotion, 1..2), WorldGen.genRand.NextFloat());
		miscQueue.Enqueue((ItemID.ShinePotion, 1..2), WorldGen.genRand.NextFloat());
		miscQueue.Enqueue((ItemID.BattlePotion, 1..2), WorldGen.genRand.NextFloat());
		miscQueue.Enqueue((ItemID.Rope, 50..100), WorldGen.genRand.NextFloat());
		miscQueue.Enqueue((ItemID.GoldCoin, 1..4), WorldGen.genRand.NextFloat());
		miscQueue.Enqueue((AutoContent.ItemType<WaningSun>(), 1..1), WorldGen.genRand.NextFloat());
		miscQueue.Enqueue((ItemID.ScarabBomb, 5..9), WorldGen.genRand.NextFloat());

		chest.item[0] = new Item(WorldGen.genRand.Next(main));

		var (type, stack) = secondary.Get();
		chest.item[1] = new Item(type, WorldGen.genRand.Next(stack.Start.Value, stack.End.Value));

		var (gemType, gemStack) = gemPool.Get();
		chest.item[2] = new Item(gemType, WorldGen.genRand.Next(gemStack.Start.Value, gemStack.End.Value + 1));

		int miscCount = WorldGen.genRand.Next(3, 5);

		for (int i = 0; i < miscCount; ++i)
		{
			var (miscType, miscStack) = miscQueue.Dequeue();
			chest.item[3 + i] = new Item(miscType, WorldGen.genRand.Next(miscStack.Start.Value, miscStack.End.Value + 1));
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
			selection = WorldGen.genRand.NextFromList<ZigguratRooms.BasicRoom>(new ZigguratRooms.StorageRoom(bound, noise), new ZigguratRooms.LibraryRoom(bound, noise), new ZigguratRooms.BurialRoom(bound, noise));
		else
			selection = new ZigguratRooms.BasicRoom(bound, noise);

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

	#region helpers
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

				int width = Math.Abs(startLink.Location.X - endLink.Location.X);
				int height = Math.Abs(startLink.Location.Y - endLink.Location.Y);

				if (width <= 10 && height <= 2 && WorldGen.genRand.NextBool()) //Add foreground walls
					WorldMethods.GenerateSquared(AddForegroundWalls, out _, new(Math.Min(start.X, end.X) - 2, Math.Min(start.Y, end.Y) - 3, width + 4, height + 6));

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
				new Actions.SetLiquid(0, 0),
				new Actions.Custom(BlockOutWalls)
			));

			WorldUtils.Gen(origin, new ModShapes.OuterOutline(shape), new Actions.Smooth());
		}
	}

	private static bool BlockOutWalls(int i, int j, object args)
	{
		Tile tile = Main.tile[i, j];
		if (tile.WallType == (ushort)ModContent.WallType<SandyZigguratWall>() && !TotalRooms.Any(x => x.Intersects(new Point(i, j), 1)))
		{
			int type = WorldGen.genRand.NextBool(3) ? RedSandstoneBrickCrackedWall.UnsafeType : RedSandstoneBrickWall.UnsafeType;
			tile.WallType = (ushort)type; //Add unsafe walls to hallways

			return true;
		}

		return false;
	}

	private static bool AddForegroundWalls(int i, int j)
	{
		Tile tile = Main.tile[i, j];
		if (!TotalRooms.Any(x => x.Intersects(new Point(i, j), 2)))
		{
			tile.WallType = (ushort)ModContent.WallType<RedSandstoneBrickForegroundWall>(); //Add unsafe walls to hallways
			return true;
		}

		return false;
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
	#endregion

	public override void NetSend(BinaryWriter writer)
	{
		base.NetSend(writer);
	}

	public override void NetReceive(BinaryReader reader)
	{
		base.NetReceive(reader);

		TotalBounds.Clear();
		foreach (Rectangle bound in GetBounds(FullArea))
			TotalBounds.Add(bound);
	}

	public override void WorldSave(TagCompound tag)
	{
		base.WorldSave(tag);
	}

	public override void WorldLoad(TagCompound tag)
	{
		base.WorldLoad(tag);

		TotalBounds.Clear();
		foreach (Rectangle bound in GetBounds(FullArea))
			TotalBounds.Add(bound);
	}
}