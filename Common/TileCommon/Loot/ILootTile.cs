using SpiritReforged.Common.Misc;
using SpiritReforged.Content.Underground.Tiles;
using Terraria.DataStructures;

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

	#region database
	/// <summary> Stores delegate loot methods by tile type. </summary>
	private static readonly Dictionary<int, LootDelegate> ActionByType = [];

	public static LootDelegate GetLootPool(int tileType)
	{
		if (ActionByType.TryGetValue(tileType, out var pool))
			return pool;

		if (TileLoader.GetTile(tileType) is ILootTile t) //If the delegate wasn't registered yet due to load order, register it now. This is most likely an issue when using calls
		{
			LootDelegate del = t.AddLoot;

			ActionByType.Add(tileType, del);
			return del;
		}

		return null;
	}

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
		if (GetLootPool(type) is LootDelegate action)
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
			return true;
		}

		return false;
	}
	#endregion

	#region call
	public static bool ManualModifyLoot(object[] args)
	{
		if (args.Length < 2)
			throw new ArgumentException("ModifyPotLoot requires 2 parameters.");

		if (args[0] is not int type)
			throw new ArgumentException("ModifyPotLoot parameter 1 should be an int.");

		if (!ParseLootAction(type, args[1]))
			throw new ArgumentException("ModifyPotLoot parameter 2 should be a bool, Action<int, ILoot>, or Action<int, Point16, ILoot>.");

		return true;
	}

	public static bool ParseLootAction(int type, object arg)
	{
		if (arg is bool hasBasicLoot && hasBasicLoot)
		{
			RegisterLoot(GetLootPool(ModContent.TileType<Pots>()), type);
			return true;
		}
		else if (arg is Action<int, ILoot> dele)
		{
			RegisterLoot((context, loot) => dele.Invoke(context.Style, loot), type); //Nest delegates to avoid using a .dll reference because of ILootTile.Context
			return true;
		}
		else if (arg is Action<int, Point16, ILoot> dele2)
		{
			RegisterLoot((context, loot) => dele2.Invoke(context.Style, context.Coordinates, loot), type);
			return true;
		}

		return false;
	}
	#endregion
}