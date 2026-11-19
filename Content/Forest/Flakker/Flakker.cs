using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.ItemCommon.MagazineSystem;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Subclasses.Shotguns;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Flakker;

public class Flakker : ReloadableItem
{
	public static readonly Asset<Texture2D> Handle = DrawHelpers.RequestLocal<Flakker>("Flakker_Handle", false);
	public static readonly Asset<Texture2D> Barrel = DrawHelpers.RequestLocal<Flakker>("Flakker_Barrel", false);
	public static readonly Asset<Texture2D> Mag = DrawHelpers.RequestLocal<Flakker>("Flakker_Magazine", false);

	private bool[] _reloadEffects;

	public override IReloadable.MagazineInfo Magazine => new(9, 55);

	public override InterfaceProfile? Profile => new(MagazineInterfacePlayer.StyleID.Shell);

	public override void SetStaticDefaults() => ShotgunGlobalItem.Shotgun[Type] = new(shotMultiplier: -0.2f, additionalSpread: 0.2f, spreadMultiplier: 0.65f);

	public override void SetDefaults()
	{
		Item.DefaultToShotgun(75, 1);
		Item.damage = 25;
		Item.knockBack = 2f;
		Item.width = 66;
		Item.height = 24;
		Item.value = Item.buyPrice(0, 3, 50, 0);
		Item.rare = ItemRarityID.Orange;
		Item.UseSound = new SoundStyle("SpiritReforged/Assets/SFX/Item/Subclasses/Shotguns/ShotgunShoot", 3) with { PitchVariance = 0.1f };
	}

	public override bool? UseItem(Player player)
	{
		_reloadEffects = new bool[5]; //Reset reloadEffects
		return null;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (ContentSamples.ItemsByType[source.AmmoItemIdUsed].ModItem is not ShotgunAmmoItem ammo)
			return false;

		Vector2 muzzlePosition = position + new Vector2(56, -2 * player.direction).RotatedBy(velocity.ToRotation());
		Vector2 shellPosition = position + new Vector2(12, -10 * player.direction).RotatedBy(velocity.ToRotation());

		for (int i = 0; i < player.GetModPlayer<ShotgunPlayer>().ModifyShotCount(3); i++)
		{
			ParticleRenderers.OverPlayers.Add(new ShotgunShellParticle(shellPosition, -velocity.RotatedByRandom(0.4f) * Main.rand.NextFloat(1f, 4f) - Vector2.UnitY * Main.rand.NextFloat(3f), 1f, 120, ammo));

			float baseSpread = i == 0 ? 0 : 0.25f;

			PreNewProjectile.New(source, position, velocity.RotatedByRandom(player.GetModPlayer<ShotgunPlayer>().ModifySpread(baseSpread)) * Main.rand.NextFloat(10, 15),
				ModContent.ProjectileType<FlakkerProjectile>(), damage / 3, knockback, player.whoAmI, source.AmmoItemIdUsed, preSpawnAction: p => (p.ModProjectile as FlakkerProjectile).parent = Item.ModItem as Flakker);
		}

		for (int i = 0; i < 5; i++)
		{
			Dust.NewDustPerfect(shellPosition, DustID.Torch, -velocity.RotatedByRandom(0.4f) * Main.rand.NextFloat(1f, 4f) - Vector2.UnitY * Main.rand.NextFloat(3f), 0, default, Main.rand.NextFloat(1f, 2f));

			ParticleRenderers.OverPlayers.Add(new SmokeCloud(shellPosition, -velocity.RotatedByRandom(0.2f) * Main.rand.NextFloat(1f, 4f) - Vector2.UnitY * Main.rand.NextFloat(2f), Color.DarkGray * 0.3f, 0.05f, EaseFunction.EaseQuadOut, 90)
			{
				Pixellate = true,
				PixelDivisor = 3,
			});

			ParticleRenderers.OverPlayers.Add(new SmokeCloud(muzzlePosition, velocity.RotatedByRandom(0.1f) * Main.rand.NextFloat(2f), Color.DarkGray * 0.2f, 0.15f, EaseFunction.EaseQuadOut, 90)
			{
				Pixellate = true,
				PixelDivisor = 2,
			});

			ParticleRenderers.OverPlayers.Add(new SmokeCloud(muzzlePosition, velocity.RotatedByRandom(0.1f) * Main.rand.NextFloat(5f), Color.Black * 0.3f, 0.1f, EaseFunction.EaseQuadOut, 60)
			{
				Pixellate = true,
				PixelDivisor = 2,
			});

			ParticleRenderers.OverPlayers.Add(new CompositeSmoke(muzzlePosition + Main.rand.NextVector2Circular(5f, 5f), velocity * Main.rand.NextFloat(2f), Color.Gray * 0.4f, Main.rand.Next(90), default, (particle) => particle.Velocity.Y -= 0.01f));
			ParticleRenderers.OverPlayers.Add(new CompositeSmoke(muzzlePosition + Main.rand.NextVector2Circular(5f, 5f), velocity * Main.rand.NextFloat(2f), Color.LightYellow * 0.4f, Main.rand.Next(90), default, (particle) => particle.Velocity.Y -= 0.005f)
			{
				Variant = CompositeSmoke.SmallVariant
			});

			for (int x = 0; x < 3; x++)
			{
				ParticleRenderers.OverPlayers.Add(new GlowParticle(muzzlePosition, velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(3f), Color.OrangeRed, 0.4f, 30, 2, p => p.Velocity *= 0.9f));
				Dust.NewDustPerfect(muzzlePosition, DustID.Torch, velocity.RotatedByRandom(0.4f) * Main.rand.NextFloat(2f, 9f), 0, default, Main.rand.NextFloat(1f, 4f)).noGravity = true;
			}
		}

		return false;
	}

	public override ModItem Clone(Item newEntity)
	{
		var newItem = newEntity.ModItem as Flakker;
		newItem._reloadEffects = _reloadEffects;

		return newEntity.ModItem;
	}

	public override void ItemStyle(Player player, Rectangle heldItemFrame, bool inUse)
	{
		if (!ReloadInfo.IsReloading(player))
			return;

		float shootRotation = ReloadInfo.LastRotation;
		float animProgress = 1f - ReloadInfo.reloadTime / (float)Magazine.ReloadDuration;

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

		if (animProgress > 0.15f && !_reloadEffects[0])
		{
			Vector2 pos = itemPosition + new Vector2(14f, -4f * player.direction).RotatedBy(itemRotation);
			Gore.NewGorePerfect(Item.GetSource_FromThis("Spirit Reforged: Flakker Reload"), pos, Vector2.UnitX.RotatedBy(itemRotation) * Main.rand.NextFloat(3f, 5f), Mod.Find<ModGore>("FlakkerMagazineGore").Type, 1f);

			SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing with { Volume = 0.33f }, pos);

			for (int i = 0; i < 3; i++)
				Dust.NewDustPerfect(pos, DustID.Smoke, Main.rand.NextVector2Circular(0.5f, 0.5f), 210, default, Main.rand.NextFloat(4f));

			_reloadEffects[0] = true;
		}

		if (animProgress > 0.6f && !_reloadEffects[1])
		{
			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/Eject") with { Volume = 0.5f }, itemPosition);
			_reloadEffects[1] = true;
		}

		if (animProgress > 0.7f && !_reloadEffects[2])
		{
			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/Subclasses/Shotguns/ShotgunPump") with { Volume = 0.65f }, itemPosition);
			_reloadEffects[2] = true;
		}

		if (animProgress > 0.8f && !_reloadEffects[3])
		{
			Vector2 dustPosition = itemPosition + new Vector2(10f, -12f * player.direction).RotatedBy(itemRotation);

			for (int i = 0; i < 5; i++)
			{
				Dust.NewDustPerfect(dustPosition, DustID.Smoke, -Vector2.UnitX.RotatedByRandom(0.3f).RotatedBy(itemRotation) * Main.rand.NextFloat(2f), 210, default, Main.rand.NextFloat(4f));
				Dust.NewDustPerfect(dustPosition, DustID.Torch, -Vector2.UnitX.RotatedByRandom(0.7f).RotatedBy(itemRotation) * Main.rand.NextFloat(5f), 0, default, Main.rand.NextFloat(4f)).noGravity = true;
			}

			_reloadEffects[3] = true;
		}
	}

	public override bool ModifyItemDraw(ref PlayerDrawSet drawInfo, ref DrawData drawData, ref DrawData? coloredDrawData, ref DrawData? glowMaskDrawData)
	{
		Player player = drawInfo.drawPlayer;
		Vector2 origin = new(8, 18);

		if (ReloadInfo.IsReloading(drawInfo.drawPlayer))
		{
			float progress = 1f - ReloadInfo.reloadTime / (float)Magazine.ReloadDuration;

			Texture2D handle = Handle.Value;
			Texture2D barrel = Barrel.Value;
			Texture2D mag = Mag.Value;
			float barrelOffset;

			if (progress < 0.2f)
				barrelOffset = MathHelper.Lerp(0, 4, EaseFunction.EaseCircularOut.Ease(progress / 0.2f));
			else if (progress < 0.8f)
				barrelOffset = 4;
			else
				barrelOffset = MathHelper.Lerp(4, 0, EaseFunction.EaseOutBack(6).Ease((progress - 0.8f) / 0.2f));

			drawInfo.DrawDataCache.Add(GetDrawHeld(drawData with { texture = handle }, drawInfo.drawPlayer, origin, out float armRotation));
			drawInfo.DrawDataCache.Add(GetDrawHeld(drawData with { texture = barrel }, drawInfo.drawPlayer, origin, out _, offset: new Vector2(barrelOffset * -player.direction, 0).RotatedBy(drawData.rotation)));

			if (progress is < 0.15f or > 0.65f)
			{
				drawInfo.DrawDataCache.Add(GetDrawHeld(drawData with { texture = mag }, drawInfo.drawPlayer, origin, out _));
			}
			else if (progress > 0.3f)
			{
				progress = (progress - 0.3f) / 0.35f;

				float fadeIn = 1f;
				if (progress < 0.5f)
					fadeIn = progress / 0.5f;

				Vector2 offset = new Vector2(-3 * drawInfo.drawPlayer.direction, 7).RotatedBy(drawData.rotation) * EaseFunction.EaseOutBack(4).Ease(1f - progress);
				float rotationOffset = 0.2f * EaseFunction.EaseOutBack(5).Ease(1f - progress) * drawInfo.drawPlayer.direction;

				drawInfo.DrawDataCache.Add(GetDrawHeld(drawData with { texture = mag, color = drawData.color * fadeIn, rotation = drawData.rotation + rotationOffset }, drawInfo.drawPlayer, origin, out float magArmRotation, offset: offset));
				player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, magArmRotation + MathHelper.PiOver4 * player.direction);
			}

			player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
		}
		else if (player.ItemAnimationActive)
		{
			float progress = 1f - player.itemAnimation / (float)player.itemAnimationMax;

			drawInfo.DrawDataCache.Add(GetDrawHeld(drawData, drawInfo.drawPlayer, origin, out float armRotation, new Recoil(EaseFunction.CompoundEase([EaseFunction.EaseCubicOut, EaseFunction.EaseSine]), 3, 1), new Vector2(10 * Math.Min(progress * 4, 1) - 10, 0).RotatedBy(drawData.rotation)));
			player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation + 0.5f * player.direction);
		}

		return false;
	}
}