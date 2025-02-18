﻿using System.Linq;

namespace SpiritReforged.Common.WorldGeneration.Chests;

public static class ChestPoolUtils
{
	/// <summary> Struct containing information related to chest item pools. </summary>
	public struct ChestInfo
	{
		/// <param name="item"> The item type to add to the chest pool. </param>
		/// <param name="stack"> The item stack. </param>
		/// <param name="chance"> The chance for the item to generate. </param>
		public ChestInfo(int item, int stack = 1, float chance = 1)
		{
			items = item;
			this.stack = stack;
			this.chance = chance;
		}

		/// <param name="items"> The item types to add to the chest pool. Only one item type will be selected when using int[]. </param>
		/// <param name="stack"> The item stack. </param>
		/// <param name="chance"> The chance for the item to generate. </param>
		public ChestInfo(int[] items, int stack = 1, float chance = 1)
		{
			this.items = items;
			this.stack = stack;
			this.chance = chance;
		}

		public object items;
		public int stack;
		public float chance;

		public readonly List<ChestInfo> ToList() => [this];
	}

	// Helper method for adding items to chests
	private static void AddItemsToChest(IEnumerable<ChestInfo> list, Chest chest, int itemIndex)
	{
		foreach (ChestInfo chestInfo in list)
		{
			switch (chestInfo.items)
			{
				case int[] itemPool:
					chest.item[itemIndex].SetDefaults(itemPool[Main.rand.Next(itemPool.Length)]);
					chest.item[itemIndex].stack = chestInfo.stack;
					break;
				case int intItem:
					chest.item[itemIndex].SetDefaults((int)chestInfo.items);
					chest.item[itemIndex].stack = chestInfo.stack;
					break;
			}

			itemIndex++;
		}
	}

	/// <summary>
	/// Method to greatly reduce the amount of effort needed to make a chest pool. <br />
	/// Input the chest's pool as a list of structs representing the item pool for each slot, stack for that pool, and chance to be added.
	/// </summary>
	public static void PlaceChestItems(List<ChestInfo> list, Chest chest, int startIndex = 0)
	{
		int itemIndex = startIndex;

		var newList = new List<ChestInfo>();

		foreach (ChestInfo c in list)
		{ //prune the list based on the chances of items being added and stacks
			if (Main.rand.NextFloat() >= c.chance || c.stack == 0)
				continue; //skip

			newList.Add(c);
		}

		if (chest.item[itemIndex].active && newList.Count > 0)
		{ //check if the spot items are being added to is active
			int itemsToMove = itemIndex + chest.item.Skip(itemIndex).Where(x => x.active).Count();
			for (int i = itemsToMove; i >= itemIndex; i--)
				chest.item[i + newList.Count] = chest.item[i].Clone();
		}

		AddItemsToChest(newList, chest, itemIndex);
	}

	public static void PlaceModChestItemsWCheck(List<ChestInfo> list, Chest chest, ref bool[] placedItems)
	{
		int itemIndex = 0;

		int[] importantItemPool = (int[])list.ElementAt(0).items;
		int itemToPlace = 0;
		bool canPlace = false;
		while (!canPlace)
		{ //check if the chosen item has been placed before, and if all items havent already been placed
			itemToPlace = WorldGen.genRand.Next(importantItemPool.Length);
			if (!placedItems[itemToPlace] || placedItems.All(x => x == true))
			{
				placedItems[itemToPlace] = true;
				canPlace = true;
				break;
			}
		}

		if (canPlace)
		{
			placedItems[itemToPlace] = true;
			chest.item[itemIndex].SetDefaults(importantItemPool[itemToPlace]);
			itemIndex++;
		}

		AddItemsToChest(list.Skip(1), chest, itemIndex);

	}

	public static void AddToModdedChest(List<ChestInfo> list, int chestType)
	{
		for (int chestIndex = 0; chestIndex < Main.chest.Length; chestIndex++)
		{
			Chest chest = Main.chest[chestIndex];
			if (chest != null && Main.tile[chest.x, chest.y].TileType == chestType)
				PlaceChestItems(list, chest);
		}
	}

	public static void AddToModdedChestWithOverlapCheck(List<ChestInfo> list, int chestType)
	{
		int[] items = (int[])list.ElementAt(0).items;
		bool[] placedItems = new bool[items.Length];

		for (int chestIndex = 0; chestIndex < Main.chest.Length; chestIndex++)
		{
			Chest chest = Main.chest[chestIndex];
			if (chest != null && Main.tile[chest.x, chest.y].TileType == chestType)
				PlaceModChestItemsWCheck(list, chest, ref placedItems);
		}
	}

	/// <inheritdoc cref="AddToVanillaChest(List{ChestInfo}, int, int, ushort)"/>
	/// <param name="item"> The <see cref="ChestInfo"/> to add. </param>
	public static void AddToVanillaChest(ChestInfo item, int chestFrame, int index, ushort tileType = TileID.Containers) => AddToVanillaChest(item.ToList(), chestFrame, index, tileType);

	/// <summary> Adds the given item info to chest inventories. </summary>
	/// <param name="items"> The <see cref="ChestInfo"/> to add. </param>
	/// <param name="chestFrame"> The horizontal frame of <paramref name="tileType"/> to consider. See <see cref="VanillaChestID"/> and <see cref="VanillaChestID2"/>. </param>
	/// <param name="index"> The chest inventory index. </param>
	/// <param name="tileType"> The chest tile type. </param>
	public static void AddToVanillaChest(List<ChestInfo> items, int chestFrame, int index = 0, ushort tileType = TileID.Containers)
	{
		chestFrame *= 36;
		for (int chestIndex = 0; chestIndex < Main.chest.Length; chestIndex++)
		{
			Chest chest = Main.chest[chestIndex];
			if (chest != null && Main.tile[chest.x, chest.y].TileType == tileType && Main.tile[chest.x, chest.y].TileFrameX == chestFrame)
				PlaceChestItems(items, chest, index);
		}
	}
}
