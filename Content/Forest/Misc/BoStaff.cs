using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.CustomTrails;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Misc;

public class BoStaff : ModItem
{
	private float _swingArc;
	/// <summary> Hit combo counter used by Bo for the owning client only. </summary>
	internal static int HitCombo;

	public override void SetDefaults()
	{
		Item.damage = 10;
		Item.knockBack = 6;
		Item.useTime = Item.useAnimation = 25;
		Item.DamageType = DamageClass.Melee;
		Item.width = Item.height = 46;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.value = Item.sellPrice(silver: 3);
		Item.rare = ItemRarityID.White;
		Item.UseSound = SoundID.Item1;
		Item.shoot = ModContent.ProjectileType<BoStaffSwing>();
		Item.shootSpeed = 1f;
		Item.autoReuse = true;
		Item.useTurn = true;
		Item.channel = true;
		Item.noUseGraphic = true;
		Item.noMelee = true;
	}

	public override float UseSpeedMultiplier(Player player) => (HitCombo == 2) ? 0.5f : 1f;
	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		float fullArc = 4f;
		bool startSpin = false;

		if (HitCombo == 3) //Start a spin
		{
			startSpin = true;
			fullArc = MathHelper.TwoPi;
		}
		else if (HitCombo == 2)
		{
			fullArc += 2;
		}

		_swingArc = (_swingArc == fullArc) ? -fullArc : fullArc;
		Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<BoStaffSwing>(), damage, knockback, player.whoAmI, _swingArc, 0, startSpin ? 1 : 0);
		return false;
	}
}

public class BoStaffSwing : ModProjectile, IManualTrailProjectile
{
	/// <summary> Damage distance in pixels. </summary>
	private const int Reach = 70;

	private float Ease
	{
		get
		{
			const float spinSpeed = 2f;

			if (Spinning && !_released)
				return (float)Counter / SwingTime * spinSpeed % 1;

			float min = MathHelper.Min((float)Counter / (SwingTime * 0.7f), 1);
			return EaseFunction.EaseCircularInOut.Ease(min);
		}
	}

	private float SwingTime => Main.player[Projectile.owner].itemTimeMax * (Projectile.extraUpdates + 1); //The full duration of the swing
	public bool Spinning => Projectile.ai[2] == 1;

	public ref float SwingArc => ref Projectile.ai[0]; //The full arc of the swing in radians
	public ref float Counter => ref Projectile.ai[1];

	public override LocalizedText DisplayName => ModContent.GetInstance<BoStaff>().DisplayName;

	/// <summary> Whether channel has been released. Only used while spinning. </summary>
	private bool _released;
	private bool _collided;
	private float _meleeScale = 1f;

	public Vector2 GetEnd(int subtract = 0, float rotate = 0) => Projectile.position + (Vector2.UnitX * (Reach - subtract)).RotatedBy(Projectile.rotation - rotate * Projectile.direction);

	public void DoTrailCreation(TrailManager tM)
	{
		float rotation = Projectile.velocity.ToRotation();
		if (Projectile.direction == -1)
		{
			rotation = -rotation;
			rotation += MathHelper.Pi;
		}

		for (int i = 0; i < 2; i++)
		{
			float trailWidth = (i == 0) ? 80 : 30;
			float trailDist = Reach - trailWidth / 2;
			float intensity = (i == 0) ? 0.25f : 0.3f;

			SwingTrailParameters parameters = new(SwingArc, rotation, trailDist, trailWidth)
			{
				Color = Color.White,
				SecondaryColor = Color.LightGray,
				TrailLength = 0.25f,
				Intensity = intensity,
				DissolveThreshold = Spinning ? 1f : 0.9f
			};

			tM.CreateCustomTrail(new SwingTrail(Projectile, parameters, p => Ease, SwingTrail.BasicSwingShaderParams));
		}
	}

	public override void SetDefaults()
	{
		Projectile.Size = new Vector2(16);
		Projectile.DamageType = DamageClass.Melee;
		Projectile.friendly = true;
		Projectile.ignoreWater = true;
		Projectile.tileCollide = false;
		Projectile.penetrate = -1;
		Projectile.timeLeft = 2;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
		Projectile.extraUpdates = 1;
	}

	public override void AI()
	{
		var owner = Main.player[Projectile.owner];
		float progress = (Projectile.direction == -1) ? (1f - Ease) : Ease;
		bool activelySpinning = Spinning && !_released;

		if (activelySpinning)
		{
			if (!owner.channel)
			{
				_released = true;
				Counter = 0;
			}

			if (Main.rand.NextBool())
				Dust.NewDustDirect(GetEnd(20), Projectile.width, Projectile.height, DustID.WoodFurniture, Alpha: Main.rand.Next(140, 200)).noGravity = true;
		}

		if (!UpdateCollision())
			Projectile.rotation = Projectile.velocity.ToRotation() - SwingArc / 2 + SwingArc * progress;

		Projectile.spriteDirection = Projectile.direction = owner.direction = (Projectile.velocity.X > 0) ? 1 : -1;
		Projectile.Center = activelySpinning ? owner.Center : owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, Projectile.rotation - 1.57f);

		owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - 1.57f);
		owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - 1.57f);
		owner.heldProj = Projectile.whoAmI;

		if (Counter == 0)
			ClientSpawn(owner);

		Counter++;

		if (activelySpinning || Counter < SwingTime - 2)
			owner.itemAnimation = owner.itemTime = Projectile.timeLeft = 2;

		if (owner.dead)
			Projectile.Kill();
	}

	/// <returns> Whether collision has occurred. </returns>
	private bool UpdateCollision()
	{
		if (Spinning && _released && !_collided && Counter > 10 && SolidCollision()) //One-time hit effects
		{
			_collided = true;

			TrailManager.TryTrailKill(Projectile);
			Collision.HitTiles(GetEnd(20), Vector2.Zero, Projectile.width, Projectile.height);

			SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact with { Pitch = -0.25f }, GetEnd());

			for (int i = 0; i < 12; i++)
				Dust.NewDustPerfect(GetEnd(20), DustID.t_LivingWood, (Vector2.UnitY * -Main.rand.NextFloat(1f, 3f)).RotatedByRandom(1f), Scale: Main.rand.NextFloat(0.9f, 1.1f)).noGravity = Main.rand.NextBool();
		}

		if (_collided)
		{
			float retreat = SolidCollision() ? 0.05f : 0.005f;
			Projectile.rotation -= retreat * Projectile.direction;
		}

		return _collided;

		bool SolidCollision() => Collision.SolidCollision(GetEnd(20, 0.25f), Projectile.width, Projectile.height);
	}

	/// <summary> Like <see cref="ModProjectile.OnSpawn"/>, but called everywhere. </summary>
	private void ClientSpawn(Player owner)
	{
		if (!Main.dedServ)
		{
			TrailManager.ManualTrailSpawn(Projectile);
		}

		_meleeScale = owner.GetAdjustedItemScale(owner.HeldItem);
		Projectile.scale = _meleeScale;

		if (Spinning)
		{
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 10;
		}
	}

	public override void OnKill(int timeLeft) => BoStaff.HitCombo = (Projectile.numHits == 0 || Spinning) ? 0 : BoStaff.HitCombo + 1;
	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.HitDirectionOverride = (target.Center.X - Projectile.Center.X < 0) ? -1 : 1;

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		var position = target.getRect().ClosestPointInRect(GetEnd());
		
		for (int i = 0; i < 5; i++)
		{
			var velocity = (Projectile.DirectionTo(position) * Main.rand.NextFloat(5f, 10f)).RotatedByRandom(1.5f);

			ParticleHandler.SpawnParticle(new ImpactLine(position, velocity, Color.White * 0.25f, new Vector2(0.5f, 0.6f) * Main.rand.NextFloat(0.5f, 1.5f),
				Main.rand.Next(15, 20), 0.8f) { UseLightColor = true, NoLight = true });
		}

		SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing with { Pitch = 0.25f }, position);
	}

	public override bool? CanCutTiles() => _collided ? false : null;
	public override bool? CanDamage() => _collided ? false : null;

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		int lineWidth = 30;
		var endCenter = GetEnd() + Projectile.Size / 2;
		float collisionPoint = 0f;

		return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, endCenter, lineWidth, ref collisionPoint);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		bool activelySpinning = Spinning && !_released;
		int cutoff = activelySpinning ? 0 : 30;

		var texture = TextureAssets.Projectile[Type].Value;
		var frame = new Rectangle(cutoff, 0, texture.Width - cutoff, texture.Height);
		var origin = activelySpinning ? frame.Size() / 2 : new Vector2(10, frame.Height / 2);

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
		return false;
	}
}