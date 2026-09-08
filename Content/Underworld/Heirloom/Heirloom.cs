using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ItemCommon.MagazineSystem;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Subclasses.Shotguns;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Items.Pepperbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using static SpiritReforged.Common.ItemCommon.MagazineSystem.MagazineGlobalItem;

namespace SpiritReforged.Content.Underworld.Heirloom;
public class Heirloom() : ShotgunItem(new())
{
	// For reload animation
	public bool spawnedReloadDust = false;
	public bool playedClickSound = false;
	public ShotgunAmmoItem lastUsedAmmo;

	// Only let the player use the alternate use when at full magazine
	public override bool AltFunctionUse(Player player) => Item.GetGlobalItem<MagazineGlobalItem>().GetCurrentMagazine().AmmoUsed == 0;

	public override void SafeSetDefaults()
	{
		Item.damage = 32;
		Item.knockBack = 10f;
		Item.width = 62;
		Item.height = 22;
		Item.useTime = Item.useAnimation = 55;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.noMelee = true;
		Item.noUseGraphic = false;
		Item.value = Item.buyPrice(0, 3, 50, 0);
		Item.rare = ItemRarityID.Orange;
		Item.autoReuse = true;

		var globalItem = Item.GetGlobalItem<MagazineGlobalItem>();

		globalItem.ActivateMagazine(Item, (pitch, position) => SoundEngine.PlaySound(SoundID.Item36 with { Pitch = pitch }, position), new(-0.2f, 0.5f, 2, 120), new(62, 22), new(-24, -2), MagazineReloadType.EntireMagazine, MagazineUIType.Shell, true, -12, -0.25f);
		globalItem.SetAnimations(new(0.04f, 0.96f), reloadStyle: ReloadUseStyle, reloadFrame: ReloadUseFrame);
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		spawnedReloadDust = false;
		playedClickSound = false;

		if (player.altFunctionUse == 2)
		{
			player.velocity -= velocity * 5f;

			if (Main.myPlayer == player.whoAmI)
				ScreenshakeHelper.Shake(player.Center, -velocity, 4, 2, 10);

			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/Cannon_2"), position);
			
			shotgunStats = new(0, 0, 0, 0.1f, 0, 0.5f);

			var mp = player.GetModPlayer<MagazinePlayer>();

			for (int i = 0; i < 2; i++)
			{
				float strength = 1f;
				// if the player has more than two shots, add 20% strength per
				if (mp.GetMagazineSize() > 2)
					strength += (mp.GetMagazineSize() - 2) * 0.20f;

				base.Shoot(player, source, position, velocity, type, (int)(damage * strength), knockback * strength);
			}

			var magazineWeapon = Item.GetGlobalItem<MagazineGlobalItem>();

			// ensure we fire the rest of the magazine
			// We could just set the ammo used to magazine size, but this works with the UI, and also procs a reload
			while (magazineWeapon.AmmoRemaining(player) > 0)
				magazineWeapon.Fire(Item, player);

			shotgunStats = new();

			return false;
		}

		return base.Shoot(player, source, position, velocity, type, damage, knockback);
	}

	public override void AdditionalShoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, ShotgunAmmoItem ammo, int damage, float knockback)
	{
		Vector2 barrelPosition = position + new Vector2(42, -8 * player.direction).RotatedBy(velocity.ToRotation());

		if (player.altFunctionUse == 2)
		{
			ParticleHandler.SpawnParticle(new TexturedPulseCircle(
				barrelPosition,
				Color.OrangeRed.Additive(),
				Color.Yellow.Additive(),
				0.35f,
				150,
				35,
				"swirlNoise",
				new Vector2(2, 3),
				EaseFunction.EaseCubicOut).WithSkew(0.9f, velocity.ToRotation()).UsesLightColor());

			for (int i = 0; i < 3; i++)
			{
				Vector2 vel = velocity.RotateRandom(0.6f) * Main.rand.NextFloat(4f);
				Vector2 pos = barrelPosition + Main.rand.NextVector2Circular(6, 6);

				ParticleHandler.SpawnParticle(new BloomParticle(pos, vel, Color.DarkOrange.Additive() * 0.75f, 0.3f, 45, extraUpdateAction: p => p.Velocity *= 0.95f));
				ParticleHandler.SpawnParticle(new GlowParticle(pos, vel, Color.LightYellow.Additive(), 0.25f, 45, extraUpdateAction: p => p.Velocity *= 0.95f));

				Dust.NewDustPerfect(barrelPosition + Main.rand.NextVector2Circular(4, 4), DustID.Torch, velocity.RotatedByRandom(0.6f) * Main.rand.NextFloat(1f, 6f), 0, default, Main.rand.NextFloat(1f, 3.5f)).noGravity = true;
			}
		}

		for (int i = 0; i < 6; i++)
		{
			Dust.NewDustPerfect(barrelPosition + Main.rand.NextVector2Circular(3, 3), DustID.Torch, velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(1f, 5f), 0, default, Main.rand.NextFloat(1f, 2.5f)).noGravity = true;
		}

		for (int i = 0; i < 4; i++)
		{
			ParticleHandler.SpawnParticle(new SmokeCloud(barrelPosition + Main.rand.NextVector2Circular(8, 8), velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(3f), Color.Black * 0.33f, Main.rand.NextFloat(0.05f, 0.15f), EaseFunction.EaseQuadOut, 30 + Main.rand.Next(60), true)
			{
				Pixellate = true,
				PixelDivisor = 3
			});

			ParticleHandler.SpawnParticle(new CompositeSmoke(barrelPosition + Main.rand.NextVector2Circular(5, 5), velocity.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.2f, 1f), Color.Black, 50 + Main.rand.Next(30), false, false)
			{
				Layer = ParticleLayer.AbovePlayer
			});

			ParticleHandler.SpawnParticle(new SmallCompositeSmoke(barrelPosition + Main.rand.NextVector2Circular(5, 5), velocity.RotatedByRandom(0.4f) * Main.rand.NextFloat(2f, 5f), new Color(20, 20, 20) * 0.66f, 35 + Main.rand.Next(30), false, false, SmokeAction)
			{
				Layer = ParticleLayer.AbovePlayer
			});		
		}

		static void SmokeAction(Particle p)
		{
			p.Velocity *= 0.97f;
			p.Velocity.Y -= 0.02f;
		}

		lastUsedAmmo = ammo;
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
		}

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

		if (item.ModItem is Heirloom heirloom)
		{
			if (animProgress > 0.2f && !heirloom.spawnedReloadDust)
			{
				Vector2 breakPosition = itemPosition + new Vector2(12f, 2f).RotatedBy(itemRotation);
				Vector2 breakVelocity = -Vector2.UnitY - Vector2.UnitX * player.direction;

				for (int i = 0; i < 3; i++)
				{
					Dust.NewDustPerfect(breakPosition + Main.rand.NextVector2Circular(15, 15), DustID.Torch, breakVelocity * Main.rand.NextFloat(2f) + Main.rand.NextVector2Circular(0.5f, 0.5f), 0, default, Main.rand.NextFloat(3f)).noGravity = true;
					Dust.NewDustPerfect(breakPosition + Main.rand.NextVector2Circular(15, 15), DustID.Torch, breakVelocity * Main.rand.NextFloat(2f) + Main.rand.NextVector2Circular(0.5f, 0.5f), 0, default, Main.rand.NextFloat(2f));

					ParticleHandler.SpawnParticle(new SmallCompositeSmoke(breakPosition + Main.rand.NextVector2Circular(5, 5), breakVelocity + Main.rand.NextVector2Circular(0.25f, 0.25f), Color.Black * 0.5f, 65, false, false)
					{
						Layer = ParticleLayer.AbovePlayer
					});

					ParticleHandler.SpawnParticle(new CompositeSmoke(breakPosition + Main.rand.NextVector2Circular(5, 5), breakVelocity + Main.rand.NextVector2Circular(0.25f, 0.25f), Color.DarkGray * 0.5f, 45, false, false)
					{
						Layer = ParticleLayer.AbovePlayer
					});
				}

				for (int i = 0; i < 2; i++)
					ParticleHandler.SpawnParticle(new ShotgunShellParticle(breakPosition, -Vector2.UnitY * Main.rand.NextFloat(3f, 6f) - Vector2.UnitX * player.direction * Main.rand.NextFloat(1.5f, 3f), 1f, 120, heirloom.lastUsedAmmo));

				SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/ShotgunOpen"), breakPosition);

				heirloom.spawnedReloadDust = true;
			}
			
			if (animProgress > 0.9f && !heirloom.playedClickSound)
			{
				SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/EmptyMagazine"), player.Center);

				heirloom.playedClickSound = true;
			}
		}

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

	public override ModItem Clone(Item newEntity)
	{
		var newItem = newEntity.ModItem as Heirloom;

		newItem.spawnedReloadDust = spawnedReloadDust;
		newItem.lastUsedAmmo = lastUsedAmmo;
		newItem.playedClickSound = playedClickSound;

		return newEntity.ModItem;
	}
}
