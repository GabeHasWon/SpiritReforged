using SpiritReforged.Common.BuffCommon;
using SpiritReforged.Common.ItemCommon;

namespace SpiritReforged.Common.PlayerCommon;

internal static class PlayerExtensions
{
	public static bool HasAccessory(this Player player, Item item) => item.ModItem is AccessoryItem acc && player.GetModPlayer<MiscAccessoryPlayer>().accessory[acc.AccName];
	public static bool HasAccessory(this Player player, ModItem item) => item is AccessoryItem acc && player.GetModPlayer<MiscAccessoryPlayer>().accessory[acc.AccName];
	public static bool HasAccessory<TItem>(this Player player) where TItem : AccessoryItem => player.GetModPlayer<MiscAccessoryPlayer>().accessory[ModContent.GetInstance<TItem>().AccName];
	public static bool HasAccessory(this Player player, int itemId) => HasAccessory(player, ContentSamples.ItemsByType[itemId]);

	/// <summary> Checks whether the player is in the corruption, crimson, or hallow. </summary>
	public static bool ZoneEvil(this Player player) => player.ZoneCorrupt || player.ZoneCrimson || player.ZoneHallow;
	/// <inheritdoc cref="CollisionPlayer.FallThrough"/>
	public static bool FallThrough(this Player player) => player.GetModPlayer<CollisionPlayer>().FallThrough();
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
}