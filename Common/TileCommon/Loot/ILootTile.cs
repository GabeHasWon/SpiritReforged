using SpiritReforged.Common.Misc;
using Terraria.DataStructures;
using static SpiritReforged.Common.TileCommon.Loot.ILootTile;

namespace SpiritReforged.Common.TileCommon.Loot;

/// <summary> Facilitates a tile with an <see cref="ILoot"/> drop table.<br/>
/// Does not drop items automatically. See <see cref="LootTable.Resolve"/>. </summary>
public interface ILootTile
{
	public delegate void LootDelegate(Context context, ILoot loot);
	public readonly record struct Context(int Style, Point16 Coordinates = default)
	{
		public readonly bool Simulated => Coordinates == default;
	}

	public void AddLoot(Context context, ILoot loot);
}

public class TileLootHandler : ILoadable
{
	/// <summary> Stores delegate loot methods by tile type. </summary>
	private static readonly Dictionary<int, LootDelegate> ActionByType = [];

	public static void InvokeLootPool(int tileType, Context context, ILoot loot)
	{
		if (TryGetLootPool(tileType, out LootDelegate action))
			action.Invoke(context, loot);
	}

	public static bool TryGetLootPool(int tileType, out LootDelegate pool) => ActionByType.TryGetValue(tileType, out pool);
	public static void RegisterLoot(LootDelegate action, params int[] types)
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
		if (TryGetLootPool(type, out LootDelegate action))
		{
			Tile t = new(); //if this method is called in KillMultiTile the tile at (i, j) is unusable
			t.TileFrameX = (short)frameX;
			t.TileFrameY = (short)frameY;
			t.TileType = type;
			t.HasTile = true;

			var data = TileObjectData.GetTileData(t); //data can be null here
			Point size = new(data?.Width ?? 2, data?.Height ?? 2);

			var loot = new LootTable();
			action.Invoke(new(TileObjectData.GetTileStyle(t), new(i, j)), loot);

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
			if (t is ILootTile i)
				RegisterLoot(i.AddLoot, t.Type);
		}
	};

	public void Unload() { }
}