using SpiritReforged.Common.BuffCommon;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.ItemCommon.Backpacks;
using SpiritReforged.Common.Misc;

namespace SpiritReforged.Common.PlayerCommon;

internal static class PlayerExtensions
{
	public static bool HasInfoItem(this Player player, string itemName) => player.GetModPlayer<InfoItem.InfoPlayer>().info[itemName];

	public static bool HasInfoItem<TItem>(this Player player) where TItem : InfoItem => player.HasInfoItem(ModContent.GetInstance<TItem>().Name);

	/// <summary> Checks whether the set bonus related to this item is active on <paramref name="player"/>.<br/>
	/// <paramref name="ofType"/> must be of the instance that overrides <see cref="ModItem.IsArmorSet"/>. </summary>
	public static bool WearingSet(this Player player, int ofType) => ItemLoader.GetItem(ofType).IsArmorSet(player.armor[0], player.armor[1], player.armor[2]);

	/// <inheritdoc cref="WearingSet(Player, int)"/>
	public static bool WearingSet<T>(this Player player) where T : ModItem => ModContent.GetInstance<T>().IsArmorSet(player.armor[0], player.armor[1], player.armor[2]);

	public static void SetFlag(this Player player, string name, bool? value = true) => player.GetModPlayer<PlayerFlags>().SetFlag(name, value);

	public static bool? CheckFlag(this Player player, string name) => player.GetModPlayer<PlayerFlags>().CheckFlag(name);

	/// <summary> Checks Whether <paramref name="player"/> has a true flag of name <typeparamref name="T"/>. Often used with <see cref="IFlagged"/>. </summary>
	public static bool HasFlag<T>(this Player player) where T : ModType => player.GetModPlayer<PlayerFlags>().CheckFlag(ModContent.GetInstance<T>().Name) == true;

	/// <summary> Checks whether the player is in the corruption, crimson, or hallow. </summary>
	public static bool ZoneEvil(this Player player) => player.ZoneCorrupt || player.ZoneCrimson || player.ZoneHallow;

	/// <inheritdoc cref="CollisionPlayer.FallThrough"/>
	public static bool FallThrough(this Player player) => player.GetModPlayer<CollisionPlayer>().FallThrough();

	/// <summary> Whether <paramref name="player"/> has used quick buff. </summary>
	public static bool UsedQuickBuff(this Player player) => player.GetModPlayer<BuffPlayer>().usedQuickBuff;

	/// <summary> Safely rotates the whole player. Must be continuously set. </summary>
	public static void Rotate(this Player player, float rotation, Vector2? origin = null)
	{
		player.GetModPlayer<CollisionPlayer>().rotation = rotation;

		player.fullRotation = rotation;
		player.fullRotationOrigin = origin ?? player.fullRotationOrigin;
	}

	/// <summary> Gets <see cref="Player.GetFrontHandPosition"/> rotated by <see cref="Player.RotatedRelativePoint"/>. </summary>
	public static Vector2 GetHandRotated(this Player player, Player.CompositeArmStretchAmount stretch, float rotation) => player.RotatedRelativePoint(player.GetFrontHandPosition(stretch, rotation));

	/// <summary> Gets rotation from <see cref="GetHandRotated(Player, Player.CompositeArmStretchAmount, float)"/> automatically using <paramref name="player"/>'s front composite arm data. </summary>
	public static Vector2 GetHandRotated(this Player player)
	{
		var stretch = player.compositeFrontArm.stretch;
		float rotation = player.compositeFrontArm.rotation;

		return player.GetHandRotated(stretch, rotation);
	}

	public static Tile TargetTile(this Player player) => Framing.GetTileSafely(Player.tileTargetX, Player.tileTargetY);

	public static void SimpleShakeScreen(this Player player, float strength, float vibrationCycles, int frames, float distanceFalloff, string uniqueIdentity = null)
	{
		var direction = (Main.rand.NextFloat() * ((float)Math.PI * 2f)).ToRotationVector2();
		ScreenshakeHelper.Shake(player.Center, direction, strength, vibrationCycles, frames, distanceFalloff, uniqueIdentity);
	}

	#region find item
	[Flags]
	public enum FindItemContext
	{
		Inventory = 0,
		VoidBag = 1,
		Backpack = 2
	}

	public static FindItemContext FindAll = FindItemContext.Inventory | FindItemContext.VoidBag | FindItemContext.Backpack;

	public readonly record struct FoundItems(params Item[] Items)
	{
		public readonly int Count
		{
			get
			{
				int value = 0;

				foreach (Item item in Items)
					value += item.stack;

				return value;
			}
		}

		public readonly bool Consume()
		{
			foreach (Item item in Items)
			{
				if (!item.IsAir && --item.stack <= 0)
				{
					item.TurnToAir();
					return true;
				}
			}

			return false;
		}
	}

	public static bool FindItems(this Player player, int type, FindItemContext context, out FoundItems foundItems)
	{
		List<Item> result = [];
		if (context.HasFlag(FindItemContext.Inventory))
		{
			foreach (Item item in player.inventory)
			{
				if (item.type == type)
					result.Add(item);
			}
		}

		if (context.HasFlag(FindItemContext.VoidBag))
		{
			foreach (Item item in player.bank4.item)
			{
				if (item.type == type)
					result.Add(item);
			}
		}

		if (context.HasFlag(FindItemContext.Backpack) && player.TryGetModPlayer(out BackpackPlayer backpackPlayer) && backpackPlayer.backpack.ModItem is BackpackItem backpack)
		{
			foreach (Item item in backpack.Items)
			{
				if (item.type == type)
					result.Add(item);
			}
		}

		foundItems = new(result.ToArray());
		return result.Count > 0;
	}
	#endregion
}