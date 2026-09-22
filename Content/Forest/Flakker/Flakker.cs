using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ItemCommon.MagazineSystem;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Subclasses.Shotguns;
using SpiritReforged.Content.Aether.Items;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Items.Pepperbox;
using SpiritReforged.Content.Underground.Tiles;
using Terraria.Audio;
using Terraria.DataStructures;
using static SpiritReforged.Common.ItemCommon.MagazineSystem.MagazineGlobalItem;

namespace SpiritReforged.Content.Forest.Flakker;
public class Flakker() : ShotgunItem(new(shotMultiplier: -0.33f, additionalSpread: 0.2f, spreadMultiplier: 0.65f))
{
	bool[] reloadEffects;
	public override void SafeSetDefaults()
	{
		Item.damage = 6;
		Item.knockBack = 2f;
		Item.width = 66;
		Item.height = 24;
		Item.useTime = Item.useAnimation = 95;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.noMelee = true;
		Item.noUseGraphic = false;
		Item.value = Item.buyPrice(0, 3, 50, 0);
		Item.rare = ItemRarityID.Orange;
		Item.autoReuse = true;

		var globalItem = Item.GetGlobalItem<MagazineGlobalItem>();
		globalItem.ActivateMagazine(Item, (pitch, position) => SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/Subclasses/Shotguns/ShotgunShoot_0" + Main.rand.Next(1, 4)) with { Pitch = pitch, Volume = 0.5f, PitchVariance = 0.1f }, position), new(-0.2f, 0.7f, 8, 70), new(62, 26), new(-18, -2), MagazineReloadType.EntireMagazine, MagazineUIType.Shell, true, false, -18, -0.15f);
		globalItem.SetAnimations(new(0.04f, 0.96f), reloadStyle: ReloadUseStyle, reloadFrame: ReloadUseFrame);
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		reloadEffects = new bool[5];

		if (ContentSamples.ItemsByType[source.AmmoItemIdUsed].ModItem is not ShotgunAmmoItem ammo)
			return false;

		Vector2 muzzlePosition = position + new Vector2(56, -2 * player.direction).RotatedBy(velocity.ToRotation());
		Vector2 shellPosition = position + new Vector2(12, -10 * player.direction).RotatedBy(velocity.ToRotation());

		for (int i = 0; i < player.GetModPlayer<ShotgunPlayer>().ModifyShotCount(3); i++)
		{
			ParticleHandler.SpawnParticle(new ShotgunShellParticle(shellPosition, -velocity.RotatedByRandom(0.4f) * Main.rand.NextFloat(1f, 4f) - Vector2.UnitY * Main.rand.NextFloat(3f), 1f, 120, ammo));

			float baseSpread = i == 0 ? 0 : 0.25f;

			PreNewProjectile.New(source, position, velocity.RotatedByRandom(player.GetModPlayer<ShotgunPlayer>().ModifySpread(baseSpread)) * Main.rand.NextFloat(10, 15),
				ModContent.ProjectileType<FlakkerProjectile>(), damage, knockback, player.whoAmI, source.AmmoItemIdUsed, preSpawnAction: p => (p.ModProjectile as FlakkerProjectile).parent = Item.ModItem as Flakker);
		}

		for (int i = 0; i < 5; i++)
		{
			Dust.NewDustPerfect(shellPosition, DustID.Torch, -velocity.RotatedByRandom(0.4f) * Main.rand.NextFloat(1f, 4f) - Vector2.UnitY * Main.rand.NextFloat(3f), 0, default, Main.rand.NextFloat(1f, 2f));
			
			ParticleHandler.SpawnParticle(new SmokeCloud(shellPosition, -velocity.RotatedByRandom(0.2f) * Main.rand.NextFloat(1f, 4f) - Vector2.UnitY * Main.rand.NextFloat(2f), Color.DarkGray * 0.3f, 0.05f, EaseFunction.EaseQuadOut, 90)
			{
				Pixellate = true,
				PixelDivisor = 3,
			});

			ParticleHandler.SpawnParticle(new SmokeCloud(muzzlePosition, velocity.RotatedByRandom(0.1f) * Main.rand.NextFloat(2f), Color.DarkGray * 0.2f, 0.15f, EaseFunction.EaseQuadOut, 90)
			{
				Pixellate = true,
				PixelDivisor = 2,
			});

			ParticleHandler.SpawnParticle(new SmokeCloud(muzzlePosition, velocity.RotatedByRandom(0.1f) * Main.rand.NextFloat(5f), Color.Black * 0.3f, 0.1f, EaseFunction.EaseQuadOut, 60)
			{
				Pixellate = true,
				PixelDivisor = 2,
			});

			ParticleHandler.SpawnParticle(new CompositeSmoke(muzzlePosition + Main.rand.NextVector2Circular(5f, 5f), velocity * Main.rand.NextFloat(2f), Color.Gray, Main.rand.Next(90), false, false, (particle) => particle.Velocity.Y -= 0.01f, 0.02f));

			ParticleHandler.SpawnParticle(new SmallCompositeSmoke(muzzlePosition + Main.rand.NextVector2Circular(5f, 5f), velocity * Main.rand.NextFloat(2f), Color.LightYellow, Main.rand.Next(90), false, false, (particle) => particle.Velocity.Y -= 0.005f, 0.02f));

			for (int x = 0; x < 3; x++)
			{
				ParticleHandler.SpawnParticle(new GlowParticle(muzzlePosition, velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(3f), Color.OrangeRed, 0.4f, 30, 2, p => p.Velocity *= 0.9f));

				Dust.NewDustPerfect(muzzlePosition, DustID.Torch, velocity.RotatedByRandom(0.4f) * Main.rand.NextFloat(2f, 9f), 0, default, Main.rand.NextFloat(1f, 4f)).noGravity = true;
			}
		}

		return false;
	}

	public override ModItem Clone(Item newEntity)
	{
		var newItem = newEntity.ModItem as Flakker;

		newItem.reloadEffects = reloadEffects;

		return newEntity.ModItem;
	}

	public override bool ModifyItemDraw(ref PlayerDrawSet drawInfo, ref DrawData drawData, ref DrawData? coloredDrawData, ref DrawData? glowMaskDrawData)
	{
		if (Item.TryGetGlobalItem<MagazineGlobalItem>(out var magazineWeapon) && magazineWeapon.Reloading)
		{
			float animationProgress = magazineWeapon.ReloadProgress(drawInfo.drawPlayer, Item);

			var handle = ModContent.Request<Texture2D>(Texture + "_Handle").Value;
			var barrel = ModContent.Request<Texture2D>(Texture + "_Barrel").Value;
			var mag = ModContent.Request<Texture2D>(Texture + "_Magazine").Value;

			SpriteEffects flip = drawInfo.drawPlayer.direction == -1 ? SpriteEffects.FlipHorizontally : 0;

			var handleData = new DrawData(handle, drawData.position, drawData.sourceRect, drawData.color, drawData.rotation, drawData.origin, drawData.scale, flip, 0f);

			float barrelOffset;

			if (animationProgress < 0.2f)
				barrelOffset = MathHelper.Lerp(0, 4, EaseBuilder.EaseCircularOut.Ease(animationProgress / 0.2f));
			else if (animationProgress < 0.8f)
				barrelOffset = 4;
			else
				barrelOffset = MathHelper.Lerp(4, 0, EaseBuilder.EaseOutBack(6).Ease((animationProgress - 0.8f) / 0.2f));

			var barrelData = new DrawData(barrel, drawData.position + new Vector2((41 + barrelOffset) * drawInfo.drawPlayer.direction, 0).RotatedBy(drawData.rotation), null, drawData.color, drawData.rotation, barrel.Size() / 2f, drawData.scale, flip, 0f);
			
			var magData = new DrawData(mag, drawData.position + new Vector2(41 * drawInfo.drawPlayer.direction, 0).RotatedBy(drawData.rotation), null, drawData.color, drawData.rotation, mag.Size() / 2f, drawData.scale, flip, 0f);

			drawInfo.DrawDataCache.Add(handleData);
			drawInfo.DrawDataCache.Add(barrelData);

			if (animationProgress is < 0.15f or > 0.65f)
				drawInfo.DrawDataCache.Add(magData);
			else if (animationProgress > 0.3f)
			{
				float progress = (animationProgress - 0.3f) / 0.35f;

				float fadeIn = 1f;
				if (progress < 0.5f)
					fadeIn = progress / 0.5f;

				Vector2 offset = new Vector2(-3 * drawInfo.drawPlayer.direction, 7).RotatedBy(drawData.rotation) * EaseBuilder.EaseOutBack(4).Ease(1f - progress);

				float rotationOffset = 0.45f * EaseBuilder.EaseOutBack(5).Ease(1f - progress) * drawInfo.drawPlayer.direction;

				magData = new DrawData(mag, drawData.position + new Vector2(41 * drawInfo.drawPlayer.direction, 0).RotatedBy(drawData.rotation) + offset, null, drawData.color * fadeIn, drawData.rotation + rotationOffset, mag.Size() / 2f, drawData.scale, flip, 0f);
				drawInfo.DrawDataCache.Add(magData);
			}

			return false;
		}

		return true;
	}
	public static void ReloadUseStyle(Item item, Player player, Rectangle heldItemFrame, int shootDirection, float shootRotation, Vector2 itemSize, Vector2 itemOrigin, float animProgress)
	{
		float itemRotation = player.compositeFrontArm.rotation + 1.5707964f * player.gravDir;
		Vector2 itemPosition = player.MountedCenter;

		float back = -8f;
		float front = 4f;

		if (animProgress < 0.1f)
		{
			float lerper = animProgress / 0.1f;
			itemPosition += itemRotation.ToRotationVector2() * MathHelper.Lerp(0f, back, EaseFunction.EaseCircularOut.Ease(lerper));
		}
		else if (animProgress < 0.2f)
		{
			float lerper = (animProgress - 0.1f) / 0.1f;
			itemPosition += itemRotation.ToRotationVector2() * MathHelper.Lerp(back, front, EaseFunction.EaseCircularInOut.Ease(lerper));

		}
		else if (animProgress < 0.6f)
		{
			float lerper = (animProgress - 0.2f) / 0.4f;
			itemPosition += itemRotation.ToRotationVector2() * MathHelper.Lerp(front, 0, EaseFunction.EaseCircularInOut.Ease(lerper));
		}
		else
		{
			float lerper = (animProgress - 0.6f) / 0.4f;
			itemPosition += itemRotation.ToRotationVector2() * MathHelper.Lerp(0, back, EaseFunction.EaseCircularInOut.Ease(lerper));
		}

		if (item.ModItem is not null and Flakker flakker)
		{
			if (animProgress > 0.15f && !flakker.reloadEffects[0])
			{
				Vector2 pos = itemPosition + new Vector2(14f, -4f * player.direction).RotatedBy(itemRotation);

				Gore.NewGorePerfect(item.GetSource_FromThis("Spirit Reforged: Flakker Reload"), pos, Vector2.UnitX.RotatedBy(itemRotation) * Main.rand.NextFloat(3f, 5f), flakker.Mod.Find<ModGore>("FlakkerMagazineGore").Type, 1f);

				SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing with { Volume = 0.33f }, pos);

				for (int i = 0; i < 3; i++)
					Dust.NewDustPerfect(pos, DustID.Smoke, Main.rand.NextVector2Circular(0.5f, 0.5f), 210, default, Main.rand.NextFloat(4f));

				flakker.reloadEffects[0] = true;
			}

			if (animProgress > 0.6f && !flakker.reloadEffects[1])
			{
				SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/Eject") with { Volume = 0.5f }, itemPosition);

				flakker.reloadEffects[1] = true;
			}

			if (animProgress > 0.7f && !flakker.reloadEffects[2])
			{
				SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/Subclasses/Shotguns/ShotgunPump") with { Volume = 0.65f }, itemPosition);

				flakker.reloadEffects[2] = true;
			}

			if (animProgress > 0.8f && !flakker.reloadEffects[3])
			{
				Vector2 dustPosition = itemPosition + new Vector2(10f, -12f * player.direction).RotatedBy(itemRotation);

				for (int i = 0; i < 5; i++)
				{
					Dust.NewDustPerfect(dustPosition, DustID.Smoke, -Vector2.UnitX.RotatedByRandom(0.3f).RotatedBy(itemRotation) * Main.rand.NextFloat(2f), 210, default, Main.rand.NextFloat(4f));
					
					Dust.NewDustPerfect(dustPosition, DustID.Torch, -Vector2.UnitX.RotatedByRandom(0.7f).RotatedBy(itemRotation) * Main.rand.NextFloat(5f), 0, default, Main.rand.NextFloat(4f)).noGravity = true;
				}

				flakker.reloadEffects[3] = true;
			}
		}

			

		ItemVisualHelpers.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin, true, false, true);
	}

	public static void ReloadUseFrame(Item item, Player player, int shootDirection, float shootRotation, Vector2 itemSize, Vector2 itemOrigin, float animProgress)
	{
		float rotation = shootRotation * player.gravDir + 1.5707964f;
		float backArmRotation = shootRotation * player.gravDir + 1.5707964f;

		Player.CompositeArmStretchAmount frontStretch = Player.CompositeArmStretchAmount.Full;

		float backArmOffset = 0f;

		if (animProgress < 0.1f)
		{
			float lerper = animProgress / 0.1f;
			rotation += MathHelper.Lerp(0, 0.1f, EaseFunction.EaseQuadInOut.Ease(lerper)) * player.direction;
		}
		else if (animProgress < 0.3f)
		{
			float lerper = (animProgress - 0.1f) / 0.2f;
			backArmOffset += MathHelper.Lerp(0f, 0.65f, EaseFunction.EaseOutBack(2.2).Ease(lerper)) * player.direction;
			rotation += MathHelper.Lerp(0.1f, -0.2f, EaseFunction.EaseOutBack(2.2).Ease(lerper)) * player.direction;
		}
		else if (animProgress < 0.45f)
		{
			float lerper = (animProgress - 0.3f) / 0.15f;
			rotation += MathHelper.Lerp(-0.2f, -0.1f, EaseFunction.EaseCircularInOut.Ease(lerper)) * player.direction;
		}
		else if (animProgress < 0.6f)
			rotation += -0.1f * player.direction;
		else if (animProgress < 0.8f)
		{
			float lerper = (animProgress - 0.6f) / 0.2f;
			rotation += MathHelper.Lerp(-0.1f, -0.3f, EaseFunction.EaseInOutBack(1.2f).Ease(lerper)) * player.direction;
		}
		else
		{
			float lerper = (animProgress - 0.8f) / 0.2f;
			rotation += MathHelper.Lerp(-0.3f, 0f, EaseFunction.EaseQuadInOut.Ease(lerper)) * player.direction;

			if (animProgress < 0.85)
				frontStretch = Player.CompositeArmStretchAmount.ThreeQuarters;
			else if (animProgress < 0.9)
				frontStretch = Player.CompositeArmStretchAmount.Quarter;
			else
				frontStretch = Player.CompositeArmStretchAmount.None;
		}

		if (animProgress is > 0.3f and < 0.65f)
		{
			float lerper = (animProgress - 0.3f) / 0.35f;

			backArmOffset += 0.65f * EaseBuilder.EaseOutBack(3).Ease(1f - lerper) * player.direction;
		}

		player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, backArmRotation + backArmOffset);
		player.SetCompositeArmFront(true, frontStretch, rotation);
	}
}
