using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.Subclasses.Shotguns;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Forest.Ammo;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Marble.Items.Gryphon;

// TODO: Obtainment
public class Gryphon : ModItem
{
	public override void SetDefaults()
	{
		Item.DefaultToShotgun(90, 10);
		Item.damage = 19;
		Item.knockBack = 8;
		Item.noUseGraphic = true;
		Item.channel = true;
		Item.value = Item.buyPrice(0, 1, 50, 0);
		Item.rare = ItemRarityID.Green;
	}

	public override bool CanUseItem(Player player) => player.ownedProjectileCounts[ModContent.ProjectileType<GryphonHoldout>()] <= 0;

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<GryphonHoldout>(), damage, knockback, player.whoAmI);
		return false;
	}
}

public class GryphonHoldout : ModProjectile, IDrawPixelated
{
	public const int MAX_FLASH_TIMER = 20;
	public const int MAX_POINTS = 5;

	public static readonly SoundStyle StrongShot = new("SpiritReforged/Assets/SFX/Item/GryphonEmpoweredShot");

	public override string Texture => ModContent.GetInstance<Gryphon>().Texture;

	public bool Fired
	{
		get => Projectile.ai[0] == 1;
		set => Projectile.ai[0] = value ? 1 : 0;
	}

	public int Timer
	{
		get => (int)Projectile.ai[1];
		set => Projectile.ai[1] = value;
	}

	public int ChargeTime
	{
		get => (int)Projectile.ai[2];
		set => Projectile.ai[2] = value;
	}

	public int _flashTimer;
	public bool CanHold => Owner.HeldItem.ModItem is Gryphon && Owner.channel && !Owner.CCed && !Owner.noItems;
	public float ChargeProgress => Timer / (float)ChargeTime;
	public bool FullyCharged => ChargeProgress >= 1f;
	public float _rotationOffset;
	public Vector2 ArmPosition => Owner.RotatedRelativePoint(Owner.MountedCenter, true) + new Vector2(16f, 0f).RotatedBy(Projectile.rotation) + _armOffset.RotatedBy(Projectile.rotation);
	public Vector2 _armOffset;
	public Player Owner => Main.player[Projectile.owner];

	private VertexTrail[] _trails;

	public override bool? CanDamage() => false;
	public override void SetDefaults()
	{
		Projectile.width = 54;
		Projectile.height = 26;
		Projectile.friendly = true;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
	}

	public override void AI()
	{
		if (_flashTimer > 0)
			_flashTimer--;

		if (!CanHold && !Fired && ChargeProgress > 0.33f)
		{
			Shoot();
			
			Fired = true;
			Projectile.timeLeft = 30;
		}

		if (Timer == 0f)
		{
			if (!Main.dedServ && _trails == null)
				CreateTrail();

			if (Main.myPlayer == Projectile.owner)
				Projectile.velocity = Owner.DirectionTo(Main.MouseWorld);

			Projectile.rotation = Projectile.velocity.ToRotation();
			Projectile.netUpdate = true;
			ChargeTime = CombinedHooks.TotalUseTime(Owner.itemTime, Owner, Owner.HeldItem);
		}

		if (!Fired)
			ChargingAI();
		else
			FiredAI();

		if (!Main.dedServ && _trails is not null)
		{
			// Two trails that converge on the same point from opposing starting rotations
			List<Vector2> cache = [];

			Vector2 start = ArmPosition + new Vector2(28f, -2f * Owner.direction).RotatedBy(Projectile.rotation);
			Vector2 end = start + new Vector2(MathHelper.Lerp(8, 32, ChargeProgress), 0).RotatedBy(Projectile.rotation + MathHelper.Lerp(-1, 0, EaseBuilder.EaseQuadInOut.Ease(ChargeProgress)));

			for (int i = 0; i < MAX_POINTS; i++)
			{
				float step = i / (float)MAX_POINTS;

				cache.Add(Vector2.Lerp(start, end, step));
			}

			cache.Add(start);

			List<Vector2> cache2 = [];

			start = ArmPosition + new Vector2(28f, -2f * Owner.direction).RotatedBy(Projectile.rotation);
			end = start + new Vector2(MathHelper.Lerp(8, 32, ChargeProgress), 0).RotatedBy(Projectile.rotation + MathHelper.Lerp(1, 0, EaseBuilder.EaseQuadInOut.Ease(ChargeProgress)));

			for (int i = 0; i < MAX_POINTS; i++)
			{
				float step = i / (float)MAX_POINTS;
				cache2.Add(Vector2.Lerp(start, end, step));
			}

			cache2.Add(start);

			foreach (VertexTrail trail in _trails)
			{
				// Flash the trail white
				if (_flashTimer > 0)
				{
					float progress = _flashTimer / (float)MAX_FLASH_TIMER;
					trail.OverrideColor = new GradientTrail(Color.Lerp(new Color(32, 197, 242, 150), Color.White.Additive(), progress), Color.White.Additive(), EaseFunction.EaseQuarticInOut);
				}
				else if (trail.OverrideColor is not null)
					trail.OverrideColor = null;

				trail.Update();
				trail._points = _trails[0] == trail ? cache : cache2;
			}
		}
	}

	protected void Shoot()
	{
		Vector2 muzzlePosition = ArmPosition + new Vector2(24, 0).RotatedBy(Projectile.rotation);

		if (FullyCharged)
		{
			SoundEngine.PlaySound(StrongShot with { Volume = 0.66f }, Owner.Center);

			ParticleRenderers.UnderProjectiles.Add(new TexturedPulseCircle(
				muzzlePosition,
				Color.Cyan,
				Color.White.Additive(),
				0.4f,
				120,
				35,
				"supPerlin",
				new Vector2(2, 3),
				EaseFunction.EaseCubicOut).WithSkew(0.9f, Projectile.rotation).UsesLightColor());

			ParticleRenderers.UnderProjectiles.Add(new TexturedPulseCircle(
				muzzlePosition + new Vector2(6, 0).RotatedBy(Projectile.rotation),
				Color.Cyan,
				Color.White.Additive(),
				0.4f,
				100,
				30,
				"supPerlin",
				new Vector2(2, 3),
				EaseFunction.EaseCubicOut).WithSkew(0.9f, Projectile.rotation).UsesLightColor());

			Owner.velocity -= Projectile.velocity * 3f;
		}

		for (int i = 0; i < 3; i++)
		{
			ParticleRenderers.UnderProjectiles.Add(new CompositeSmoke(muzzlePosition + Main.rand.NextVector2Circular(5f, 5f), Projectile.velocity * Main.rand.NextFloat(1.5f * ChargeProgress), new Color(150, 230, 255) * 0.4f, 30 + Main.rand.Next(60), default, (particle) => particle.Velocity.Y -= 0.01f));
			ParticleRenderers.UnderProjectiles.Add(new CompositeSmoke(muzzlePosition + Main.rand.NextVector2Circular(5f, 5f), Projectile.velocity * Main.rand.NextFloat(3f * ChargeProgress), Color.Gray * 0.4f, 20 + Main.rand.Next(60), default, (particle) => particle.Velocity.Y -= 0.01f)
			{
				Variant = CompositeSmoke.SmallVariant
			});

			ParticleRenderers.UnderProjectiles.Add(new EmberParticle(muzzlePosition, Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(3f), new Color(200, 160, 0, 0), 0.2f, 30, 2));
		}

		for (int i = 0; i < (int)Math.Max(1, 15 * ChargeProgress); i++)
			Dust.NewDustPerfect(muzzlePosition + Main.rand.NextVector2Circular(10f, 10f), DustID.Electric, Projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(1f, 15f * ChargeProgress), 0, default, Main.rand.NextFloat()).noGravity = true;

		SoundEngine.PlaySound(SoundID.Item36, Owner.Center);

		Item item = Owner.HeldItem;
		StatModifiers modifiers = new(
			new StatModifier(FullyCharged ? 1.2f : 1f, 1f),
			new StatModifier(MathHelper.Lerp(1.5f, 0.3f, ChargeProgress), 1));

		Item ammoItem = Owner.ChooseAmmo(item);
		if (ammoItem != null && ammoItem.ModItem is IShotgunAmmo ammo)
		{
			Vector2 shellPosition = ArmPosition + new Vector2(-16, -8 * Owner.direction).RotatedBy(Projectile.rotation);
			ParticleRenderers.UnderProjectiles.Add(new ShotgunShellParticle(shellPosition, -Projectile.velocity.RotatedByRandom(0.4f) * Main.rand.NextFloat(2f, 6f) * ChargeProgress - Vector2.UnitY * Main.rand.NextFloat(3f), 1f, 120, ammo));

			for (int i = 0; i < 4; i++)
				Dust.NewDustPerfect(shellPosition + Main.rand.NextVector2Circular(6f, 6f), DustID.Torch, -Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(5f), 50, default, Main.rand.NextFloat(2f));

			Vector2 position = ArmPosition;
			Vector2 velocity = position.DirectionTo(Main.MouseWorld) * (FullyCharged ? 12.5f : 10f);
			StatModifiers ammoModifiers = ammo.Modifiers;

			for (int i = 0; i < ammoModifiers.GetShotCount(Owner, modifiers); i++)
			{
				Projectile newProjectile = ammo.OnShoot(item, Owner, new EntitySource_ItemUse_WithAmmo(Owner, item, ammoItem.type), position, velocity.RotatedByRandom(ammoModifiers.GetShotSpread(Owner, modifiers)), ammoItem.shoot, Projectile.damage, Projectile.knockBack, i);
				if (FullyCharged)
				{
					newProjectile.usesLocalNPCImmunity = true;
					newProjectile.localNPCHitCooldown = 20;

					newProjectile.GetGlobalProjectile<GryphonGlobalProjectile>().active = true;
				}
			}
		}
	}

	protected void FiredAI()
	{
		float progress = 1f - Projectile.timeLeft / 30f;

		_armOffset = Vector2.Lerp(new(-16f, 0), Vector2.Zero, EaseFunction.EaseQuinticOut.Ease(progress));
		_rotationOffset = MathHelper.Lerp(-0.5f, 0f, EaseFunction.EaseOutBack().Ease(progress));
		
		UpdateHeldProjectile(false, false);
	}

	protected void ChargingAI()
	{
		UpdateHeldProjectile();

		if (Timer < ChargeTime)
		{
			Timer++;

			if (Timer == ChargeTime)
			{
				SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/ClubReady") with { Pitch = 0.3f }, Owner.Center); //CACHE ME
				_flashTimer = MAX_FLASH_TIMER;
			}
		}		
	}

	protected void UpdateHeldProjectile(bool updateTimeleft = true, bool updateVelocity = true)
	{
		Owner.ChangeDir(Projectile.direction);
		Owner.heldProj = Projectile.whoAmI;

		Owner.itemTime = 2;
		Owner.itemAnimation = 2;

		if (updateTimeleft)
			Projectile.timeLeft = 2;

		Projectile.rotation = Projectile.velocity.ToRotation() + _rotationOffset * Owner.direction;
		Owner.itemRotation = Utils.ToRotation(Projectile.velocity * Projectile.direction);

		Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.ToRadians(90f));

		Projectile.position = ArmPosition - Projectile.Size * 0.5f;

		if (Main.myPlayer == Projectile.owner && updateVelocity)
		{
			Vector2 oldVelocity = Projectile.velocity;

			Projectile.velocity = Vector2.Lerp(Projectile.velocity, Owner.DirectionTo(Main.MouseWorld), 0.15f);

			if (Projectile.velocity != oldVelocity)
			{
				Projectile.netSpam = 0;
				Projectile.netUpdate = true;
			}
		}

		Projectile.spriteDirection = Projectile.direction;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D tex = TextureAssets.Projectile[Type].Value;
		Texture2D texWhite = TextureColorCache.ColorSolid(tex, Color.White);
		Texture2D starTexture = AssetLoader.LoadedTextures["Star"].Value;
		Texture2D bloomTexture = AssetLoader.LoadedTextures["Bloom"].Value;

		SpriteEffects spriteEffects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
		Vector2 position = ArmPosition - Main.screenPosition;

		float rotation = Projectile.rotation + (spriteEffects == SpriteEffects.FlipHorizontally ? MathHelper.Pi : 0f) * Projectile.spriteDirection;
		float fadeIn = 1f;

		if (Timer < 10f)
			fadeIn = Timer / 10f;

		Main.EntitySpriteDraw(tex, position, null, lightColor * fadeIn, rotation, tex.Size() / 2f, Projectile.scale, spriteEffects);

		if (_flashTimer > 0)
		{
			float progress = _flashTimer / (float)MAX_FLASH_TIMER;
			Main.EntitySpriteDraw(texWhite, position, null, Color.White * progress, rotation, texWhite.Size() / 2f, Projectile.scale, spriteEffects);
			
			Vector2 starPosition = ArmPosition + new Vector2(16f, -8f * Owner.direction).RotatedBy(Projectile.rotation);

			Main.EntitySpriteDraw(bloomTexture, starPosition - Main.screenPosition, null, Color.Cyan.Additive() * progress * 0.33f, 0, bloomTexture.Size() / 2f, Projectile.scale * 0.15f, 0f);

			Main.EntitySpriteDraw(starTexture, starPosition - Main.screenPosition, null, Color.Cyan.Additive() * progress, 0, starTexture.Size() / 2f, Projectile.scale * 0.1f, 0f);
			Main.EntitySpriteDraw(starTexture, starPosition - Main.screenPosition, null, Color.White.Additive() * progress, 0, starTexture.Size() / 2f, Projectile.scale * 0.07f, 0f);
		}

		return false;
	}

	public void DrawPixelated(SpriteBatch sb)
	{
		if (_trails != null)
			foreach (VertexTrail trail in _trails)
			{
				trail.Opacity = 1f;

				if (Fired)
				{
					trail.Opacity = Projectile.timeLeft / 30f;
					trail.WidthMultiplier = Projectile.timeLeft / 30f;
				}

				trail?.Draw(TrailSystem.TrailShaders, sb.GraphicsDevice, Matrix.Identity);
			}
	}

	private void CreateTrail()
	{
		ITrailCap tCap = new RoundCap();
		ITrailPosition tPos = new EntityTrailPosition(Projectile);
		ITrailShader tShader = new ImageShader(AssetLoader.LoadedTextures["GlowTrail"].Value, Vector2.One);

		_trails =
		[
			new VertexTrail(new GradientTrail(new Color(32, 197, 242, 150), Color.White.Additive(), EaseFunction.EaseQuarticInOut), tCap, tPos, tShader, 6, MAX_POINTS, trailWidthFunction: factor => 4),
			new VertexTrail(new GradientTrail(new Color(32, 197, 242, 150), Color.White.Additive(), EaseFunction.EaseQuarticInOut), tCap, tPos, tShader, 6, MAX_POINTS, trailWidthFunction: factor => 4),
		];
	}
}