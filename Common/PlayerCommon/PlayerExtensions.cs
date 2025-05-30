using SpiritReforged.Common.BuffCommon;
using SpiritReforged.Common.ItemCommon;
using Terraria.Graphics.CameraModifiers;

namespace SpiritReforged.Common.PlayerCommon;

internal static class PlayerExtensions
{
	public static bool HasEquip(this Player player, Item item) => item.ModItem is EquippableItem && player.GetModPlayer<ItemEquipPlayer>().equips[item.Name];
	public static bool HasEquip(this Player player, ModItem item) => item is EquippableItem && player.GetModPlayer<ItemEquipPlayer>().equips[item.Name];
	public static bool HasEquip<TItem>(this Player player) where TItem : EquippableItem => player.GetModPlayer<ItemEquipPlayer>().equips[ModContent.GetInstance<TItem>().Name];
	public static bool HasEquip(this Player player, int itemId) => HasEquip(player, ContentSamples.ItemsByType[itemId]);

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

	public static void SimpleShakeScreen(this Player player, float strength, float vibrationCycles, int frames, float distanceFalloff, string uniqueIdentity = null)
	{
		var direction = (Main.rand.NextFloat() * ((float)Math.PI * 2f)).ToRotationVector2();
		PunchCameraModifier modifier = new(player.Center, direction, strength, vibrationCycles, frames, distanceFalloff, uniqueIdentity);
		Main.instance.CameraModifiers.Add(modifier);
	}
}