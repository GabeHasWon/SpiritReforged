using SpiritReforged.Common.ItemCommon.Backpacks;
using SpiritReforged.Common.WorldGeneration.Microbiomes;
using SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes;
using SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes.Ziggurat;
using SpiritReforged.Common.WorldGeneration.Micropasses.Discoveries.Passes;
using SpiritReforged.Common.WorldGeneration.Micropasses.Passes;
using SpiritReforged.Common.WorldGeneration.PointOfInterest;
using SpiritReforged.Content.Desert.Silk;
using SpiritReforged.Content.Forest.Backpacks;
using SpiritReforged.Content.Forest.Botanist.Items;
using SpiritReforged.Content.Forest.Botanist.Tiles;
using SpiritReforged.Content.Forest.Stargrass.Tiles;
using SpiritReforged.Content.Forest.WoodClub;
using SpiritReforged.Content.Ocean.Items.KoiTotem;
using SpiritReforged.Content.Ocean.Items.Reefhunter.OceanPendant;
using SpiritReforged.Content.Ocean.Items.Vanity.DiverSet;
using SpiritReforged.Content.SaltFlats.Items;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using SpiritReforged.Content.Savanna.Items.HuntingRifle;
using SpiritReforged.Content.Savanna.Items.Vanity;
using SpiritReforged.Content.Savanna.Tiles;
using SpiritReforged.Content.Vanilla.Leather.HideTunic;
using SpiritReforged.Content.Ziggurat.Walls;
using SpiritReforged.Content.Ziggurat.Windshear;
using System.Linq;
using Terraria.DataStructures;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.ModCompat;

internal class NewBeginningsCompat : ModSystem
{
	public static Asset<Texture2D> GetIcon(string name) => ModContent.Request<Texture2D>("SpiritReforged/Common/ModCompat/NewBeginningsOrigins/" + name);

	public override bool IsLoadingEnabled(Mod mod) => CrossMod.NewBeginnings.Enabled;
	public override void Load()
	{
		var beginnings = CrossMod.NewBeginnings.Instance;

		beginnings.Call("Delay", () =>
		{
			AddDiver();
			AddBotanist();
			AddRecluse();
			AddHunter();
			AddHiker();
			AddCaveman();
			AddDisentombed();
			AddWorshipper();
			AddPurifier();
		});

		void AddDiver()
		{
			object equip = beginnings.Call("EquipData", ModContent.ItemType<DiverHead>(), ModContent.ItemType<DiverBody>(), ModContent.ItemType<DiverLegs>(),
				new int[] { ItemID.Flipper, ModContent.ItemType<OceanPendant>() });
			object misc = beginnings.Call("MiscData", 100, 20, -1);
			object dele = GetDelegateData(() => true, list => { }, () => true, FindBeachSpawnPoint);
			AddOrigin("Diver", [], equip, misc, dele);
		}

		void AddBotanist()
		{
			object equip = beginnings.Call("EquipData", ModContent.ItemType<BotanistHat>(), ModContent.ItemType<BotanistBody>(), ModContent.ItemType<BotanistLegs>(),
				Array.Empty<int>());
			object misc = beginnings.Call("MiscData", 100, 20, -1, ItemID.Sickle);
			object dele = GetDelegateData(() => true, list => { }, () => true, FindScarecrowSpawnPoint);
			AddOrigin("Botanist", [(ItemID.HerbBag, 3)], equip, misc, dele);
		}

		void AddRecluse()
		{
			object equip = beginnings.Call("EquipData", ItemID.AnglerHat, ItemID.None, ItemID.None, Array.Empty<int>());
			object misc = beginnings.Call("MiscData", 100, 20, -1);
			object dele = GetDelegateData(() => true, list => { }, () => true, FindRecluseSpawn);
			AddOrigin("Recluse", [(ItemID.FiberglassFishingPole, 1), (ItemID.MasterBait, 2), (ItemID.ApprenticeBait, 10), (ItemID.WoodenCrate, 3), (ItemID.Torch, 25)], equip, misc, dele);
		}

		void AddHunter()
		{
			object equip = beginnings.Call("EquipData", ModContent.ItemType<SafariHat>(), ModContent.ItemType<SafariVest>(), ModContent.ItemType<SafariShorts>(), 
				Array.Empty<int>());
			object misc = beginnings.Call("MiscData", 100, 20, -1, ModContent.ItemType<HuntingRifle>());
			object dele = GetDelegateData(() => true, list => { }, () => true, FindHunterSpawn);
			AddOrigin("Hunter", [(ItemID.MusketBall, 60)], equip, misc, dele);
		}

		void AddHiker()
		{
			object equip = beginnings.Call("EquipData", ItemID.None, ItemID.None, ItemID.None, Array.Empty<int>());
			object misc = beginnings.Call("MiscData", 100, 20, -1);
			object dele = GetDelegateData(() => true, list => { }, () => true, FindHighestSurface, AddHikerBackpack);
			AddOrigin("Hiker", [(ItemID.Rope, 150), (ItemID.Glowstick, 30), (ItemID.Torch, 60)], equip, misc, dele);
		}

		void AddCaveman()
		{
			object equip = beginnings.Call("EquipData", ItemID.None, ModContent.ItemType<HideTunic>(), ItemID.None, Array.Empty<int>());
			object misc = beginnings.Call("MiscData", 100, 20, -1, ModContent.ItemType<WoodenClub>());
			object dele = GetDelegateData(() => true, list => { }, () => true, SpawnUnderground);
			AddOrigin("Caveman", [], equip, misc, dele);
		}

		void AddDisentombed()
		{
			object equip = beginnings.Call("EquipData", ItemID.MummyMask, ItemID.MummyShirt, ItemID.MummyPants, Array.Empty<int>());
			object misc = beginnings.Call("MiscData", 100, 20, -1);
			object dele = GetDelegateData(() => true, list => { }, () => true, SpawnInZiggurat);
			AddOrigin("Disentombed", [], equip, misc, dele);
		}

		void AddWorshipper()
		{
			object equip = beginnings.Call("EquipData", ModContent.ItemType<SunEarrings>(), ModContent.ItemType<SilkTop>(), ModContent.ItemType<SilkSirwal>(), Array.Empty<int>());
			object misc = beginnings.Call("MiscData", 100, 20, -1, ModContent.ItemType<WindshearScepter>());
			object dele = GetDelegateData(() => true, list => { }, () => true, SpawnInOasis);
			AddOrigin("Worshipper", [], equip, misc, dele);
		}

		void AddPurifier()
		{
			object equip = beginnings.Call("EquipData", ModContent.ItemType<MahakalaMaskBlue>(), ItemID.None, ItemID.None, Array.Empty<int>());
			object misc = beginnings.Call("MiscData", 100, 20, -1, ModContent.ItemType<BoStaff>());
			object dele = GetDelegateData(() => true, list => { }, () => true, TrySpawnInSaltFlats, player =>
			{
				if (Main.rand.NextBool()) // Randomize mask on creation
					player.armor[0] = new Item(ModContent.ItemType<MahakalaMaskRed>());
			});

			AddOrigin("Purifier", [], equip, misc, dele);
		}

		void AddOrigin(string name, (int, int)[] inventory, object equipData, object miscData, object delegateData) 
			=> beginnings.Call("ShortAddOrigin", GetIcon(name), "Reforged" + name, "Mods.SpiritReforged.Origins." + name, inventory, equipData, miscData, delegateData);
	}

	private static Point16 TrySpawnInSaltFlats()
	{
		List<Point16> points = new(500);

		for (int i = WorldGen.beachDistance; i < Main.maxTilesX - WorldGen.beachDistance; ++i)
		{
			for (int j = (int)(Main.worldSurface * 0.35); j < Main.worldSurface; ++j)
			{
				Tile tile = Main.tile[i, j];

				if (tile.HasTile && (tile.TileType == ModContent.TileType<SaltBlockDull>() || tile.TileType == ModContent.TileType<SaltBlockReflective>()) && 
					!Collision.SolidCollision(new Vector2(i, j - 3) * 16, 54, 54) && Main.tile[i, j - 2].WallType == WallID.None)
					points.Add(new Point16(i, j - 3));
			}
		}

		if (points.Count == 0)
			return Point16.NegativeOne;

		return WorldGen.genRand.Next(points);
	}

	private static Point16 SpawnInOasis()
	{
		if (UndergroundOasisBiome.OasisAreas.Count == 0)
			return Point16.NegativeOne;

		Rectangle selection = WorldGen.genRand.NextFromList([.. UndergroundOasisBiome.OasisAreas]);
		return new(selection.Center.X, selection.Center.Y);
	}

	private static Point16 SpawnInZiggurat()
	{
		var ziggurats = MicrobiomeSystem.Microbiomes.Where(x => x is ZigguratMicrobiome).ToHashSet();

		if (ziggurats.Count == 0)
			return Point16.NegativeOne;

		Point16 pos = WorldGen.genRand.Next([.. ziggurats]).Position;
		Point16 spawn;
		Tile tile;

		do
		{
			spawn = new Point16(pos.X + WorldGen.genRand.Next(-100, 300), pos.Y + WorldGen.genRand.Next(-200, 200));
			tile = Main.tile[spawn];
		} while (tile.HasTile || !(tile.WallType == ModContent.WallType<RedSandstoneBrickCrackedWall>() || tile.WallType == ModContent.WallType<RedSandstoneBrickWall>())
			|| Collision.SolidCollision(spawn.ToWorldCoordinates() - new Vector2(36), 72, 72) || !Collision.SolidCollision(spawn.ToWorldCoordinates() + new Vector2(-18, 36), 36, 16));

		return spawn;
	}

	private static Point16 SpawnUnderground()
	{
		for (int i = 0; i < 30000; ++i)
		{
			Point16 point = new(WorldGen.genRand.Next(WorldGen.beachDistance * 2, Main.maxTilesX - WorldGen.beachDistance * 2), 
				WorldGen.genRand.Next((int)Main.worldSurface + 100, (int)Main.worldSurface + 400));

			if (Collision.SolidCollision(point.ToWorldCoordinates() - new Vector2(36), 72, 72))
				continue;

			if (!Collision.SolidCollision(point.ToWorldCoordinates() + new Vector2(-18, 36), 36, 16))
				continue;

			const int Range = 70;

			int stoneCount = 150;

			for (int x = point.X - Range; x < point.X + Range; ++x)
			{
				for (int y = point.Y - Range; y < point.Y + Range; ++y)
				{
					Tile tile = Main.tile[x, y];

					if (tile.HasTile && tile.TileType is TileID.Stone or TileID.Dirt or TileID.Granite or TileID.Marble or TileID.MushroomGrass)
						stoneCount--;
					else if (tile.HasTile && tile.TileType is TileID.JungleGrass or TileID.CorruptGrass or TileID.CrimsonGrass or TileID.Ash)
						stoneCount++;
				}
			}

			if (stoneCount > 0)
				continue;

			return point;
		}

		return Point16.NegativeOne;
	}

	public static object GetDelegateData(Func<bool> condition, Action<List<GenPass>> modifyWorldGen , Func<bool> hasCustomSpawn, Func<Point16> actualSpawn) 
		=> ((Mod)CrossMod.NewBeginnings).Call("DelegateData", condition, modifyWorldGen, hasCustomSpawn, actualSpawn);

	public static object GetDelegateData(Func<bool> condition, Action<List<GenPass>> modifyWorldGen, Func<bool> hasCustomSpawn, Func<Point16> actualSpawn,
		Action<Player> modifyCreation) => ((Mod)CrossMod.NewBeginnings).Call("DelegateData", condition, modifyWorldGen, hasCustomSpawn, actualSpawn, modifyCreation);

	private Point16 FindHighestSurface()
	{
		int dir = WorldGen.genRand.NextBool() ? -1 : 1;
		Point position = new(Main.spawnTileX, Main.spawnTileY);
		Point16 highest = new(Main.spawnTileX, Main.spawnTileY);

		HashSet<int> moveDownTiles = [TileID.Cloud, TileID.RainCloud];
		HashSet<int> skipVertTiles = [TileID.LivingWood, TileID.LeafBlock];
		HashSet<int> grasses = [TileID.Grass, ModContent.TileType<StargrassTile>(), ModContent.TileType<SavannaGrass>()];

		while (WorldGen.InWorld(position.X, position.Y, 40))
		{
			position.X += dir;

			if (Main.tile[position].HasTile && !skipVertTiles.Contains(Main.tile[position].TileType))
			{
				while (WorldGen.SolidOrSlopedTile(position.X, position.Y))
				{
					if (!moveDownTiles.Contains(Main.tile[position.X, position.Y].TileType))
						position.Y--;
					else
						position.Y++;
				}
			}

			if (highest.Y > position.Y && grasses.Contains(Main.tile[position.X, position.Y + 1].TileType))
				highest = new Point16(position.X, position.Y);
		}

		return highest;
	}

	private void AddHikerBackpack(Player player)
	{
		player.GetModPlayer<BackpackPlayer>().backpack = new Item(ModContent.ItemType<LeatherBackpack>(), 1);
		(player.GetModPlayer<BackpackPlayer>().backpack.ModItem as BackpackItem).items[0] = new Item(ItemID.CalmingPotion, 3);
	}

	private static Point16 FindHunterSpawn()
	{
		List<Point16> spawns = [];

		for (int x = 200; x < Main.maxTilesX - 200; ++x)
		{
			for (int y = (int)(Main.worldSurface * 0.35f); y < Main.worldSurface + 120; ++y)
			{
				Tile tile = Main.tile[x, y];

				if (tile.HasTile && tile.TileType == ModContent.TileType<SavannaGrass>())
					spawns.Add(new Point16(x, y - 3));
			}
		}

		if (spawns.Count == 0)
			return new Point16(Main.spawnTileX, Main.spawnTileY - 3);

		return WorldGen.genRand.Next([.. spawns]);
	}

	private static Point16 FindRecluseSpawn()
	{
		var area = WorldGen.genRand.Next([.. FishingAreaMicropass.Coves]);

		for (int x = area.Left; x < area.Right; x++)
			for (int y = area.Top; x < area.Bottom; y++)
			{
				if (Main.tile[x, y].HasTile && Main.tile[x, y].TileType == ModContent.TileType<KoiTotemTile>())
					return new Point16(x, y);
			}

		return new Point16(Main.spawnTileX, Main.spawnTileY);
	}

	private static Point16 FindScarecrowSpawnPoint()
	{
		Point16 position = new(ScarecrowDiscovery.Position);
		return (position == Point16.Zero) ? Point16.NegativeOne : position;
	}

	public static Point16 FindBeachSpawnPoint()
	{
		bool left = WorldGen.genRand.NextBool(2);
		int x = left ? 280 : Main.maxTilesX - 280;
		int y = 80;

		while (Main.tile[x, y].LiquidAmount <= 0 && !Main.tile[x, y].HasTile)
			y++;

		return new Point16(x, y - 8);
	}
}