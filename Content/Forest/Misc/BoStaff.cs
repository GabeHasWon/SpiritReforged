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
		Item.useTime = Item.useAnimation = 28;
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

	public override float UseSpeedMultiplier(Player player) => (HitCombo == 2) ? 0.38f : 1f;
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
			fullArc += 3;
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

			if (ActivelySpinning)
			{
				float progress = (float)Counter / SwingTime * spinSpeed % 1;
				return (Counter < SwingTime / 2) ? EaseFunction.EaseCircularIn.Ease(progress) : progress;
			}

			return EaseFunction.EaseCircularInOut.Ease(MathHelper.Min((float)Counter / (SwingTime * 0.7f), 1));
		}
	}

	private float SwingTime => Main.player[Projectile.owner].itemTimeMax * (Projectile.extraUpdates + 1); //The full duration of the swing
	public bool SpinMove => Projectile.ai[2] == 1;
	public bool ActivelySpinning => SpinMove && !_released;

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
			float trailDist = Reach * (ActivelySpinning ? 0.7f : 1) - trailWidth / 2;
			float intensity = (i == 0) ? 0.25f : 0.3f;

			SwingTrailParameters parameters = new(SwingArc, rotation, trailDist, trailWidth)
			{
				Color = Color.White,
				SecondaryColor = Color.MediumVioletRed,
				TrailLength = 0.25f,
				Intensity = intensity,
				DissolveThreshold = SpinMove ? 1f : 0.9f
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
		Projectile.scale = 0;
	}

	public override void AI()
	{
		var owner = Main.player[Projectile.owner];
		float progress = (Projectile.direction == -1) ? (1f - Ease) : Ease;
		int direction = (Projectile.velocity.X > 0) ? 1 : -1;

		Projectile.scale = MathHelper.Min(Projectile.scale + 0.05f, _meleeScale);

		if (ActivelySpinning)
		{
			if (!owner.channel)
			{
				_released = true;
				Counter = 0; //Causes ClientSpawn to be called again

				if (!Main.dedServ)
					TrailManager.TryTrailKill(Projectile);
			}

			if (Main.rand.NextBool())
			{
				var dust = Dust.NewDustDirect(GetEnd(20), Projectile.width, Projectile.height, DustID.DynastyShingle_Red, Alpha: Main.rand.Next(100, 150), Scale: 0.75f);
				dust.noGravity = true;
				dust.velocity = -Vector2.UnitY.RotatedBy(Projectile.rotation);
			}

			if (Main.myPlayer == Projectile.owner)
			{
				int newDir = Math.Sign(Main.MouseWorld.X - owner.Center.X);

				if (newDir != direction)
				{
					Projectile.velocity.X *= -1;
					Projectile.netUpdate = true;
				}
			}
		}

		if (!UpdateCollision())
			Projectile.rotation = Projectile.velocity.ToRotation() - SwingArc / 2 + SwingArc * progress;

		Projectile.spriteDirection = Projectile.direction = owner.direction = direction;
		Projectile.Center = ActivelySpinning ? owner.Center : owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, Projectile.rotation - 1.57f);

		owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - 1.57f);
		owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - 1.57f);
		owner.heldProj = Projectile.whoAmI;

		if (Counter == 0)
			ClientSpawn(owner);

		Counter++;

		if (ActivelySpinning || Counter < SwingTime - 1)
			owner.itemAnimation = owner.itemTime = Projectile.timeLeft = 2;

		if (owner.dead)
			Projectile.Kill();
	}

	/// <returns> Whether collision has occurred. </returns>
	private bool UpdateCollision()
	{
		if (SpinMove && _released && !_collided && Counter > 10 && SolidCollision()) //One-time hit effects
		{
			_collided = true;

			TrailManager.TryTrailKill(Projectile);
			Collision.HitTiles(GetEnd(20), Vector2.Zero, Projectile.width, Projectile.height);

			SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact with { Pitch = -0.5f }, GetEnd());

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
		Projectile.localNPCHitCooldown = ActivelySpinning ? 20 : -1;

		if (!ActivelySpinning)
			Projectile.scale = _meleeScale;
	}

	public override void OnKill(int timeLeft)
	{
		if (Main.myPlayer == Projectile.owner)
			BoStaff.HitCombo = (Projectile.numHits == 0 || SpinMove) ? 0 : BoStaff.HitCombo + 1;
	}

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
	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		int lineWidth = 30;
		var endCenter = GetEnd() + Projectile.Size / 2;
		float collisionPoint = 0f;

		return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, endCenter, lineWidth, ref collisionPoint);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		int cutoff = ActivelySpinning ? 0 : 30;

		var texture = TextureAssets.Projectile[Type].Value;
		var frame = new Rectangle(cutoff, 0, texture.Width - cutoff, texture.Height);
		var origin = ActivelySpinning ? frame.Size() / 2 : new Vector2(10, frame.Height / 2);

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
		return false;
	}
}