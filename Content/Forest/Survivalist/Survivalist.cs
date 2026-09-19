using SpiritReforged.Common.ItemCommon.MagazineSystem;
using SpiritReforged.Common.Subclasses.Shotguns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SpiritReforged.Common.ItemCommon.MagazineSystem.MagazineGlobalItem;
using Terraria.Audio;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underworld.Heirloom;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Survivalist;
public class Survivalist() : ShotgunItem(new(1, speedMultiplier: 0.33f))
{
	public const int MAX_COOLDOWN = 300;

	int cooldown;

	public override bool AltFunctionUse(Player player) => cooldown <= 0 && !MagazinePlayer.GetMagazineWeapon(player).Reloading;

	public override void SafeSetDefaults()
	{
		Item.damage = 19;
		Item.knockBack = 10f;
		Item.width = 66;
		Item.height = 24;
		Item.useTime = Item.useAnimation = 71;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.noMelee = true;
		Item.noUseGraphic = false;
		Item.value = Item.buyPrice(0, 3, 50, 0);
		Item.rare = ItemRarityID.Orange;
		Item.autoReuse = true;

		var globalItem = Item.GetGlobalItem<MagazineGlobalItem>();

		globalItem.ActivateMagazine(Item, (pitch, position) => SoundEngine.PlaySound(SoundID.Item36 with { Pitch = pitch }, position), new(-0.1f, 0.5f, 5, 180), new(66, 24), new(-18, -2), MagazineReloadType.OneAtATime, MagazineUIType.Shell, true, false, -16, -0.3f);
		globalItem.SetAnimations(new(0.02f, 0.98f), reloadStyle: ReloadUseStyle, reloadFrame: ReloadUseFrame);
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (player.altFunctionUse == 2)
		{
			Projectile.NewProjectile(source, position, velocity * 13f, ModContent.ProjectileType<SurvivalistGrenadeProjectile>(), damage / 4, knockback * 2, player.whoAmI);

			return false;
		}

		return base.Shoot(player, source, position, velocity, type, damage, knockback);
	}

	public override bool ModifyItemDraw(ref PlayerDrawSet drawInfo, ref DrawData drawData, ref DrawData? coloredDrawData, ref DrawData? glowMaskDrawData)
	{
		if (Item.TryGetGlobalItem<MagazineGlobalItem>(out var magazineWeapon) && magazineWeapon.Reloading)
		{
			float animationProgress = magazineWeapon.ReloadProgress(drawInfo.drawPlayer, Item);

			var handle = ModContent.Request<Texture2D>(Texture + "_Handle").Value;
			var barrel = ModContent.Request<Texture2D>(Texture + "_Barrel").Value;

			SpriteEffects flip = drawInfo.drawPlayer.direction == -1 ? SpriteEffects.FlipHorizontally : 0;

			var handleData = new DrawData(handle, drawData.position, drawData.sourceRect, drawData.color, drawData.rotation, drawData.origin, drawData.scale, flip, 0f);

			float maxRotation = 0.6f * drawInfo.drawPlayer.direction;

			float barrelRot = drawData.rotation;

			if (animationProgress > 0.05f)
			{
				float lerper = (animationProgress - 0.05f) / 0.95f;

				if (lerper is > 0.1f)
				{
					if (lerper < 0.2f)
					{
						float interpolant = (lerper - 0.1f) / 0.1f;
						barrelRot += MathHelper.Lerp(0, maxRotation, EaseBuilder.EaseInBack(4).Ease(interpolant));
					}
					else if (lerper < 0.8f)
						barrelRot += maxRotation;
					else if (lerper >= 0.8f)
					{
						float interpolant = (lerper - 0.8f) / 0.2f;
						barrelRot += MathHelper.Lerp(maxRotation, 0, EaseBuilder.EaseInOutBack(2.3f).Ease(interpolant));
					}
				}
			}

			var barrelData = new DrawData(barrel, drawData.position + new Vector2(43 * drawInfo.drawPlayer.direction, 0).RotatedBy(drawData.rotation), null, drawData.color, barrelRot, barrel.Size() / 2f, drawData.scale, flip, 0f);

			drawInfo.DrawDataCache.Add(handleData);
			drawInfo.DrawDataCache.Add(barrelData);

			return false;
		}

		return true;
	}

	public static void ReloadUseStyle(Item item, Player player, Rectangle heldItemFrame, int shootDirection, float shootRotation, Vector2 itemSize, Vector2 itemOrigin, float animProgress)
	{
		float itemRotation = player.compositeBackArm.rotation + 1.5707964f * player.gravDir;
		Vector2 itemPosition = player.MountedCenter;

		const float back = -6f;
		const float front = -2f;

		if (animProgress < 0.1f)
		{
			float lerper = animProgress / 0.1f;
			itemPosition += itemRotation.ToRotationVector2() * MathHelper.Lerp(0f, back, EaseFunction.EaseCircularInOut.Ease(lerper));
		}
		else
		{
			float lerper = (animProgress - 0.1f) / 0.9f;

			if (lerper < 0.1f)
				itemPosition += itemRotation.ToRotationVector2() * back;
			else if (lerper < 0.25f)
			{
				float interpolant = (lerper - 0.1f) / 0.15f;
				itemPosition += itemRotation.ToRotationVector2() * MathHelper.Lerp(back, front, EaseFunction.EaseCircularOut.Ease(interpolant));
			}
			else if (lerper < 0.8f)
				itemPosition += itemRotation.ToRotationVector2() * front;
			else if (lerper >= 0.8f)
			{
				float interpolant = (lerper - 0.8f) / 0.2f;
				itemPosition += itemRotation.ToRotationVector2() * MathHelper.Lerp(front, -0f, EaseFunction.EaseInOutBack().Ease(interpolant));
			}
		}

		//if (animProgress < 0.3f)
		//	Dust.NewDustPerfect(itemPosition + new Vector2(42, -8 * player.direction).RotatedBy(itemRotation), DustID.Smoke, -Vector2.UnitY + Main.rand.NextVector2Circular(0.3f, 0.3f), 250, default, 3f);

		ItemVisualHelpers.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin, true, false, true);
	}

	public static void ReloadUseFrame(Item item, Player player, int shootDirection, float shootRotation, Vector2 itemSize, Vector2 itemOrigin, float animProgress)
	{
		float rotation = shootRotation * player.gravDir + 1.5707964f;
		float frontArmRotation = shootRotation * player.gravDir + 1.5707964f;

		Player.CompositeArmStretchAmount frontStretch = Player.CompositeArmStretchAmount.ThreeQuarters;

		const float min = 0.2f;
		const float max = 0.45f;

		if (animProgress < 0.05f)
		{
			float lerper = animProgress / 0.05f;
			rotation += MathHelper.Lerp(0f, min, EaseFunction.EaseCircularOut.Ease(lerper)) * player.direction;
		}
		else
		{
			float lerper = (animProgress - 0.05f) / 0.95f;

			if (lerper < 0.05f)
				rotation += min * player.direction;
			else if (lerper < 0.25f)
			{
				float interpolant = (lerper - 0.05f) / 0.2f;
				rotation += MathHelper.Lerp(min, max, EaseFunction.EaseOutBack(3).Ease(interpolant)) * player.direction;
			}
			else if (lerper < 0.8f)
				rotation += max * player.direction;
			else if (lerper >= 0.8f)
			{
				float interpolant = (lerper - 0.8f) / 0.2f;
				rotation += MathHelper.Lerp(max, 0f, EaseFunction.EaseInOutBack(4.2f).Ease(interpolant)) * player.direction;
			}
		}

		player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, rotation);
		player.SetCompositeArmFront(true, frontStretch, frontArmRotation);
	}
}
