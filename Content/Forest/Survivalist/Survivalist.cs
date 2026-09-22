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
using SpiritReforged.Content.Aether.Items;
using SpiritReforged.Common.Misc;
using static tModPorter.ProgressUpdate;
using SpiritReforged.Content.Underground.Items.Pepperbox;

namespace SpiritReforged.Content.Forest.Survivalist;
public class Survivalist() : ShotgunItem(new(1, speedMultiplier: 0.33f))
{
	public const int MAX_COOLDOWN = 300;

	public ShotgunAmmoItem lastUsedAmmo;

	bool[] reloadEffects;
	bool spawnedEffects = false;

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

		globalItem.ActivateMagazine(Item, (pitch, position) => SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/Subclasses/Shotguns/ShotgunShoot_0" + Main.rand.Next(1, 4)) with { Pitch = pitch, Volume = 0.66f, PitchVariance = 0.1f }, position), new(-0.2f, 0.6f, 5, 180), new(66, 24), new(-18, -2), MagazineReloadType.OneAtATime, MagazineUIType.Shell, true, false, -16, -0.3f);
		globalItem.SetAnimations(null, ShootUseStyle, ShootUseFrame, ReloadUseStyle, ReloadUseFrame);
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		reloadEffects = new bool[5];
		spawnedEffects = false;

		if (player.altFunctionUse == 2)
		{
			cooldown = MAX_COOLDOWN;

			Vector2 underbarrelPosition = position + new Vector2(46, 2 * player.direction).RotatedBy(velocity.ToRotation());
			
			for (int i = 0; i < 8; i++)
			{
				ParticleHandler.SpawnParticle(new SmokeCloud(underbarrelPosition, velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(3f), Color.DarkGray * 0.3f, 0.05f, EaseFunction.EaseQuadOut, 60)
				{
					Pixellate = true,
					PixelDivisor = 3,
				});

				Dust.NewDustPerfect(underbarrelPosition, DustID.Torch, velocity.RotatedByRandom(0.2f) * Main.rand.NextFloat(6f), 0, default, Main.rand.NextFloat(1f, 4f)).noGravity = true;
			}

			ParticleHandler.SpawnParticle(new TexturedPulseCircle(
				underbarrelPosition,
				Color.DarkOrange.Additive(),
				Color.Orange.Additive(),
				0.5f,
				80,
				35,
				"swirlNoise",
				new Vector2(2, 3),
				EaseFunction.EaseCubicOut).WithSkew(0.9f, velocity.ToRotation()).UsesLightColor());

			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/Subclasses/Shotguns/SurvivalistGrenadeShot_0" + Main.rand.Next(1, 3)) with { Volume = 0.5f, PitchVariance = 0.1f}, position);

			Projectile.NewProjectile(source, position, velocity * 11f, ModContent.ProjectileType<SurvivalistGrenadeProjectile>(), 30 + damage / 3, knockback * 2, player.whoAmI);

			return false;
		}

		return base.Shoot(player, source, position, velocity, type, damage, knockback);
	}

	public override void AdditionalShoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, ShotgunAmmoItem ammo, int damage, float knockback)
	{
		Vector2 muzzlePosition = position + new Vector2(56, -2 * player.direction).RotatedBy(velocity.ToRotation());

		for (int i = 0; i < 5; i++)
		{
			ParticleHandler.SpawnParticle(new SmokeCloud(muzzlePosition, velocity.RotatedByRandom(0.1f) * Main.rand.NextFloat(2f), Color.LightGray * 0.2f, 0.15f, EaseFunction.EaseQuadOut, 90)
			{
				Pixellate = true,
				PixelDivisor = 2,
			});

			ParticleHandler.SpawnParticle(new SmokeCloud(muzzlePosition, velocity.RotatedByRandom(0.1f) * Main.rand.NextFloat(5f), Color.DimGray * 0.3f, 0.1f, EaseFunction.EaseQuadOut, 60)
			{
				Pixellate = true,
				PixelDivisor = 2,
			});

			ParticleHandler.SpawnParticle(new CompositeSmoke(muzzlePosition + Main.rand.NextVector2Circular(5f, 5f), velocity * Main.rand.NextFloat(2f), Color.LightGray, Main.rand.Next(90), false, false, (particle) => particle.Velocity.Y -= 0.01f, 0.02f));
			
			ParticleHandler.SpawnParticle(new SmallCompositeSmoke(muzzlePosition + Main.rand.NextVector2Circular(5f, 5f), velocity * Main.rand.NextFloat(2f), Color.Gray, Main.rand.Next(90), false, false, (particle) => particle.Velocity.Y -= 0.005f, 0.02f));

			for (int x = 0; x < 2; x++)
			{ 
				ParticleHandler.SpawnParticle(new GlowParticle(muzzlePosition, velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(3f), Color.DarkOrange, 0.4f, 30, 2, p => p.Velocity *= 0.9f));
				
				ParticleHandler.SpawnParticle(new GlowParticle(muzzlePosition, Main.rand.NextVector2Circular(3, 3), Color.Orange, 0.2f, 40, 2, p => p.Velocity *= 0.9f));

				Dust.NewDustPerfect(muzzlePosition, DustID.Torch, Main.rand.NextVector2Circular(4, 4), 0, default, Main.rand.NextFloat(1f, 3f)).noGravity = true;
			}
		}

		lastUsedAmmo = ammo;
	}

	public override void UpdateInventory(Player player)
	{
		if (cooldown > 0)
		{
			cooldown--;
			if (cooldown == 0)
				SoundEngine.PlaySound(SoundID.MaxMana, player.Center);
		}		
	}

	public override ModItem Clone(Item newEntity)
	{
		var newItem = newEntity.ModItem as Survivalist;

		newItem.spawnedEffects = spawnedEffects;
		newItem.reloadEffects = reloadEffects;

		return newEntity.ModItem;
	}

	public override bool ModifyItemDraw(ref PlayerDrawSet drawInfo, ref DrawData drawData, ref DrawData? coloredDrawData, ref DrawData? glowMaskDrawData)
	{
		var handle = ModContent.Request<Texture2D>(Texture + "_Handle").Value;
		var barrel = ModContent.Request<Texture2D>(Texture + "_Barrel").Value;
		var underBarrel = ModContent.Request<Texture2D>(Texture + "_Underbarrel").Value;

		var player = drawInfo.drawPlayer;

		SpriteEffects flip = player.direction == -1 ? SpriteEffects.FlipHorizontally : 0;

		var handleData = new DrawData(handle, drawData.position, drawData.sourceRect, drawData.color, drawData.rotation, drawData.origin, drawData.scale, flip, 0f);

		float maxRotation = 0.6f * player.direction;

		float barrelRot = drawData.rotation;
		float underBarrelOffset = 0f;

		if (Item.TryGetGlobalItem<MagazineGlobalItem>(out var magazineWeapon) && magazineWeapon.Reloading)
		{
			float animationProgress = magazineWeapon.ReloadProgress(player, Item);

			if (animationProgress > 0.1f)
			{
				float lerper = (animationProgress - 0.1f) / 0.9f;

				if (lerper is > 0.1f)
				{
					if (lerper < 0.25f)
					{
						float interpolant = (lerper - 0.1f) / 0.15f;
						barrelRot += MathHelper.Lerp(0, maxRotation, EaseBuilder.EaseOutBack(2).Ease(interpolant));
					}
					else if (lerper < 0.6f)
						barrelRot += maxRotation;
					else if (lerper < 0.8f)
					{
						float interpolant = (lerper - 0.6f) / 0.2f;
						barrelRot += MathHelper.Lerp(maxRotation, 0, EaseBuilder.EaseInOutBack(2.3f).Ease(interpolant));
					}
					else if (lerper < 0.9f)
					{
						float lerp = (lerper - 0.8f) / 0.1f;
						underBarrelOffset = MathHelper.Lerp(0f, -6f, EaseBuilder.EaseCircularOut.Ease(lerp));
					}
					else
					{
						float lerp = (lerper - 0.9f) / 0.1f;
						underBarrelOffset = MathHelper.Lerp(-6f, 0f, EaseBuilder.EaseCircularInOut.Ease(lerp));
					}
				}
			}
		}
		else if (player.itemTime > 0 && player.altFunctionUse != 2)
		{
			float progress = 1f - player.itemTime / (float)player.itemTimeMax;

			if (progress is > 0.5f and < 0.7f)
			{
				float lerp = (progress - 0.5f) / 0.2f;
				underBarrelOffset = MathHelper.Lerp(0f, -6f, EaseBuilder.EaseCircularOut.Ease(lerp));
			}
			else if (progress >= 0.7f)
			{
				float lerp = (progress - 0.7f) / 0.3f;
				underBarrelOffset = MathHelper.Lerp(-6f, 0, EaseBuilder.EaseCircularInOut.Ease(lerp));
			}
		}

		var barrelData = new DrawData(barrel, drawData.position + new Vector2(43 * player.direction, 0).RotatedBy(drawData.rotation), null, drawData.color, barrelRot, barrel.Size() / 2f, drawData.scale, flip, 0f);

		var underBarrelData = new DrawData(underBarrel, drawData.position + new Vector2((43 + underBarrelOffset) * player.direction, 0).RotatedBy(drawData.rotation), null, drawData.color, barrelRot, barrel.Size() / 2f, drawData.scale, flip, 0f);

		drawInfo.DrawDataCache.Add(handleData);
		drawInfo.DrawDataCache.Add(barrelData);
		drawInfo.DrawDataCache.Add(underBarrelData);

		return false;
	}

	#region Shooting Animation

	static void ShootUseStyle(Item item, Player player, Rectangle heldItemFrame, int shootDirection, float shootRotation, Vector2 itemSize, Vector2 itemOrigin)
	{
		if (player.altFunctionUse == 2)
		{
			ItemVisualHelpers.SetGunUseStyle(player, item, shootDirection, -12, EaseFunction.EaseQuinticOut, EaseFunction.EaseOutBack(2.2f), itemSize, itemOrigin, new(0.05f, 0.95f));

			return;
		}

		float animProgress = 1f - player.itemTime / (float)player.itemTimeMax;

		if (Main.myPlayer == player.whoAmI)
			player.direction = shootDirection;

		float itemRotation = player.compositeFrontArm.rotation + 1.5707964f * player.gravDir;
		Vector2 itemPosition = player.MountedCenter;

		if (animProgress < 0.05f)
		{
			float lerper = animProgress / 0.25f;
			itemPosition += itemRotation.ToRotationVector2() * MathHelper.Lerp(0f, -14f, EaseFunction.EaseCircularOut.Ease(lerper));
		}
		else
		{
			float interpolant = (animProgress - 0.05f) / 0.95f;

			if (interpolant < 0.3f)
			{
				float lerper = interpolant / 0.3f;
				itemPosition += itemRotation.ToRotationVector2() * MathHelper.Lerp(-14, -8f, EaseFunction.EaseQuadIn.Ease(lerper));
			}
			else if (interpolant < 0.5f)
			{
				float lerper = (interpolant - 0.3f) / 0.2f;
				itemPosition += itemRotation.ToRotationVector2() * MathHelper.Lerp(-8, -6f, EaseFunction.EaseCircularInOut.Ease(lerper));
			}
			else if (interpolant < 0.7f)
			{
				if (item.ModItem is not null and Survivalist survivalist && !survivalist.spawnedEffects)
				{
					survivalist.spawnedEffects = true;

					Vector2 breakPosition = itemPosition + new Vector2(12f, 2f).RotatedBy(itemRotation);
					Vector2 breakVelocity = -Vector2.UnitY * 0.5f - Vector2.UnitX * player.direction;

					for (int i = 0; i < 3; i++)
					{
						Dust.NewDustPerfect(breakPosition + Main.rand.NextVector2Circular(15, 15), DustID.Torch, breakVelocity * Main.rand.NextFloat(2f) + Main.rand.NextVector2Circular(0.5f, 0.5f), 0, default, Main.rand.NextFloat(3f)).noGravity = true;
						Dust.NewDustPerfect(breakPosition + Main.rand.NextVector2Circular(15, 15), DustID.Torch, breakVelocity * Main.rand.NextFloat(2f) + Main.rand.NextVector2Circular(0.5f, 0.5f), 0, default, Main.rand.NextFloat(2f));

						ParticleHandler.SpawnParticle(new SmallCompositeSmoke(breakPosition + Main.rand.NextVector2Circular(5, 5), breakVelocity + Main.rand.NextVector2Circular(0.25f, 0.25f), Color.AntiqueWhite * 0.9f, 65, false, false)
						{
							Layer = ParticleLayer.AbovePlayer
						});

						ParticleHandler.SpawnParticle(new CompositeSmoke(breakPosition + Main.rand.NextVector2Circular(5, 5), breakVelocity + Main.rand.NextVector2Circular(0.25f, 0.25f), Color.Gray * 0.66f, 45, false, false)
						{
							Layer = ParticleLayer.AbovePlayer
						});
					}

					ParticleHandler.SpawnParticle(new ShotgunShellParticle(breakPosition, -Vector2.UnitY * Main.rand.NextFloat(1f, 3f) - Vector2.UnitX * player.direction * Main.rand.NextFloat(2f, 3.5f), 1f, 120, survivalist.lastUsedAmmo));
					SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/Subclasses/Shotguns/ShotgunPump") with { Volume = 0.75f }, breakPosition);
				}

				float lerper = (interpolant - 0.5f) / 0.2f;
				itemPosition += itemRotation.ToRotationVector2() * MathHelper.Lerp(-6, -12f, EaseFunction.EaseCircularOut.Ease(lerper));
			}
			else
			{
				float lerper = (interpolant - 0.7f) / 0.3f;
				itemPosition += itemRotation.ToRotationVector2() * MathHelper.Lerp(-12, -2f, EaseFunction.EaseCircularInOut.Ease(lerper));
			}
		}

		ItemVisualHelpers.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin, true, false, true);
	}

	static void ShootUseFrame(Item item, Player player, int shootDirection, float shootRotation, Vector2 itemSize, Vector2 itemOrigin)
	{
		if (player.altFunctionUse == 2)
		{
			ItemVisualHelpers.SetGunUseItemFrame(player, shootDirection, shootRotation, -0.25f, EaseFunction.EaseQuinticOut, EaseFunction.EaseOutBack(2.2f), false, new(0.05f, 0.95f));

			return;
		}

		if (Main.myPlayer == player.whoAmI)
			player.direction = shootDirection;

		float animProgress = 1f - player.itemTime / (float)player.itemTimeMax;
		float rotation = shootRotation * player.gravDir + 1.5707964f;

		if (animProgress < 0.05f)
		{
			float lerper = animProgress / 0.25f;
			rotation += MathHelper.Lerp(0f, -0.35f, EaseFunction.EaseCircularOut.Ease(lerper)) * player.direction;
		}
		else
		{
			float interpolant = (animProgress - 0.05f) / 0.95f;

			if (interpolant < 0.3f)
			{
				float lerper = interpolant / 0.3f;
				rotation += MathHelper.Lerp(-0.35f, 0f, EaseFunction.EaseQuadInOut.Ease(lerper)) * player.direction;
			}
			else if (interpolant < 0.5f)
			{
				float lerper = (interpolant - 0.3f) / 0.2f;
				rotation += MathHelper.Lerp(0f, 0.05f, EaseFunction.EaseCircularInOut.Ease(lerper)) * player.direction;
			}
			else if (interpolant < 0.7f)
			{
				float lerper = (interpolant - 0.5f) / 0.2f;
				rotation += MathHelper.Lerp(0.05f, 0.2f, EaseFunction.EaseCircularOut.Ease(lerper)) * player.direction;
			}
			else
			{
				float lerper = (interpolant - 0.7f) / 0.3f;
				rotation += MathHelper.Lerp(0.2f, 0f, EaseFunction.EaseCircularInOut.Ease(lerper)) * player.direction;
			}
		}

		Player.CompositeArmStretchAmount stretch = Player.CompositeArmStretchAmount.Full;
		if (animProgress < 0.5f)
			stretch = Player.CompositeArmStretchAmount.None;
		else if (animProgress < 0.75f)
			stretch = Player.CompositeArmStretchAmount.ThreeQuarters;

		player.SetCompositeArmFront(true, stretch, rotation);
	}

	#endregion

	#region Reload Animation
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

				if (item.ModItem is not null and Survivalist survivalist && !survivalist.reloadEffects[0])
				{
					Vector2 breakPosition = itemPosition + new Vector2(20f, -2f).RotatedBy(itemRotation);
					Vector2 breakVelocity = -Vector2.UnitY - Vector2.UnitX * player.direction;

					for (int i = 0; i < 3; i++)
					{
						Dust.NewDustPerfect(breakPosition + Main.rand.NextVector2Circular(15, 15), DustID.Torch, breakVelocity + Main.rand.NextVector2Circular(0.5f, 0.5f), 0, default, Main.rand.NextFloat(3f)).noGravity = true;

						Dust.NewDustPerfect(breakPosition + Main.rand.NextVector2Circular(15, 15), DustID.Smoke, breakVelocity + Main.rand.NextVector2Circular(0.5f, 0.5f), 120, default, Main.rand.NextFloat(4f)).noGravity = true;

						ParticleHandler.SpawnParticle(new SmallCompositeSmoke(breakPosition + Main.rand.NextVector2Circular(5, 5), breakVelocity + Main.rand.NextVector2Circular(0.25f, 0.25f), Color.AntiqueWhite * 0.5f, 45, false, false)
						{
							Layer = ParticleLayer.AbovePlayer
						});
					}

					SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/ShotgunOpen"), breakPosition);

					survivalist.reloadEffects[0] = true;
				}
			}
			else if (lerper < 0.6f)
				itemPosition += itemRotation.ToRotationVector2() * front;
			else if (lerper < 0.8f)
			{
				float interpolant = (lerper - 0.6f) / 0.2f;
				itemPosition += itemRotation.ToRotationVector2() * MathHelper.Lerp(front, 0f, EaseFunction.EaseInOutBack().Ease(interpolant));

				if (lerper > 0.77f && item.ModItem is not null and Survivalist survivalist && !survivalist.reloadEffects[1])
				{
					SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/EmptyMagazine"), player.Center);

					survivalist.reloadEffects[1] = true;
				}
			}
			else if (lerper < 0.9f)
			{
				float interpolant = (lerper - 0.8f) / 0.1f;
				itemPosition += itemRotation.ToRotationVector2() * MathHelper.Lerp(0, back * 1.5f, EaseFunction.EaseCircularOut.Ease(interpolant));

				if (item.ModItem is not null and Survivalist survivalist && !survivalist.reloadEffects[2])
				{
					SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/Subclasses/Shotguns/ShotgunPump") with { Volume = 0.75f }, player.Center);

					survivalist.reloadEffects[2] = true;
				}
			}
			else
			{
				float interpolant = (lerper - 0.9f) / 0.1f;
				itemPosition += itemRotation.ToRotationVector2() * MathHelper.Lerp(back * 1.5f, front, EaseFunction.EaseCircularInOut.Ease(interpolant));
			}
		}

		if (animProgress < 0.3f)
			Dust.NewDustPerfect(itemPosition + new Vector2(46, -10 * player.direction).RotatedBy(itemRotation), DustID.Smoke, -Vector2.UnitY + Main.rand.NextVector2Circular(0.3f, 0.3f), 250, default, 3f);

		ItemVisualHelpers.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin, true, false, true);
	}

	public static void ReloadUseFrame(Item item, Player player, int shootDirection, float shootRotation, Vector2 itemSize, Vector2 itemOrigin, float animProgress)
	{
		float rotation = shootRotation * player.gravDir + 1.5707964f;
		float frontArmRotation = shootRotation * player.gravDir + 1.5707964f;

		Player.CompositeArmStretchAmount frontStretch = Player.CompositeArmStretchAmount.ThreeQuarters;

		const float min = -0.15f;
		const float max = 0.45f;

		if (animProgress < 0.1f)
		{
			float lerper = animProgress / 0.1f;
			rotation += MathHelper.Lerp(0f, min + 0.1f, EaseFunction.EaseCircularOut.Ease(lerper)) * player.direction;
		}
		else
		{
			float lerper = (animProgress - 0.1f) / 0.9f;
			if (lerper < 0.1f)
				rotation += MathHelper.Lerp(min + 0.1f, min, EaseFunction.EaseInBack(3).Ease(lerper / 0.1f)) * player.direction;
			else if (lerper is > 0.1f and < 0.15f)
			{
				float interpolant = (lerper - 0.1f) / 0.05f;
				rotation += MathHelper.Lerp(min, max, EaseFunction.EaseInOutBack(2).Ease(interpolant)) * player.direction;
			}
			else if (lerper < 0.6f)
				rotation += max * player.direction;
			else if (lerper < 0.8f)
			{
				float interpolant = (lerper - 0.6f) / 0.2f;
				rotation += MathHelper.Lerp(max, 0f, EaseFunction.EaseInOutBack(2.2f).Ease(interpolant)) * player.direction;
			}
			else if (lerper < 0.9f)
			{
				float interpolant = (lerper - 0.8f) / 0.1f;
				rotation += MathHelper.Lerp(0, 0.2f, EaseFunction.EaseCircularOut.Ease(interpolant)) * player.direction;
			}
			else
			{
				float interpolant = (lerper - 0.9f) / 0.1f;
				rotation += MathHelper.Lerp(0.2f, 0f, EaseFunction.EaseCircularInOut.Ease(interpolant)) * player.direction;
			}
		}

		player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, rotation);
		player.SetCompositeArmFront(true, frontStretch, frontArmRotation);
	}
	#endregion
}
