using ReLogic.Utilities;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Desert;
using SpiritReforged.Content.Desert.Tiles;
using SpiritReforged.Content.Jungle.Pineapple;
using SpiritReforged.Content.Ziggurat.Walls;
using System.Linq;
using Terraria.DataStructures;
using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes;

#nullable enable
public class UndergroundOasisBiome : Microbiome
{
	private static WeightedRandom<int> MainWaterItem = null!;
	private static WeightedRandom<(int type, Range stackRange, Func<bool>? canPlace)> RandomItem = null!;

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

	public static readonly Point16 Size = new(50, 40);
	public static readonly HashSet<Rectangle> OasisAreas = [];

	public Rectangle Rectangle => new(Position.X - Size.X / 2, Position.Y - Size.Y / 2, Size.X, Size.Y);

	#region detours
	public override void Load()
	{
		NPCEvents.OnEditSpawnRate += ReduceSpawns;
		PlayerEvents.OnPostUpdateEquips += HealInSprings;
		PlayerEvents.OnPostUpdateEquips += HappyInOasis;

        MicrobiomeSystem.PopulateMicrobiomes += static () =>
        {
            OasisAreas.Clear();
            foreach (var b in MicrobiomeSystem.Microbiomes)
            {
                if (b is UndergroundOasisBiome oasis)
                    OasisAreas.Add(oasis.Rectangle);
            }
        };
	}

	private void HappyInOasis(Player player)
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

	#region worldgen
	protected override void OnPlace(Point16 point)
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
		Point lakeOrigin = new(origin.X + Main.rand.Next(-deviation, deviation), origin.Y);
		CarveLake(lakeOrigin);

		PlaceStalactites(origin, radius.X, WorldGen.genRand.Next(4, 8));
		Decorate(origin, shape);
		PlaceLightShafts(origin);

		GenVars.structures.AddProtectedStructure(new Rectangle(origin.X - Size.X / 2, origin.Y - Size.Y / 2, Size.X, Size.Y), 4);

		MainWaterItem = null!;
		RandomItem = null!;
	}

	private static void Decorate(Point origin, ShapeData clearingShape)
	{
		int palmCount = 0;

		WorldUtils.Gen(origin, new ModShapes.All(clearingShape), Actions.Chain(
			new Modifiers.OnlyTiles(TileID.Sand),
			new Actions.Custom((i, j, args) => {
				if (WorldGen.genRand.NextBool(palmCount == 0 ? 5 : 15) && Main.tile[i, j].Slope == SlopeType.Solid && !Main.tile[i, --j].HasTile)
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
					if (WorldGen.genRand.NextBool(3))
					{
						WorldGen.PlaceCatTail(i, j);

						int height = WorldGen.genRand.Next(3, 6);
						for (int h = 0; h < height; h++)
							WorldGen.GrowCatTail(i, j);
					}
				}
				else
				{
					if (WorldGen.genRand.NextBool(3))
						WorldGen.PlaceOasisPlant(i, j);

					if (WorldGen.genRand.NextBool(4))
						Placer.PlaceTile(i, j, ModContent.TileType<Glowflower>());

					if (WorldGen.genRand.NextBool(8))
						Placer.PlaceTile(i, j, ModContent.TileType<PineapplePlant>());

					if (WorldGen.genRand.NextBool(2))
					{
						var t = Main.tile[i, j];

						if (!t.HasTile)
						{
							t.ResetToType(TileID.SeaOats);
							t.HasTile = true;
							t.TileFrameX = (short)(18 * Main.rand.Next(15));
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

		int count = WorldGen.genRand.Next(1, 4);
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

		WorldUtils.Gen(origin, new Shapes.Circle(WorldGen.genRand.Next(6, 11), WorldGen.genRand.Next(3, 6)), Actions.Chain(
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
			MainWaterItem.Add(ItemID.BreathingReed, 0.8);
			MainWaterItem.Add(ItemID.Flipper, 1);
			MainWaterItem.Add(ItemID.Trident, 1);
			MainWaterItem.Add(ItemID.WaterWalkingBoots, 1);
			MainWaterItem.Add(ItemID.MagicConch, 1.5);
			MainWaterItem.Add(ItemID.AncientChisel, 1);
			MainWaterItem.Add(ItemID.MysticCoilSnake, 1);
			MainWaterItem.Add(ItemID.SandBoots, 1);
		}

		if (RandomItem is null) 
		{
			RandomItem = new(WorldGen.genRand);
			RandomItem.Add((TileLoader.GetTile(ModContent.TileType<PolishedAmber>()).AutoItemType(), 3..6, null), 1);
			RandomItem.Add((ItemID.IronBar, 5..14, static () => GenVars.iron == TileID.Iron), 1);
			RandomItem.Add((ItemID.LeadBar, 5..14, static () => GenVars.iron == TileID.Lead), 1);
			RandomItem.Add((ItemID.SilverBar, 5..14, static () => GenVars.silver == TileID.Silver), 1);
			RandomItem.Add((ItemID.TungstenBar, 5..14, static () => GenVars.silver == TileID.Tungsten), 1);
			RandomItem.Add((ItemID.RegenerationPotion, 1..2, null), 0.33);
			RandomItem.Add((ItemID.GillsPotion, 1..2, null), 0.33);
			RandomItem.Add((ItemID.NightOwlPotion, 1..2, null), 0.33);
			RandomItem.Add((ItemID.SwiftnessPotion, 1..2, null), 0.33);
			RandomItem.Add((ItemID.ShinePotion, 1..2, null), 0.33);
			RandomItem.Add((ItemID.ArcheryPotion, 1..2, null), 0.33);
			RandomItem.Add((ItemID.HunterPotion, 1..2, null), 0.33);
			RandomItem.Add((ItemID.MiningPotion, 1..2, null), 0.33);
			RandomItem.Add((ItemID.TrapsightPotion, 1..2, null), 0.33);
			RandomItem.Add((ItemID.RecallPotion, 2..4, null), 1);
			RandomItem.Add((ItemID.Extractinator, 1..1, null), 1);
			RandomItem.Add((ItemID.Bomb, 10..19, null), 1);
			RandomItem.Add((ItemID.ThrowingKnife, 8..15, null), 1.5f);
			RandomItem.Add((ItemID.Shuriken, 8..15, null), 1.5f);
			RandomItem.Add((ItemID.WoodenArrow, 5..12, null), 2f);
		}

		// Try a few times
		for (int i = 0; i < 3; ++i)
		{
			int chestX = origin.X + Main.rand.Next(-2, 3);
			WorldMethods.FindGround(chestX, ref origin.Y);
			int chestIndex = WorldGen.PlaceChest(chestX, origin.Y - 1, TileID.Containers, false, 17);

			if (chestIndex != -1)
			{
				Chest chest = Main.chest[chestIndex];
				chest.item[0] = new(MainWaterItem.Get());
				chest.item[0].Prefix(-1);

				int miscLength = WorldGen.genRand.Next(6, 9);
				HashSet<int> takenRandomIds = [];

				for (int j = 1; j < miscLength; ++j)
				{
					var (type, stackRange, canPlace) = RandomItem.Get();

					while (takenRandomIds.Contains(type) || canPlace?.Invoke() == false)
						(type, stackRange, canPlace) = RandomItem.Get();

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
				if (!(num5 < 0.25) && (num5 < 0.5 && r.NextBool(13) || num5 < 0.7 && r.NextBool(9) || !(num5 < 0.95) || !r.NextBool(5)|| true))
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
}