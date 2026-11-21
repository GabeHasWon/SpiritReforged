using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.ItemCommon.MagazineSystem;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Subclasses.Shotguns;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Forest.Survivalist;
using SpiritReforged.Content.Ocean.Items.Blunderbuss;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underworld.Heirloom;

public class Heirloom : ReloadableItem
{
	private const int MAX_ARM_PULLBACK_TIMER = 15;

	public static readonly SoundStyle ShellLoad = new("SpiritReforged/Assets/SFX/UI/Magazine/ShellLoad");

	public static readonly Asset<Texture2D> Handle = DrawHelpers.RequestLocal<Heirloom>("Heirloom_Handle", false);
	public static readonly Asset<Texture2D> Barrel = DrawHelpers.RequestLocal<Heirloom>("Heirloom_Barrel", false);

	// For reload animation
	// Each reload animation has 4 "keyframes" in which dust, sound effects, etc are played / spawned
	// This array keeps track of them and is reset upon firing
	private bool[] _reloadEffects = new bool[4];

	/// <summary> When greater than zero, it will interpolate from a fully extended arm to a non extended arm, <see cref="MAX_ARM_PULLBACK_TIMER"/> </summary>
	public int armPullbackTimer;
	public IShotgunAmmo lastUsedAmmo;

	public override IReloadable.MagazineInfo Magazine => new(2, 120, false);

	public override InterfaceProfile? Profile => new(MagazinePlayer.StyleID.Shell);

	public override void SetDefaults()
	{
		Item.DefaultToShotgun(55, 1);
		Item.damage = 23;
		Item.knockBack = 10f;
		Item.width = 62;
		Item.height = 22;
		Item.value = Item.buyPrice(0, 3, 50, 0);
		Item.rare = ItemRarityID.Orange;
		Item.UseSound = SoundID.Item36;
	}

	public override bool AltFunctionUse(Player player) => ReloadInfo.capacity == Magazine.FullCapacity; //Only let the player use the alternate use when at full magazine

	public override bool? UseItem(Player player)
	{
		_reloadEffects = new bool[4];
		return null;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (player.altFunctionUse == 2)
		{
			damage = (int)(damage * 0.94f); // 6% decrease in damage
			player.velocity -= velocity * 5f;

			if (Main.myPlayer == player.whoAmI)
				ScreenshakeHelper.Shake(player.Center, -velocity, 4, 2, 10);

			SoundEngine.PlaySound(Blunderbuss.Fire2, position);
			//shotgunStats = new(0, 0, 0, 0.1f, 0, 0.5f);

			for (int i = 0; i < 2; i++)
			{
				float strength = 1f;

				// if the player has more than two shots, add 20% strength per
				if (Magazine.FullCapacity > 2)
					strength += (Magazine.FullCapacity - 2) * 0.20f;

				//base.Shoot(player, source, position, velocity, type, (int)(damage * strength), knockback * strength);
			}

			while (ReloadInfo.capacity > 0)
			{
				player.PickAmmo(Item, out type, out float speed, out damage, out knockback, out _);
				Projectile.NewProjectile(source, position, Vector2.Normalize(velocity) * speed, type, damage, knockback);
			}

			//shotgunStats = new();

			return false;
		}
		else
		{
			StatModifiers modifiers = new(
				StatModifier.Default,
				StatModifier.Default);

			ShotgunGlobalItem.ShotgunShoot(modifiers, source, position, velocity, type, damage, knockback);
		}

		Vector2 barrelPosition = position + new Vector2(42, -8 * player.direction).RotatedBy(velocity.ToRotation());
		if (player.altFunctionUse == 2)
		{
			ParticleRenderers.UnderProjectiles.Add(new TexturedPulseCircle(
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

				ParticleRenderers.UnderProjectiles.Add(new BloomParticle(pos, vel, Color.DarkOrange.Additive() * 0.75f, 0.3f, 45, extraUpdateAction: p => p.Velocity *= 0.95f));
				ParticleRenderers.UnderProjectiles.Add(new GlowParticle(pos, vel, Color.LightYellow.Additive(), 0.25f, 45, extraUpdateAction: p => p.Velocity *= 0.95f));

				Dust.NewDustPerfect(barrelPosition + Main.rand.NextVector2Circular(4, 4), DustID.Torch, velocity.RotatedByRandom(0.6f) * Main.rand.NextFloat(1f, 6f), 0, default, Main.rand.NextFloat(1f, 3.5f)).noGravity = true;
			}
		}

		for (int i = 0; i < 6; i++)
			Dust.NewDustPerfect(barrelPosition + Main.rand.NextVector2Circular(3, 3), DustID.Torch, velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(1f, 5f), 0, default, Main.rand.NextFloat(1f, 2.5f)).noGravity = true;

		for (int i = 0; i < 4; i++)
		{
			ParticleRenderers.UnderProjectiles.Add(new SmokeCloud(barrelPosition + Main.rand.NextVector2Circular(8, 8), velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(3f), Color.Black * 0.4f, Main.rand.NextFloat(0.07f, 0.15f), EaseFunction.EaseQuadOut, 30 + Main.rand.Next(60), true)
			{
				Pixellate = true,
				PixelDivisor = 3
			});

			ParticleRenderers.OverPlayers.Add(new CompositeSmoke(barrelPosition + Main.rand.NextVector2Circular(5, 5), velocity.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.2f, 1f), Color.Black * 0.4f, 50 + Main.rand.Next(30)));
			ParticleRenderers.OverPlayers.Add(new CompositeSmoke(barrelPosition + Main.rand.NextVector2Circular(5, 5), velocity.RotatedByRandom(0.4f) * Main.rand.NextFloat(2f, 5f), new Color(20, 20, 20) * 0.3f, 35 + Main.rand.Next(30), default, SmokeAction)
			{
				Variant = CompositeSmoke.SmallVariant
			});
		}

		if (ContentSamples.ItemsByType[source.AmmoItemIdUsed].ModItem is IShotgunAmmo shotgunAmmo)
			lastUsedAmmo = shotgunAmmo;

		return false;

		static void SmokeAction(Particle p)
		{
			p.Velocity *= 0.97f;
			p.Velocity.Y -= 0.02f;
		}
	}

	public override void ItemStyle(Player player, Rectangle heldItemFrame, bool inUse)
	{
		if (!ReloadInfo.IsReloading(player))
			return;

		float shootRotation = ReloadInfo.LastRotation;
		float animProgress = 1f - ReloadInfo.reloadTime / (float)Magazine.ReloadDuration;

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

		if (animProgress > 0.2f && !_reloadEffects[0])
		{
			Vector2 breakPosition = itemPosition + new Vector2(12f, 2f).RotatedBy(itemRotation);
			Vector2 breakVelocity = -Vector2.UnitY - Vector2.UnitX * player.direction;

			for (int i = 0; i < 3; i++)
			{
				Dust.NewDustPerfect(breakPosition + Main.rand.NextVector2Circular(15, 15), DustID.Torch, breakVelocity * Main.rand.NextFloat(2f) + Main.rand.NextVector2Circular(0.5f, 0.5f), 0, default, Main.rand.NextFloat(3f)).noGravity = true;
				Dust.NewDustPerfect(breakPosition + Main.rand.NextVector2Circular(15, 15), DustID.Torch, breakVelocity * Main.rand.NextFloat(2f) + Main.rand.NextVector2Circular(0.5f, 0.5f), 0, default, Main.rand.NextFloat(2f));

				ParticleRenderers.OverPlayers.Add(new CompositeSmoke(breakPosition + Main.rand.NextVector2Circular(5, 5), breakVelocity + Main.rand.NextVector2Circular(0.25f, 0.25f), Color.Black * 0.5f, 65)
				{
					Variant = CompositeSmoke.SmallVariant
				});

				ParticleRenderers.OverPlayers.Add(new CompositeSmoke(breakPosition + Main.rand.NextVector2Circular(5, 5), breakVelocity + Main.rand.NextVector2Circular(0.25f, 0.25f), Color.DarkGray * 0.5f, 45));
			}

			for (int i = 0; i < 2; i++)
				ParticleRenderers.OverPlayers.Add(new ShotgunShellParticle(breakPosition, -Vector2.UnitY * Main.rand.NextFloat(3f, 6f) - Vector2.UnitX * player.direction * Main.rand.NextFloat(1.5f, 3f), 1f, 120, lastUsedAmmo));

			SoundEngine.PlaySound(Survivalist.BreakOpen, breakPosition);

			_reloadEffects[0] = true;
		}

		if (animProgress > 0.55f && !_reloadEffects[1])
		{
			SoundEngine.PlaySound(ShellLoad with { Volume = 1f });

			armPullbackTimer = MAX_ARM_PULLBACK_TIMER;
			_reloadEffects[1] = true;
		}

		if (animProgress > 0.7f && !_reloadEffects[2])
		{
			SoundEngine.PlaySound(ShellLoad with { Volume = 1f });

			armPullbackTimer = MAX_ARM_PULLBACK_TIMER;
			_reloadEffects[2] = true;
		}

		if (animProgress > 0.9f && !_reloadEffects[3])
		{
			SoundEngine.PlaySound(Survivalist.EmptyMagazine, player.Center);
			_reloadEffects[3] = true;
		}
	}

	public override void ItemFrame(Player player, bool inUse)
	{
		if (!ReloadInfo.IsReloading(player))
			return;

		float shootRotation = ReloadInfo.LastRotation;
		float animProgress = 1f - ReloadInfo.reloadTime / (float)Magazine.ReloadDuration;

		float rotation = shootRotation * player.gravDir - 1.5707964f;
		float frontArmRotation = shootRotation * player.gravDir - 1.5707964f;

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

		if (armPullbackTimer > 0)
		{
			float interpolant = 1f - armPullbackTimer / (float)MAX_ARM_PULLBACK_TIMER;
			int armType = (int)MathHelper.Lerp(0, 3, interpolant);

			// Because why would Player.CompositeArmStretchAmount be in order?
			switch (armType)
			{
				case 0:
					frontStretch = Player.CompositeArmStretchAmount.Full; break;
				case 1:
					frontStretch = Player.CompositeArmStretchAmount.ThreeQuarters; break;
				case 2:
					frontStretch = Player.CompositeArmStretchAmount.Quarter; break;
				case 3:
					frontStretch = Player.CompositeArmStretchAmount.None; break;
			}

			armPullbackTimer--;
		}

		player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, rotation);
		player.SetCompositeArmFront(true, frontStretch, frontArmRotation);
	}

	public override bool ModifyItemDraw(ref PlayerDrawSet drawInfo, ref DrawData drawData, ref DrawData? coloredDrawData, ref DrawData? glowMaskDrawData)
	{
		Player player = drawInfo.drawPlayer;
		Vector2 origin = new(2, 18);

		if (ReloadInfo.IsReloading(drawInfo.drawPlayer))
		{
			Texture2D handle = Handle.Value;
			Texture2D barrel = Barrel.Value;

			float animationProgress = 1f - ReloadInfo.reloadTime / (float)Magazine.ReloadDuration;
			float maxRotation = 1.1f * drawInfo.drawPlayer.direction;
			float barrelRot = drawData.rotation;

			if (animationProgress is > 0.25f and < 0.35f)
			{
				float lerp = EaseFunction.EaseCircularOut.Ease((animationProgress - 0.25f) / 0.1f);
				barrelRot += MathHelper.Lerp(0, maxRotation, lerp);
			}
			else if (animationProgress is >= 0.35f and < 0.75f)
			{
				barrelRot += maxRotation;
			}
			else if (animationProgress >= 0.75f)
			{
				float lerp = EaseFunction.EaseInOutBack().Ease((animationProgress - 0.75f) / 0.25f);
				barrelRot += MathHelper.Lerp(maxRotation, 0, lerp);
			}

			Vector2 centerOrigin = new(24, 14);
			Vector2 offset = new Vector2(22 * player.direction, -4).RotatedBy(drawData.rotation);

			drawInfo.DrawDataCache.Add(GetDrawHeld(drawData with { texture = handle }, player, origin, out _));
			drawInfo.DrawDataCache.Add(GetDrawHeld(drawData with { texture = barrel, rotation = barrelRot }, player, centerOrigin, out _, offset: offset));
		}
		else if (player.ItemAnimationActive)
		{
			float progress = 1f - player.itemAnimation / (float)player.itemAnimationMax;

			drawInfo.DrawDataCache.Add(GetDrawHeld(drawData, player, origin, out float armRotation, new Recoil(EaseFunction.CompoundEase([EaseFunction.EaseCubicOut, EaseFunction.EaseSine]), 3, 1), new Vector2(10 * Math.Min(progress * 4, 1) - 10, 0).RotatedBy(drawData.rotation)));
			player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
		}

		return false;
	}

	public override ModItem Clone(Item newEntity)
	{
		var newItem = newEntity.ModItem as Heirloom;

		newItem._reloadEffects = _reloadEffects;
		newItem.lastUsedAmmo = lastUsedAmmo;
		newItem.armPullbackTimer = armPullbackTimer;

		return newEntity.ModItem;
	}
}