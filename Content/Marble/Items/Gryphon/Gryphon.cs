using SpiritReforged.Common.Subclasses.Shotguns;
using SpiritReforged.Common.Visuals;
using Terraria.DataStructures;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering;
using Terraria.Audio;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;

namespace SpiritReforged.Content.Marble.Items.Gryphon;
// TODO: Obtainment
// This equates to +150% spread, before any charging, works in favor of the weapon though.
public class Gryphon() : ShotgunItem(new(spreadMultiplier: 1.5f))
{
	public override bool CanUseItem(Player player) => player.ownedProjectileCounts[ModContent.ProjectileType<GryphonHoldout>()] <= 0;
	public override void SafeSetDefaults()
	{
		Item.damage = 12;
		Item.knockBack = 6;
		Item.width = 54;
		Item.height = 26;
		Item.useTime = Item.useAnimation = 90;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.noMelee = true;
		Item.noUseGraphic = true;
		Item.channel = true;

		Item.value = Item.buyPrice(0, 1, 50, 0);
		Item.rare = ItemRarityID.Green;
		Item.autoReuse = true;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<GryphonHoldout>(), damage, knockback, player.whoAmI);

		return false;
	}
}

class GryphonHoldout : ModProjectile, IDrawPixelated
{
	public const int MAX_FLASH_TIMER = 20;
	public const int MAX_POINTS = 5;

	public static readonly Asset<Texture2D> BaseTexture = DrawHelpers.RequestLocal<Gryphon>("Gryphon", false);
	public override string Texture => "SpiritReforged/Content/Marble/Items/Gryphon/Gryphon";
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

			Vector2 start = ArmPosition + new Vector2(16f, -8f * Owner.direction).RotatedBy(Projectile.rotation);
			Vector2 end = start + new Vector2(MathHelper.Lerp(8, 32, ChargeProgress), 0).RotatedBy(Projectile.rotation + MathHelper.Lerp(-1, 0, EaseBuilder.EaseQuadInOut.Ease(ChargeProgress)));

			for (int i = 0; i < MAX_POINTS; i++)
			{
				float step = i / (float)MAX_POINTS;

				cache.Add(Vector2.Lerp(start, end, step));
			}

			cache.Add(start);

			List<Vector2> cache2 = [];

			start = ArmPosition + new Vector2(16f, -8f * Owner.direction).RotatedBy(Projectile.rotation);
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
			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/GryphonEmpoweredShot") with { Volume = 0.66f }, Owner.Center);

			ParticleHandler.SpawnParticle(new TexturedPulseCircle(
				muzzlePosition,
				Color.Cyan,
				Color.White.Additive(),
				0.4f,
				120,
				35,
				"supPerlin",
				new Vector2(2, 3),
				EaseFunction.EaseCubicOut).WithSkew(0.9f, Projectile.rotation).UsesLightColor());

			ParticleHandler.SpawnParticle(new TexturedPulseCircle(
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
			ParticleHandler.SpawnParticle(new CompositeSmoke(muzzlePosition + Main.rand.NextVector2Circular(5f, 5f), Projectile.velocity * Main.rand.NextFloat(1.5f * ChargeProgress), new(150, 230, 255), 30 + Main.rand.Next(60), false, false, (particle) => particle.Velocity.Y -= 0.01f, 0.02f));
			
			ParticleHandler.SpawnParticle(new SmallCompositeSmoke(muzzlePosition + Main.rand.NextVector2Circular(5f, 5f), Projectile.velocity * Main.rand.NextFloat(3f * ChargeProgress), Color.Gray, 20 + Main.rand.Next(60), false, false, (particle) => particle.Velocity.Y -= 0.01f, 0.02f));

			ParticleHandler.SpawnParticle(new EmberParticle(muzzlePosition, Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(3f), new Color(200, 160, 0, 0), 0.2f, 30, 2));
		}

		for (int i = 0; i < (int)Math.Max(1, 15 * ChargeProgress); i++)
			Dust.NewDustPerfect(muzzlePosition + Main.rand.NextVector2Circular(10f, 10f), DustID.Electric, Projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(1f, 15f * ChargeProgress), 0, default, Main.rand.NextFloat()).noGravity = true;

		SoundEngine.PlaySound(SoundID.Item36, Owner.Center);

		if (Owner.HeldItem.ModItem is Gryphon gryphon)
		{
			var shotgunStats = gryphon.shotgunStats;

			Item ammoItem = Owner.ChooseAmmo(gryphon.Item);

			if (ammoItem != null && ammoItem.ModItem is ShotgunAmmoItem ammo)
			{
				Vector2 shellPosition = ArmPosition + new Vector2(-16, -8 * Owner.direction).RotatedBy(Projectile.rotation);

				ParticleHandler.SpawnParticle(new ShotgunShellParticle(shellPosition, -Projectile.velocity.RotatedByRandom(0.4f) * Main.rand.NextFloat(2f, 6f) * ChargeProgress - Vector2.UnitY * Main.rand.NextFloat(3f), 1f, 120, ammo));

				for (int i = 0; i < 4; i++)
					Dust.NewDustPerfect(shellPosition + Main.rand.NextVector2Circular(6f, 6f), DustID.Torch, -Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(5f), 50, default, Main.rand.NextFloat(2f));

				Vector2 position = ArmPosition;
				Vector2 direction = Vector2.Zero;
				if (Main.myPlayer == Owner.whoAmI)
					direction = position.DirectionTo(Main.MouseWorld);

				var shotgunPlayer = Owner.GetModPlayer<ShotgunPlayer>();

				List<Projectile> spawnedProjectiles = ammo._behavior.Invoke(gryphon.Item, Owner, new Terraria.DataStructures.EntitySource_ItemUse_WithAmmo(Owner, gryphon.Item, ammoItem.type, "SpiritReforged: Gryphon Shoot"), position, direction,
					shotgunPlayer.ModifyShotCount(ammo._shotCount, shotgunStats._additionalShots, shotgunStats._shotMultiplier),
					shotgunPlayer.ModifySpread(MathHelper.Lerp(ammo._spreadAmount, ammo._spreadAmount * 0.2f, ChargeProgress), shotgunStats._additionalSpread, shotgunStats._spreadMultiplier),
					shotgunPlayer.ModifySpeed(MathHelper.Lerp(ammo._speed, ammo._speed * 1.5f, ChargeProgress), shotgunStats._additionalSpeed, shotgunStats._speedMultiplier),	
					Projectile.damage, Projectile.knockBack);

				if (FullyCharged)
					foreach (Projectile p in spawnedProjectiles)
					{
						p.usesLocalNPCImmunity = true;
						p.localNPCHitCooldown = 20;

						p.GetGlobalProjectile<GryphonGlobalProjectile>().active = true;
					}
			}
		}
	}
	protected void FiredAI()
	{
		float progress = 1f - Projectile.timeLeft / 30f;

		_armOffset = Vector2.Lerp(new(-16f, 0), Vector2.Zero, EaseBuilder.EaseQuinticOut.Ease(progress));
		_rotationOffset = MathHelper.Lerp(-0.5f, 0f, EaseBuilder.EaseOutBack().Ease(progress));
		
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
				SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/ClubReady") with { Pitch = 0.3f}, Owner.Center);

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
		var tex = BaseTexture.Value;
		var texWhite = TextureColorCache.ColorSolid(tex, Color.White);
		var starTexture = AssetLoader.LoadedTextures["Star"].Value;
		var bloomTexture = AssetLoader.LoadedTextures["Bloom"].Value;

		SpriteEffects spriteEffects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		Vector2 position = ArmPosition - Main.screenPosition;

		float rotation = Projectile.rotation + (spriteEffects == SpriteEffects.FlipHorizontally ? MathHelper.Pi : 0f) * Projectile.spriteDirection;

		float fadeIn = 1f;

		if (Timer < 10f)
			fadeIn = Timer / 10f;

		Main.spriteBatch.Draw(tex, position, null, lightColor * fadeIn, rotation, tex.Size() / 2f, Projectile.scale, spriteEffects, 0f);

		if (_flashTimer > 0)
		{
			float progress = _flashTimer / (float)MAX_FLASH_TIMER;
			Main.spriteBatch.Draw(texWhite, position, null, Color.White * progress, rotation, texWhite.Size() / 2f, Projectile.scale, spriteEffects, 0f);
			
			Vector2 starPosition = ArmPosition + new Vector2(16f, -8f * Owner.direction).RotatedBy(Projectile.rotation);
			
			Main.spriteBatch.Draw(bloomTexture, starPosition - Main.screenPosition, null, Color.Cyan.Additive() * progress * 0.33f, 0, bloomTexture.Size() / 2f, Projectile.scale * 0.15f, 0f, 0f);

			Main.spriteBatch.Draw(starTexture, starPosition - Main.screenPosition, null, Color.Cyan.Additive() * progress, 0, starTexture.Size() / 2f, Projectile.scale * 0.1f, 0f, 0f);
			Main.spriteBatch.Draw(starTexture, starPosition - Main.screenPosition, null, Color.White.Additive() * progress, 0, starTexture.Size() / 2f, Projectile.scale * 0.07f, 0f, 0f);
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