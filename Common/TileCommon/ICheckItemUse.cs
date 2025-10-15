using Terraria.DataStructures;

namespace SpiritReforged.Common.TileCommon;

internal interface ICheckItemUse
{
	/// <summary> Allows you to make things happen when a player targets this tile/wall and uses an item, like the Staff of Regrowth growing grass on dirt.<br/>
	/// Behaviours are registered as delegates. See <see cref="CheckItem.RegisterTileCheck"/> to manually add one. </summary>
	/// <param name="type"> The type of item used. </param>
	/// <param name="i"> The selected tile's X position. </param>
	/// <param name="j"> The selected tile's Y position. </param>
	/// <returns> Whether the item of 'type' did something when used. Return null for vanilla effects. </returns>
	public bool? CheckItemUse(int type, int i, int j);
}

internal sealed class CheckItem : GlobalItem
{
	public delegate bool? ItemUseDelegate(int itemType, int i, int j);
	private static readonly Dictionary<int, ItemUseDelegate> TileToAction = [];
	private static readonly Dictionary<int, ItemUseDelegate> WallToAction = [];

	public static void RegisterTileCheck(int tileType, ItemUseDelegate dele) => TileToAction.Add(tileType, dele);
	public static void RegisterWallCheck(int wallType, ItemUseDelegate dele) => WallToAction.Add(wallType, dele);

	public override void SetStaticDefaults()
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

	public override bool? UseItem(Item item, Player player)
	{
		var target = new Point(Player.tileTargetX, Player.tileTargetY);
		var tile = Main.tile[target.X, target.Y];

		if (tile.HasTile) //Check tiles
		{
			if (player.InInteractionRange(target.X, target.Y, TileReachCheckSettings.Simple) && TileToAction.TryGetValue(tile.TileType, out var check))
				return check.Invoke(item.type, target.X, target.Y);
		}
		else if (tile.WallType != WallID.None) //Check walls
		{
			if (player.InInteractionRange(target.X, target.Y, TileReachCheckSettings.Simple) && WallToAction.TryGetValue(tile.WallType, out var check))
				return check.Invoke(item.type, target.X, target.Y);
		}

		return null;
	}
}