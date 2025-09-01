using SpiritReforged.Common.Misc;
using Terraria.DataStructures;

namespace SpiritReforged.Common.TileCommon;

/// <summary> Provides additional <see cref="LootTable"/> context specifically for tiles. </summary>
public class TileLootTable(int style, Point16 coordinates = default) : LootTable
{
	public bool Simulated => Coordinates == default;

	public int Style = style;
	public Point16 Coordinates = coordinates;
}

public class TileLootHandler : ILoadable
{
	/// <summary> Stores delegate loot methods by tile type. </summary>
	private static readonly Dictionary<int, LootTable.LootDelegate> ActionByType = [];

	public static bool TryGetLootPool(int tileType, out LootTable.LootDelegate pool) => ActionByType.TryGetValue(tileType, out pool);
	public static void RegisterLoot(LootTable.LootDelegate action, params int[] types)
	{
		foreach (int type in types)
		{
			if (!ActionByType.TryAdd(type, action))
				ActionByType[type] += action;
		}
	}

	/// <summary> Calls <see cref="LootTable.Resolve(Rectangle, Player)"/> using tile data from the given coordinates. </summary>
	public static bool Resolve(int i, int j, ushort type, int frameX, int frameY)
	{
		if (TryGetLootPool(type, out LootTable.LootDelegate action))
		{
			Tile t = new(); //if this method is called in KillMultiTile the tile at (i, j) is unusable
			t.TileFrameX = (short)frameX;
			t.TileFrameY = (short)frameY;
			t.TileType = type;
			t.HasTile = true;

			var data = TileObjectData.GetTileData(t); //data can be null here
			Point size = new(data?.Width ?? 2, data?.Height ?? 2);

			var loot = new TileLootTable(TileObjectData.GetTileStyle(t), new(i, j));
			action.Invoke(loot);

			var spawn = new Vector2(i, j).ToWorldCoordinates(size.X * 8, size.Y * 8);
			var p = Main.player[Player.FindClosest(spawn, 0, 0)];

			loot.Resolve(new Rectangle(i * 16, j * 16, size.X * 16, size.Y * 16), p);
			t.HasTile = false; //Deactivate the tile just to be safe

			return true;
		}

		return false;
	}

	/// <summary> Automatically registers loot delegates by <see cref="ILootTile"/> types. </summary>
	public void Load(Mod mod) => SpiritReforgedMod.OnLoad += static () =>
	{
		foreach (var t in SpiritReforgedMod.Instance.GetContent<ModTile>())
		{
			if (t is ILootable i)
				RegisterLoot(i.AddLoot, t.Type);
		}
	};

	public void Unload() { }
}