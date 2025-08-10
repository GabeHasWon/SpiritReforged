using Terraria.DataStructures;

namespace SpiritReforged.Common.TileCommon;

internal interface ICheckItemUse
{
	/// <summary> Allows you to make things happen when a player targets this tile and uses an item, like the Staff of Regrowth growing grass on dirt.<br/>
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

	public static void RegisterTileCheck(int tileType, ItemUseDelegate dele) => TileToAction.Add(tileType, dele);

	public override void SetStaticDefaults()
	{
		foreach (var tile in Mod.GetContent<ModTile>())
			if (tile is ICheckItemUse check)
				RegisterTileCheck(tile.Type, check.CheckItemUse);
	}

	public override bool? UseItem(Item item, Player player)
	{
		var target = new Point(Player.tileTargetX, Player.tileTargetY);
		var tile = Main.tile[target.X, target.Y];

		if (tile.HasTile && player.InInteractionRange(target.X, target.Y, TileReachCheckSettings.Simple) && TileToAction.TryGetValue(tile.TileType, out var check))
			return check.Invoke(item.type, target.X, target.Y);

		return null;
	}
}