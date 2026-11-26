using Terraria.DataStructures;

namespace SpiritReforged.Common.TileCommon;

public interface ICheckItemUse
{
	/// <summary> Allows you to make things happen when a player targets this tile/wall and uses an item, like the Staff of Regrowth growing grass on dirt.<br/>
	/// Behaviours are registered as delegates. See <see cref="CheckItem.RegisterTileCheck"/> to manually add one. </summary>
	/// <param name="type"> The type of item used. </param>
	/// <param name="player">The player using the item.</param>
	/// <param name="i"> The selected tile's X position. </param>
	/// <param name="j"> The selected tile's Y position. </param>
	/// <returns> Whether the item of 'type' did something when used. Return null for vanilla effects. </returns>
	public bool? CheckItemUse(int type, Player player, int i, int j);
}

/// <summary>
/// Functionality for <see cref="ICheckItemUse"/> that allows for items to do things when used on a tile or wall, such as with Trellises.
/// </summary>
public sealed class CheckItem : GlobalItem
{
	public delegate bool? ItemUseDelegate(int itemType, Player player, int i, int j);

	private static readonly Dictionary<int, ItemUseDelegate> TileToAction = [];
	private static readonly Dictionary<int, ItemUseDelegate> WallToAction = [];
	private static readonly List<Mod> AddedMods = [];

	internal static void RegisterTileCheck(int tileType, ItemUseDelegate dele) => TileToAction.Add(tileType, dele);
	internal static void RegisterWallCheck(int wallType, ItemUseDelegate dele) => WallToAction.Add(wallType, dele);

	/// <summary>
	/// Registers a mod to be checked for <see cref="ICheckItemUse"/> logic.
	/// </summary>
	/// <param name="mod"></param>
	public static void RegisterMod(Mod mod) => AddedMods.Add(mod);

	public override void SetStaticDefaults()
	{
		AddedMods.Add(Mod);

		foreach (Mod mod in AddedMods)
		{
			foreach (var tile in Mod.GetContent<ModTile>())
			{
				if (tile is ICheckItemUse check)
					RegisterTileCheck(tile.Type, check.CheckItemUse);
			}

			foreach (var wall in Mod.GetContent<ModWall>())
			{
				if (wall is ICheckItemUse check)
					RegisterWallCheck(wall.Type, check.CheckItemUse);
			}
		}
	}

	public override bool? UseItem(Item item, Player player)
	{
		var target = new Point(Player.tileTargetX, Player.tileTargetY);
		var tile = Main.tile[target.X, target.Y];

		if (tile.HasTile) //Check tiles
		{
			if (player.InInteractionRange(target.X, target.Y, TileReachCheckSettings.Simple) && TileToAction.TryGetValue(tile.TileType, out var check))
				return check.Invoke(item.type, player, target.X, target.Y);
		}
		else if (tile.WallType != WallID.None) //Check walls
		{
			if (player.InInteractionRange(target.X, target.Y, TileReachCheckSettings.Simple) && WallToAction.TryGetValue(tile.WallType, out var check))
				return check.Invoke(item.type, player, target.X, target.Y);
		}

		return null;
	}
}