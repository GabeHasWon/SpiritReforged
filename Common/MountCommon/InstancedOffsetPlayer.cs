using Terraria.DataStructures;

namespace SpiritReforged.Common.MountCommon;

#nullable enable

/// <summary>
/// Allows for <see cref="Mount.MountData.playerYOffsets"/> to be "instanced", avoiding niche drawing issues in multiplayer for dynamic offsets.<br/>
/// Instead of setting the MountData value, call <see cref="SetInstancedOffsets(int[])"/>. This value is nulled out when the mount is inactive or switched.
/// </summary>
internal class InstancedOffsetPlayer : ModPlayer
{
	public int[]? InstancedOffsets { get; private set; }

	private int? lastInstancedType = null;

	public override void Load()
	{
		On_Mount.Draw += InstanceOffset;
		On_Player.RotatedRelativePoint += InstanceOffset;
	}

	public void SetInstancedOffsets(int[] offsets)
	{
		lastInstancedType = Player.mount.Type;
		InstancedOffsets = offsets;
	}

	public override void PreUpdate()
	{
		if (InstancedOffsets is null && lastInstancedType is null)
			return;

		// Force-reset these values if the mount is invalid, inactive or switched
		if (Player.mount is null || Player.mount._data is null || !Player.mount.Active || Player.mount.Type != lastInstancedType)
		{
			InstancedOffsets = null;
			lastInstancedType = null;
		}
	}

	private Vector2 InstanceOffset(On_Player.orig_RotatedRelativePoint orig, Player self, Vector2 pos, bool reverseRotation, bool addGfxOffY) 
		=> WrapCall(() => orig(self, pos, reverseRotation, addGfxOffY), self.mount, self);

	private void InstanceOffset(On_Mount.orig_Draw orig, Mount self, List<DrawData> drawData, int drawType, Player drawPlayer, Vector2 Position, Color c, SpriteEffects e, float shadow)
		=> WrapCall(() => orig(self, drawData, drawType, drawPlayer, Position, c, e, shadow), self, drawPlayer);

	private static void WrapCall(Action call, Mount self, Player player)
	{
		if (IsMountInvalid(self, player))
		{
			call();
			return;
		}

		int[] oldOffsets = self._data.playerYOffsets;
		self._data.playerYOffsets = player.GetModPlayer<InstancedOffsetPlayer>().InstancedOffsets;

		call();

		self._data.playerYOffsets = oldOffsets;
	}

	private static bool IsMountInvalid(Mount self, Player player) => self is null || self._data is null || 
		player.GetModPlayer<InstancedOffsetPlayer>().InstancedOffsets is not int[] offsets || offsets.Length != self._data.playerYOffsets.Length;

	private static T WrapCall<T>(Func<T> call, Mount self, Player player)
	{
		if (IsMountInvalid(self, player))
			return call();

		int[] oldOffsets = self._data.playerYOffsets;
		self._data.playerYOffsets = player.GetModPlayer<InstancedOffsetPlayer>().InstancedOffsets;

		T result = call();

		self._data.playerYOffsets = oldOffsets;
		return result;
	}
}
