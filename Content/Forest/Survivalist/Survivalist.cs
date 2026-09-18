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

	public override bool AltFunctionUse(Player player) => cooldown <= 0;

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
			Projectile.NewProjectile(source, position, velocity, ProjectileID.Grenade, damage * 2, knockback * 2, player.whoAmI);

			return false;
		}

		return base.Shoot(player, source, position, velocity, type, damage, knockback);
	}

	public override bool ModifyItemDraw(ref PlayerDrawSet drawInfo, ref DrawData drawData, ref DrawData? coloredDrawData, ref DrawData? glowMaskDrawData)
	{
		/*if (Item.TryGetGlobalItem<MagazineGlobalItem>(out var magazineWeapon) && magazineWeapon.Reloading)
		{
			float animationProgress = magazineWeapon.ReloadProgress(drawInfo.drawPlayer, Item);

			var handle = ModContent.Request<Texture2D>(Texture + "_Handle").Value;
			var barrel = ModContent.Request<Texture2D>(Texture + "_Barrel").Value;

			SpriteEffects flip = drawInfo.drawPlayer.direction == -1 ? SpriteEffects.FlipHorizontally : 0;

			var handleData = new DrawData(handle, drawData.position, drawData.sourceRect, drawData.color, drawData.rotation, drawData.origin, drawData.scale, flip, 0f);

			float maxRotation = 1.1f * drawInfo.drawPlayer.direction;

			float barrelRot = drawData.rotation;
			if (animationProgress is > 0.25f and < 0.35f)
			{
				float lerp = EaseBuilder.EaseCircularOut.Ease((animationProgress - 0.25f) / 0.1f);

				barrelRot += MathHelper.Lerp(0, maxRotation, lerp);
			}
			else if (animationProgress is >= 0.35f and < 0.75f)
				barrelRot += maxRotation;
			else if (animationProgress >= 0.75f)
			{
				float lerp = EaseBuilder.EaseInOutBack().Ease((animationProgress - 0.75f) / 0.25f);

				barrelRot += MathHelper.Lerp(maxRotation, 0, lerp);
			}

			var barrelData = new DrawData(barrel, drawData.position + new Vector2(40 * drawInfo.drawPlayer.direction, 0).RotatedBy(drawData.rotation), null, drawData.color, barrelRot, barrel.Size() / 2f, drawData.scale, flip, 0f);

			drawInfo.DrawDataCache.Add(handleData);
			drawInfo.DrawDataCache.Add(barrelData);

			return false;
		}*/

		return true;
	}

	public static void ReloadUseStyle(Item item, Player player, Rectangle heldItemFrame, int shootDirection, float shootRotation, Vector2 itemSize, Vector2 itemOrigin, float animProgress)
	{
		float itemRotation = player.compositeBackArm.rotation + 1.5707964f * player.gravDir;
		Vector2 itemPosition = player.MountedCenter;

		if (animProgress < 0.15f)
		{
			float lerper = animProgress / 0.15f;
			itemPosition += itemRotation.ToRotationVector2() * MathHelper.Lerp(0f, -2f, EaseFunction.EaseCircularInOut.Ease(lerper));
		}
		else
		{
			if (animProgress < 0.75f)
			{
				itemPosition += itemRotation.ToRotationVector2() * -2f;
			}
			else
			{
				float lerper = (animProgress - 0.75f) / 0.25f;
				itemPosition += itemRotation.ToRotationVector2() * MathHelper.Lerp(-2f, 0f, EaseFunction.EaseCircularInOut.Ease(lerper));
			}
		}

		if (animProgress < 0.3f)
			Dust.NewDustPerfect(itemPosition + new Vector2(42, -8 * player.direction).RotatedBy(itemRotation), DustID.Smoke, -Vector2.UnitY + Main.rand.NextVector2Circular(0.3f, 0.3f), 250, default, 3f);

		ItemVisualHelpers.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin, true, false, true);
	}

	public static void ReloadUseFrame(Item item, Player player, int shootDirection, float shootRotation, Vector2 itemSize, Vector2 itemOrigin, float animProgress)
	{
		float rotation = shootRotation * player.gravDir + 1.5707964f;
		float frontArmRotation = shootRotation * player.gravDir + 1.5707964f;

		Player.CompositeArmStretchAmount frontStretch = Player.CompositeArmStretchAmount.ThreeQuarters;

		const float kickback = -0.35f;
		const float lowerRotation = 0.6f;
		const float endRotation = -0.05f;

		if (animProgress < 0.35f)
		{
			if (animProgress < 0.1f)
			{
				float lerper = animProgress / 0.1f;
				rotation += MathHelper.Lerp(0f, kickback, EaseFunction.EaseCircularOut.Ease(lerper)) * player.direction;
				frontArmRotation += MathHelper.Lerp(0f, kickback, EaseFunction.EaseCircularOut.Ease(lerper)) * player.direction;
			}
			else
			{
				float lerper = (animProgress - 0.1f) / 0.25f;
				rotation += MathHelper.Lerp(kickback, lowerRotation, EaseFunction.EaseCircularInOut.Ease(lerper)) * player.direction;
				frontArmRotation -= 0.15f * player.direction;
			}
		}
		else
		{
			if (animProgress > 0.75f)
			{
				frontStretch = Player.CompositeArmStretchAmount.None;
				if (animProgress > 0.85f)
					frontStretch = Player.CompositeArmStretchAmount.Full;

				float lerper = (animProgress - 0.75f) / 0.25f;
				rotation += MathHelper.Lerp(lowerRotation, endRotation, EaseFunction.EaseInOutBack(3f).Ease(lerper)) * player.direction;
			}
			else
			{
				frontStretch = Player.CompositeArmStretchAmount.Quarter;

				frontArmRotation -= 0.15f * player.direction;

				rotation += lowerRotation * player.direction;
			}
		}

		player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, rotation);
		player.SetCompositeArmFront(true, frontStretch, frontArmRotation);
	}
}
