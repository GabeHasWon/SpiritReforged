using SpiritReforged.Common.Easing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpiritReforged.Common.ItemCommon;
public static class ItemVisualHelpers
{
	/// <summary>
	/// Helper function for a quick gun recoil animation. Controls the items position
	/// </summary>
	/// <param name="player">The player using the item</param>
	/// <param name="item">The item being used</param>
	/// <param name="shootDirection">The direction of the shot</param>
	/// <param name="recoil">How many pixels the gun will recoil back</param>
	/// <param name="firstSection">Easing function for the first section of the animation</param>
	/// <param name="secondSection">Easing function for the second section of the animation</param>
	/// <param name="itemSize">Size for drawing the sprite</param>
	/// <param name="itemOrigin">Origin for drawing the sprite</param>
	/// <param name="animationRatio">Ratio of the animation. Defaults to 5% startup, 95% back down</param>
	/// <returns></returns>
	public static Vector2 SetGunUseStyle(Player player, Item item, int shootDirection, float recoil, EaseFunction firstSection, EaseFunction secondSection, Vector2 itemSize, Vector2 itemOrigin, Vector2? animationRatio = null)
	{
		if (animationRatio == null)
			animationRatio = new Vector2(0.05f, 0.95f);

		if (item.noUseGraphic) // the item draws wrong for the first frame it is drawn when you switch directions for some odd reason, this plus setting it to true in shoot makes it not draw for the first frame.
			item.noUseGraphic = false;

		float animProgress = 1f - player.itemTime / (float)player.itemTimeMax;

		if (Main.myPlayer == player.whoAmI)
			player.direction = shootDirection;

		float itemRotation = player.compositeFrontArm.rotation + 1.5707964f * player.gravDir;
		Vector2 itemPosition = player.MountedCenter;

		if (animProgress < animationRatio.Value.X)
		{
			float lerper = animProgress / animationRatio.Value.X;
			itemPosition += itemRotation.ToRotationVector2() * MathHelper.Lerp(0f, recoil, firstSection.Ease(lerper));
		}
		else
		{
			float lerper = (animProgress - animationRatio.Value.X) / animationRatio.Value.Y;
			itemPosition += itemRotation.ToRotationVector2() * MathHelper.Lerp(recoil, 0f, secondSection.Ease(lerper));
		}

		CleanHoldStyle(player, itemRotation, itemPosition, itemSize, new Vector2?(itemOrigin), true, false, true);

		return itemPosition;
	}

	/// <summary>
	/// Helper function for a quick gun recoil animation. Controls the items position
	/// </summary>
	/// <param name="player">The player using the item</param>
	/// <param name="shootDirection">The direction of the shot</param>
	/// <param name="shootRotation">The rotation of the shot</param>
	/// <param name="recoil">How many pixels the gun will recoil back</param>
	/// <param name="firstSection">Easing function for the first section of the animation</param>
	/// <param name="secondSection">Easing function for the second section of the animation</param>
	/// <param name="animationRatio">Ratio of the animation. Defaults to 5% startup, 95% back down</param>
	/// <returns></returns>
	public static void SetGunUseItemFrame(Player player, int shootDirection, float shootRotation, float recoil, EaseFunction firstSection, EaseFunction secondSection, bool setBackArm = false, Vector2? animationRatio = null)
	{
		if (animationRatio == null)
			animationRatio = new Vector2(0.05f, 0.95f);

		if (Main.myPlayer == player.whoAmI)
			player.direction = shootDirection;

		float animProgress = 1f - player.itemTime / (float)player.itemTimeMax;
		float rotation = shootRotation * player.gravDir + 1.5707964f;

		if (animProgress < animationRatio.Value.X)
		{
			float lerper = animProgress / animationRatio.Value.X;
			rotation += MathHelper.Lerp(0f, recoil, firstSection.Ease(lerper)) * player.direction;
		}
		else
		{
			float lerper = (animProgress - animationRatio.Value.X) / animationRatio.Value.Y;
			rotation += MathHelper.Lerp(recoil, 0, secondSection.Ease(lerper)) * player.direction;
		}

		Player.CompositeArmStretchAmount stretch = Player.CompositeArmStretchAmount.Full;
		if (animProgress < 0.5f)
			stretch = Player.CompositeArmStretchAmount.None;
		else if (animProgress < 0.75f)
			stretch = Player.CompositeArmStretchAmount.ThreeQuarters;

		player.SetCompositeArmFront(true, stretch, rotation);
		if (setBackArm)
			player.SetCompositeArmBack(true, stretch, rotation + MathHelper.ToRadians(25f) * player.direction);
	}

	public static void CleanHoldStyle(Player player, float desiredRotation, Vector2 desiredPosition, Vector2 spriteSize, Vector2? rotationOriginFromCenter = null, bool noSandstorm = false, bool flipAngle = false, bool stepDisplace = true)
	{
		if (noSandstorm)
			player.sandStorm = false;

		if (rotationOriginFromCenter == null)
			rotationOriginFromCenter = new Vector2?(Vector2.Zero);

		Vector2 origin = rotationOriginFromCenter.Value;
		origin.X *= player.direction;
		origin.Y *= player.gravDir;
		player.itemRotation = desiredRotation;

		if (flipAngle)
			player.itemRotation *= player.direction;
		else if (player.direction < 0)
			player.itemRotation += 3.1415927f;

		Vector2 consistentAnchor = player.itemRotation.ToRotationVector2() * (spriteSize.X / -2f - 10f) * player.direction - origin.RotatedBy(player.itemRotation, default);
		Vector2 offsetAgain = spriteSize * -0.5f;
		Vector2 finalPosition = desiredPosition + offsetAgain + consistentAnchor;
		if (stepDisplace)
		{
			int frame = player.bodyFrame.Y / player.bodyFrame.Height;
			if (frame > 6 && frame < 10 || frame > 13 && frame < 17)
				finalPosition -= Vector2.UnitY * 2f;
		}

		player.itemLocation = finalPosition + new Vector2(spriteSize.X * 0.5f, 0f);
	}
}
