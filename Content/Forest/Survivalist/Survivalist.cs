using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.ItemCommon.MagazineSystem;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Subclasses.Shotguns;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Survivalist;

public class Survivalist : ReloadableItem
{
	public const int MAX_COOLDOWN = 300;

	public static readonly SoundStyle GrenadeShot = new("SpiritReforged/Assets/SFX/Item/Subclasses/Shotguns/SurvivalistGrenadeShot", 2);
	public static readonly SoundStyle Pump = new("SpiritReforged/Assets/SFX/Item/Subclasses/Shotguns/ShotgunPump");
	public static readonly SoundStyle BreakOpen = new("SpiritReforged/Assets/SFX/Item/ShotgunOpen");
	public static readonly SoundStyle EmptyMagazine = new("SpiritReforged/Assets/SFX/Item/EmptyMagazine");
	public static readonly SoundStyle Eject = new("SpiritReforged/Assets/SFX/Item/Eject");

	public static readonly Asset<Texture2D> Handle = DrawHelpers.RequestLocal<Survivalist>("Survivalist_Handle", false);
	public static readonly Asset<Texture2D> Barrel = DrawHelpers.RequestLocal<Survivalist>("Survivalist_Barrel", false);
	public static readonly Asset<Texture2D> Underbarrel = DrawHelpers.RequestLocal<Survivalist>("Survivalist_Underbarrel", false);

	public IShotgunAmmo lastUsedAmmo;

	private bool[] _reloadEffects;
	private bool _spawnedEffects = false;
	private int _cooldown;

	public override IReloadable.MagazineInfo Magazine => new(5, 150, false);

	public override InterfaceProfile? Profile => new(MagazinePlayer.StyleID.Shell);

	public override void SetDefaults()
	{
		Item.DefaultToShotgun(65, 1);
		Item.damage = 19;
		Item.knockBack = 5f;
		Item.width = 66;
		Item.height = 24;
		Item.value = Item.buyPrice(0, 3, 50, 0);
		Item.rare = ItemRarityID.Orange;
		Item.UseSound = new SoundStyle("SpiritReforged/Assets/SFX/Item/Subclasses/Shotguns/ShotgunShoot", 3) with { Volume = 0.66f, PitchVariance = 0.1f };
	}

	public override bool AltFunctionUse(Player player) => _cooldown <= 0 && !ReloadInfo.IsReloading(player);

	public override void OnReload(Item item, Player player, IReloadable.ReloadContext context)
	{
		if (context == IReloadable.ReloadContext.Ended)
		{
			if (++ReloadInfo.capacity < Magazine.FullCapacity) //Reload one shot at a time
			{
				ReloadInfo.StartReload(player, item);

				_reloadEffects = new bool[5];
				_spawnedEffects = false;
			}
		}
	}

	public override bool? UseItem(Player player)
	{
		_reloadEffects = new bool[5];
		_spawnedEffects = false;

		return null;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (player.altFunctionUse == 2)
		{
			_cooldown = MAX_COOLDOWN;

			Vector2 underbarrelPosition = position + new Vector2(46, 2 * player.direction).RotatedBy(velocity.ToRotation());
			
			for (int i = 0; i < 8; i++)
			{
				ParticleRenderers.UnderProjectiles.Add(new SmokeCloud(underbarrelPosition, velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(3f), Color.DarkGray * 0.3f, 0.05f, EaseFunction.EaseQuadOut, 60)
				{
					Pixellate = true,
					PixelDivisor = 3,
				});

				Dust.NewDustPerfect(underbarrelPosition, DustID.Torch, velocity.RotatedByRandom(0.2f) * Main.rand.NextFloat(6f), 0, default, Main.rand.NextFloat(1f, 4f)).noGravity = true;
			}

			ParticleRenderers.UnderProjectiles.Add(new TexturedPulseCircle(
				underbarrelPosition,
				Color.DarkOrange.Additive(),
				Color.Orange.Additive(),
				0.5f,
				80,
				35,
				"swirlNoise",
				new Vector2(2, 3),
				EaseFunction.EaseCubicOut).WithSkew(0.9f, velocity.ToRotation()).UsesLightColor());

			SoundEngine.PlaySound(GrenadeShot with { Volume = 0.5f, PitchVariance = 0.1f }, position);
			Projectile.NewProjectile(source, position, velocity * 11f, ModContent.ProjectileType<SurvivalistGrenadeProjectile>(), 30 + damage / 3, knockback * 2, player.whoAmI);
		}
		else
		{
			StatModifiers modifiers = new(
				StatModifier.Default,
				new StatModifier(0.33f, 1));

			ShotgunGlobalItem.ShotgunShoot(modifiers, source, position, velocity, type, damage, knockback);
			Vector2 muzzlePosition = position + new Vector2(56, -2 * player.direction).RotatedBy(velocity.ToRotation());

			for (int i = 0; i < 5; i++)
			{
				ParticleRenderers.UnderProjectiles.Add(new SmokeCloud(muzzlePosition, velocity.RotatedByRandom(0.1f) * Main.rand.NextFloat(2f), Color.LightGray * 0.2f, 0.15f, EaseFunction.EaseQuadOut, 90)
				{
					Pixellate = true,
					PixelDivisor = 2,
				});

				ParticleRenderers.UnderProjectiles.Add(new SmokeCloud(muzzlePosition, velocity.RotatedByRandom(0.1f) * Main.rand.NextFloat(5f), Color.DimGray * 0.3f, 0.1f, EaseFunction.EaseQuadOut, 60)
				{
					Pixellate = true,
					PixelDivisor = 2,
				});

				ParticleRenderers.UnderProjectiles.Add(new CompositeSmoke(muzzlePosition + Main.rand.NextVector2Circular(5f, 5f), velocity * Main.rand.NextFloat(2f), Color.LightGray * 0.4f, Main.rand.Next(90), default, (particle) => particle.Velocity.Y -= 0.01f));
				ParticleRenderers.UnderProjectiles.Add(new CompositeSmoke(muzzlePosition + Main.rand.NextVector2Circular(5f, 5f), velocity * Main.rand.NextFloat(2f), Color.Gray * 0.4f, Main.rand.Next(90), default, (particle) => particle.Velocity.Y -= 0.005f)
				{
					Variant = CompositeSmoke.SmallVariant
				});
			ParticleRenderers.UnderProjectiles.Add(new CompositeSmoke(muzzlePosition + Main.rand.NextVector2Circular(5f, 5f), velocity * Main.rand.NextFloat(2f), Color.LightGray * 0.4f, 10 + Main.rand.Next(90), default, (particle) => particle.Velocity.Y -= 0.01f));
			ParticleRenderers.UnderProjectiles.Add(new CompositeSmoke(muzzlePosition + Main.rand.NextVector2Circular(5f, 5f), velocity * Main.rand.NextFloat(2f), Color.Gray * 0.4f, 10 + Main.rand.Next(90), default, (particle) => particle.Velocity.Y -= 0.005f)
			{
				Variant = CompositeSmoke.SmallVariant
			});

				for (int x = 0; x < 2; x++)
				{
					ParticleRenderers.UnderProjectiles.Add(new GlowParticle(muzzlePosition, velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(3f), Color.DarkOrange, 0.4f, 30, 2, p => p.Velocity *= 0.9f));
					ParticleRenderers.UnderProjectiles.Add(new GlowParticle(muzzlePosition, Main.rand.NextVector2Circular(3, 3), Color.Orange, 0.2f, 40, 2, p => p.Velocity *= 0.9f));

					Dust.NewDustPerfect(muzzlePosition, DustID.Torch, Main.rand.NextVector2Circular(4, 4), 0, default, Main.rand.NextFloat(1f, 3f)).noGravity = true;
				}
			}

			if (ContentSamples.ItemsByType[source.AmmoItemIdUsed].ModItem is IShotgunAmmo shotgunAmmo)
				lastUsedAmmo = shotgunAmmo;
		}

		return false;
	}

	public override void UpdateInventory(Player player)
	{
		if (_cooldown > 0 && --_cooldown == 0)
			SoundEngine.PlaySound(SoundID.MaxMana, player.Center);
	}

	public override ModItem Clone(Item newEntity)
	{
		var newItem = newEntity.ModItem as Survivalist;

		newItem._spawnedEffects = _spawnedEffects;
		newItem._reloadEffects = _reloadEffects;

		return newEntity.ModItem;
	}

	public override bool ModifyItemDraw(ref PlayerDrawSet drawInfo, ref DrawData drawData, ref DrawData? coloredDrawData, ref DrawData? glowMaskDrawData)
	{
		Player player = drawInfo.drawPlayer;
		Vector2 origin = new(8, 18);

		Texture2D handle = Handle.Value;
		Texture2D barrel = Barrel.Value;
		Texture2D underBarrel = Underbarrel.Value;

		float maxRotation = 0.6f * player.direction;
		float barrelRot = drawData.rotation;
		float underBarrelOffset = 0f;

		if (ReloadInfo.IsReloading(player))
		{
			float progress = 1f - ReloadInfo.reloadTime / (float)Magazine.ReloadDuration;
			if (progress > 0.1f)
			{
				float lerper = (progress - 0.1f) / 0.9f;
				if (lerper is > 0.1f)
				{
					if (lerper < 0.25f)
					{
						float interpolant = (lerper - 0.1f) / 0.15f;
						barrelRot += MathHelper.Lerp(0, maxRotation, EaseFunction.EaseOutBack(2).Ease(interpolant));
					}
					else if (lerper < 0.6f)
					{
						barrelRot += maxRotation;
					}
					else if (lerper < 0.8f)
					{
						float interpolant = (lerper - 0.6f) / 0.2f;
						barrelRot += MathHelper.Lerp(maxRotation, 0, EaseFunction.EaseInOutBack(2.3f).Ease(interpolant));
					}
					else if (lerper < 0.9f)
					{
						float lerp = (lerper - 0.8f) / 0.1f;
						underBarrelOffset = MathHelper.Lerp(0f, -6f, EaseFunction.EaseCircularOut.Ease(lerp));
					}
					else
					{
						float lerp = (lerper - 0.9f) / 0.1f;
						underBarrelOffset = MathHelper.Lerp(-6f, 0f, EaseFunction.EaseCircularInOut.Ease(lerp));
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
				underBarrelOffset = MathHelper.Lerp(0f, -6f, EaseFunction.EaseCircularOut.Ease(lerp));
			}
			else if (progress >= 0.7f)
			{
				float lerp = (progress - 0.7f) / 0.3f;
				underBarrelOffset = MathHelper.Lerp(-6f, 0, EaseFunction.EaseCircularInOut.Ease(lerp));
			}
		}

		Vector2 centerOrigin = new(barrel.Width / 2, barrel.Height);

		drawInfo.DrawDataCache.Add(GetDrawHeld(drawData with { texture = handle }, player, origin, out _));
		drawInfo.DrawDataCache.Add(GetDrawHeld(drawData with { texture = barrel, rotation = barrelRot }, player, centerOrigin, out _, offset: new Vector2(25 * player.direction, 6).RotatedBy(drawData.rotation)));
		drawInfo.DrawDataCache.Add(GetDrawHeld(drawData with { texture = underBarrel, rotation = barrelRot }, player, centerOrigin, out _, offset: new Vector2((25 + underBarrelOffset) * player.direction, 6).RotatedBy(drawData.rotation)));

		return false;
	}

	public override void ItemStyle(Player player, Rectangle heldItemFrame, bool inUse)
	{
		if (inUse)
		{
			int shootDirection = ReloadInfo.LastDirection;

			float animProgress = 1f - player.itemAnimation / (float)player.itemAnimationMax;

			if (Main.myPlayer == player.whoAmI)
				player.direction = shootDirection;

			float itemRotation = player.compositeFrontArm.rotation + 1.5707964f * player.gravDir;
			Vector2 itemPosition = player.MountedCenter;

			if (animProgress < 0.05f)
			{
				float lerper = Math.Clamp(animProgress / 0.25f, 0, 1);
				itemPosition += itemRotation.ToRotationVector2() * MathHelper.Lerp(0f, -14f, EaseFunction.EaseCircularOut.Ease(lerper));
			}
			else if (player.altFunctionUse != 2)
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
					if (!_spawnedEffects)
					{
						_spawnedEffects = true;

						Vector2 breakPosition = itemPosition + new Vector2(12f, 2f).RotatedBy(itemRotation);
						Vector2 breakVelocity = -Vector2.UnitY * 0.5f - Vector2.UnitX * player.direction;

						for (int i = 0; i < 3; i++)
						{
							Dust.NewDustPerfect(breakPosition + Main.rand.NextVector2Circular(15, 15), DustID.Torch, breakVelocity * Main.rand.NextFloat(2f) + Main.rand.NextVector2Circular(0.5f, 0.5f), 0, default, Main.rand.NextFloat(3f)).noGravity = true;
							Dust.NewDustPerfect(breakPosition + Main.rand.NextVector2Circular(15, 15), DustID.Torch, breakVelocity * Main.rand.NextFloat(2f) + Main.rand.NextVector2Circular(0.5f, 0.5f), 0, default, Main.rand.NextFloat(2f));

							ParticleRenderers.OverPlayers.Add(new CompositeSmoke(breakPosition + Main.rand.NextVector2Circular(5, 5), breakVelocity + Main.rand.NextVector2Circular(0.25f, 0.25f), Color.AntiqueWhite * 0.36f, 65)
							{
								Variant = CompositeSmoke.SmallVariant
							});

							ParticleRenderers.OverPlayers.Add(new CompositeSmoke(breakPosition + Main.rand.NextVector2Circular(5, 5), breakVelocity + Main.rand.NextVector2Circular(0.25f, 0.25f), Color.Gray * 0.25f, 45));
						}

						ParticleRenderers.OverPlayers.Add(new ShotgunShellParticle(breakPosition, -Vector2.UnitY * Main.rand.NextFloat(1f, 3f) - Vector2.UnitX * player.direction * Main.rand.NextFloat(2f, 3.5f), 1f, 120, lastUsedAmmo));
						SoundEngine.PlaySound(Pump with { Volume = 0.75f }, breakPosition); //CACHE
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

			ItemVisualHelpers.CleanHoldStyle(player, itemRotation, itemPosition, new(66, 24), new(-18, -2), true, false, true);
		}
		else if (ReloadInfo.IsReloading(player))
		{
			float animProgress = 1f - ReloadInfo.reloadTime / (float)Magazine.ReloadDuration;
			float itemRotation = player.compositeBackArm.rotation - 1.5707964f * player.gravDir;
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

					if (!_reloadEffects[0])
					{
						Vector2 breakPosition = itemPosition + new Vector2(20f, -2f).RotatedBy(itemRotation);
						Vector2 breakVelocity = -Vector2.UnitY - Vector2.UnitX * player.direction;

						for (int i = 0; i < 3; i++)
						{
							Dust.NewDustPerfect(breakPosition + Main.rand.NextVector2Circular(15, 15), DustID.Torch, breakVelocity + Main.rand.NextVector2Circular(0.5f, 0.5f), 0, default, Main.rand.NextFloat(3f)).noGravity = true;
							Dust.NewDustPerfect(breakPosition + Main.rand.NextVector2Circular(15, 15), DustID.Smoke, breakVelocity + Main.rand.NextVector2Circular(0.5f, 0.5f), 120, default, Main.rand.NextFloat(4f)).noGravity = true;

							ParticleRenderers.OverPlayers.Add(new CompositeSmoke(breakPosition + Main.rand.NextVector2Circular(5, 5), breakVelocity + Main.rand.NextVector2Circular(0.25f, 0.25f), Color.AntiqueWhite * 0.5f, 45)
							{
								Variant = CompositeSmoke.SmallVariant
							});
						}

						SoundEngine.PlaySound(BreakOpen, breakPosition); //CACHE ME

						_reloadEffects[0] = true;
					}
				}
				else if (lerper < 0.6f)
					itemPosition += itemRotation.ToRotationVector2() * front;
				else if (lerper < 0.8f)
				{
					float interpolant = (lerper - 0.6f) / 0.2f;
					itemPosition += itemRotation.ToRotationVector2() * MathHelper.Lerp(front, 0f, EaseFunction.EaseInOutBack().Ease(interpolant));

					if (lerper > 0.77f && !_reloadEffects[1])
					{
						SoundEngine.PlaySound(EmptyMagazine, player.Center); //CACHE ME
						_reloadEffects[1] = true;
					}
				}
				else if (lerper < 0.9f)
				{
					float interpolant = (lerper - 0.8f) / 0.1f;
					itemPosition += itemRotation.ToRotationVector2() * MathHelper.Lerp(0, back * 1.5f, EaseFunction.EaseCircularOut.Ease(interpolant));

					if (!_reloadEffects[2])
					{
						SoundEngine.PlaySound(Pump with { Volume = 0.75f }, player.Center); //CACHE
						_reloadEffects[2] = true;
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

			ItemVisualHelpers.CleanHoldStyle(player, itemRotation, itemPosition, new(66, 24), new(-18, -2), true, false, true);
		}
	}

	public override void ItemFrame(Player player, bool inUse)
	{
		float shootRotation = ReloadInfo.LastRotation;
		if (inUse)
		{
			int shootDirection = ReloadInfo.LastDirection;

			if (Main.myPlayer == player.whoAmI)
				player.direction = shootDirection;

			float animProgress = 1f - player.itemTime / (float)player.itemTimeMax;
			float rotation = shootRotation * player.gravDir - 1.5707964f;

			if (animProgress < 0.05f)
			{
				float lerper = Math.Clamp(animProgress / 0.25f, 0, 1);
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
		else if (ReloadInfo.IsReloading(player))
		{
			float animProgress = 1f - ReloadInfo.reloadTime / (float)Magazine.ReloadDuration;
			float rotation = shootRotation * player.gravDir - 1.5707964f;
			float frontArmRotation = shootRotation * player.gravDir - 1.5707964f;

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
	}
}
