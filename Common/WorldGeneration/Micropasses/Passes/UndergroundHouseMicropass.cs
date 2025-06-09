using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.WorldGeneration.Micropasses.Passes.MannequinInventories;
using System.Linq;
using System.Reflection;
using Terraria.DataStructures;
using Terraria.GameContent.Biomes.CaveHouse;
using Terraria.GameContent.Tile_Entities;
using Terraria.ModLoader.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class UndergroundHouseMicropass : ModSystem
{
	[Flags]
	private enum AddedHouseFlags : byte
	{
		None = 0,
		Sign = 1,
		Mannequin = 2,
		LoomHouse = 4
	}

	private readonly static Dictionary<Point16, HouseType> dolls = [];

	internal static FieldInfo teDollInventory = null;

	public override void Load()
	{
		teDollInventory = typeof(TEDisplayDoll).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic);

		On_HouseBuilder.Place += PostBuildHouse;
	}

	private void PostBuildHouse(On_HouseBuilder.orig_Place orig, HouseBuilder self, HouseBuilderContext context, StructureMap structures)
	{
		orig(self, context, structures);

		if (self.Type != HouseType.Wood && self.Type != HouseType.Ice)
			return;

		bool hasPlaced = false;
		List<Rectangle> rooms = [];
		AddedHouseFlags skipFlags = 0b_000;

		foreach (Rectangle room in self.Rooms)
		{
			int y = room.Height - 1 + room.Y;

			if (!skipFlags.HasFlag(AddedHouseFlags.Sign) && WorldGen.genRand.NextBool(5) && PlaceDecorInRoom(room, room.Y + 1, TileID.Signs) && self.Type == HouseType.Wood)
			{
				hasPlaced = true;
				skipFlags |= AddedHouseFlags.Sign;
			}

			if (!skipFlags.HasFlag(AddedHouseFlags.Mannequin) && WorldGen.genRand.NextBool(4) 
				&& PlaceDecorInRoom(room, y, WorldGen.genRand.NextBool() ? TileID.Womannequin : TileID.Mannequin, Main.rand.Next(2)))
			{
				hasPlaced = true;
				skipFlags |= AddedHouseFlags.Mannequin;
			}

			if (!skipFlags.HasFlag(AddedHouseFlags.LoomHouse) && WorldGen.genRand.NextBool(10) && self.Type == HouseType.Wood)
			{
				if (PlaceDecorInRoom(room, y, TileID.Loom, Main.rand.Next(2)))
				{
					int topY = room.Y + 2;
					int count = room.Width / 9;

					for (int i = 0; i < count; ++i)
						PlaceDecorInRoom(room, topY, TileID.Banners, Main.rand.Next(4));

					skipFlags |= AddedHouseFlags.LoomHouse;
				}
			}

			if (hasPlaced)
				rooms.Add(room);

			if (((byte)skipFlags & 0b_111) == 0b_111)
				break;
		}

		if (hasPlaced)
		{
			bool addedToChest = false;

			foreach (Rectangle room in rooms)
			{
				for (int i = room.Left; i < room.Right; ++i)
				{
					for (int j = room.Top; j < room.Bottom; ++j)
					{
						Tile tile = Main.tile[i, j];

						if (!tile.HasTile)
							continue;

						int x = i;
						int y = j;

						TileExtensions.GetTopLeft(ref x, ref y);
						tile = Main.tile[x, y];

						if (tile.TileType == TileID.Signs)
						{
							int sign = Sign.ReadSign(x, y);

							Main.sign[sign].text = WorldGen.genRand.NextBool(25)
								? Language.GetTextValue("Mods.SpiritReforged.Generation.Signs.Underground.Rare." + Main.rand.Next(3))
								: Language.GetTextValue("Mods.SpiritReforged.Generation.Signs.Underground.Common." + Main.rand.Next(11));
						}
						else if (TileID.Sets.BasicChest[tile.TileType] && !addedToChest)
							addedToChest = AddToChest(x, y, skipFlags);
						else if (tile.TileType is TileID.Mannequin or TileID.Womannequin && !dolls.ContainsKey(new Point16(x, y)))
						{
							if (!TileEntity.ByPosition.TryGetValue(new Point16(x, y), out TileEntity te) || te is not TEDisplayDoll mannequin)
							{
								int id = TEDisplayDoll.Place(x, y);
								mannequin = TileEntity.ByID[id] as TEDisplayDoll;
							}

							dolls.Add(new(x, y), self.Type);
						}
					}
				}
			}
		}
	}

	private static bool AddToChest(int i, int j, AddedHouseFlags skipFlags)
	{
		if (!skipFlags.HasFlag(AddedHouseFlags.LoomHouse))
			return false;

		int chestIndex = Chest.FindChest(i, j);

		if (chestIndex != -1 && Main.chest[chestIndex] is not null)
		{
			Chest chest = Main.chest[chestIndex];

			for (int k = 0; k < chest.item.Length; ++k)
			{
				ref Item item = ref chest.item[k];

				if (item is not null && item.IsAir)
				{
					item = new Item(ItemID.Silk, WorldGen.genRand.Next(4, 9));
					return true;
				}	
			}
		}

		return false;
	}

	private static bool PlaceDecorInRoom(Rectangle room, int y, int type, int style = 0, int randomRepeats = 12)
	{
		bool hasPlaced = false;
		Point placedPos = Point.Zero;

		for (int i = 0; i < randomRepeats; i++)
		{
			placedPos = new Point(WorldGen.genRand.Next(2, room.Width - 2) + room.X, y - 1);

			if (hasPlaced = WorldGen.PlaceObject(placedPos.X, placedPos.Y, type, true, style))
				break;
		}

		if (hasPlaced && Main.tile[placedPos].TileType == type && Main.tile[placedPos].HasTile)
			return true;

		for (int j = room.X + 2; j <= room.X + room.Width - 2; j++)
		{
			placedPos = new Point(j, y);

			if (hasPlaced = WorldGen.PlaceObject(j, y, type, true, style))
				break;
		}

		return hasPlaced && Main.tile[placedPos].TileType == type && Main.tile[placedPos].HasTile;
	}

	public override void SaveWorldData(TagCompound tag)
	{
		if (dolls.Count > 0)
		{
			tag.Add("dolls", dolls.Keys.ToArray());
			tag.Add("biomes", dolls.Values.Select(x => (byte)x).ToArray());
		}
	}

	public override void LoadWorldData(TagCompound tag)
	{
		if (!tag.TryGet("dolls", out Point16[] positions) || !tag.TryGet("biomes", out byte[] biomes))
			return;

		dolls.Clear();

		for (int i = 0; i < positions.Length; i++)
		{
			Point16 position = positions[i];
			var biome = (HouseType)biomes[i];
			dolls.Add(position, biome);
		}
	}

	public override void PreUpdateTime()
	{
		if (dolls.Count == 0)
			return;

		foreach (var doll in dolls)
			MannequinInventory.InventoryByBiome[doll.Value].SetMannequin(doll.Key);

		dolls.Clear();
	}
}
