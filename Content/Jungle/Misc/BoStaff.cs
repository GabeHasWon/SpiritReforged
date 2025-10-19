using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Desert.Tiles;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Jungle.Misc;

public class BoStaff : ModItem, SwordStand.ISwordStandTexture
{
	private float _swingArc;
	/// <summary> Hit combo counter used by Bo for the owning client only. </summary>
	internal static int HitCombo;

	public Asset<Texture2D> StandTexture => DrawHelpers.RequestLocal(GetType(), "BoStaffSwing", false);

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

	public override float UseSpeedMultiplier(Player player)
	{
		if (player.altFunctionUse == 2)
			return 1.8f;

		return HitCombo == 2 ? 0.55f : 1f;
	}

	public override bool AltFunctionUse(Player player) => false; //Makes the alt attack inaccessible

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (player.altFunctionUse == 2)
		{
			Projectile.NewProjectile(source, position, velocity.RotatedByRandom(0.1f), type, (int)(damage * 0.3f), knockback * 2f, player.whoAmI, 0, 0, (int)BoStaffSwing.Style.Jab);
			return false;
		}

		float fullArc = 4f;
		BoStaffSwing.Style style = BoStaffSwing.Style.Swing;

		if (HitCombo >= 3) //Start a spin
		{
			style = BoStaffSwing.Style.Spin;
			fullArc = MathHelper.TwoPi;
		}
		else if (HitCombo == 2)
			fullArc += 3;

		_swingArc = _swingArc == fullArc ? -fullArc : fullArc;
		Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, _swingArc, 0, (int)style);
		return false;
	}
}

public class BoStaffSwing : ModProjectile
{
	private class BoNoiseCone(Entity entity, Vector2 basePosition, Vector2 velocity, float width, float length, float rotation, int maxTime, float taperExponent, int detatchTime = -1) : MotionNoiseCone(entity, basePosition, velocity, width, length, rotation, maxTime, detatchTime)
	{
		internal override bool UseLightColor => true;
		private readonly float _taperExponent = taperExponent;
		internal override float GetScroll() => 1.5f * (EaseFunction.EaseCircularOut.Ease(Progress) + TimeActive / 60f);

		internal override Color BrightColor => Color.PaleVioletRed;
		internal override Color DarkColor => new(168, 83, 72);

		internal override void DissipationStyle(ref float dissipationProgress, ref float finalExponent, ref float xCoordExponent)
		{
			dissipationProgress = EaseFunction.EaseQuadIn.Ease(Progress);
			finalExponent = 3f;
			xCoordExponent = 1.2f;
		}

		internal override float ColorLerpExponent => 1.5f;

		internal override int NumColors => 8;

		internal override float FinalIntensity => 2f;

		internal override void TaperStyle(ref float totalTapering, ref float taperExponent)
		{
			totalTapering = 1;
			taperExponent = _taperExponent;
		}

		internal override void TextureExponent(ref float minExponent, ref float maxExponent, ref float lerpExponent)
		{
			minExponent = 0.01f;
			maxExponent = 40f;
			lerpExponent = 2.25f;
		}

		internal override void XDistanceFade(ref float centeredPosition, ref float exponent)
		{
			float easedProgress = EaseFunction.EaseQuadIn.Ease(Progress);
			centeredPosition = MathHelper.Lerp(0.15f, 0.5f, easedProgress);
			exponent = MathHelper.Lerp(2.5f, 4f, easedProgress);
		}
	}

	public const float MaxReach = 70;

	public enum Style : byte
	{
		Swing,
		Spin,
		Jab
	}

	private float Ease
	{
		get
		{
			const float spinSpeed = 2f;

			if (ActivelySpinning)
			{
				float progress = (float)Counter / SwingTime * spinSpeed % 1;
				return Counter < SwingTime / 2 ? EaseFunction.EaseCircularIn.Ease(progress) : progress;
			}

			return EaseFunction.EaseCircularInOut.Ease((float)Counter / SwingTime);
		}
	}

	/// <summary> The damage distance in pixels. </summary>
	public float Reach => MaxReach * EaseFunction.EaseCircularOut.Ease(Math.Min((float)Counter * 1.1f / SwingTime, 1));
	public float SwingTime => Main.player[Projectile.owner].itemTimeMax * (Projectile.extraUpdates + 1);
	public bool ActivelySpinning => UseStyle is Style.Spin && !_released;

	public ref float SwingArc => ref Projectile.ai[0];
	public ref float Counter => ref Projectile.ai[1];
	public Style UseStyle => (Style)Projectile.ai[2];

	public override LocalizedText DisplayName => ModContent.GetInstance<BoStaff>().DisplayName;

	/// <summary> Whether channel has been released. Only used while spinning. </summary>
	private bool _released;
	private bool _collided;
	private float _meleeScale = 1f;

	public Vector2 GetEnd(int subtract = 0, float rotate = 0) => Projectile.position + (Vector2.UnitX * (Reach - subtract)).RotatedBy(Projectile.rotation - rotate * Projectile.direction);

	public void CreateTrail(ProjectileTrailRenderer renderer)
	{
		float rotation = Projectile.velocity.ToRotation();
		if (Projectile.direction == -1)
		{
			rotation = -rotation;
			rotation += MathHelper.Pi;
		}

		Color color = new(168, 83, 72);
		float trailWidth = 60;
		float trailDist = MaxReach - trailWidth / 2;

		SwingTrailParameters altParameters = new(SwingArc, rotation, trailDist, trailWidth * 1.1f)
		{
			Color = Color.PaleVioletRed,
			SecondaryColor = color,
			TrailLength = 0.2f,
			Intensity = 1,
			DissolveThreshold = UseStyle is Style.Spin ? 1f : 0.9f
		};

		SwingTrailParameters parameters = new(SwingArc, rotation, trailDist, trailWidth)
		{
			Color = color,
			SecondaryColor = Color.Black,
			TrailLength = 0.2f,
			Intensity = 1,
			DissolveThreshold = UseStyle is Style.Spin ? 1f : 0.9f
		};

		renderer.CreateTrail(Projectile, new SwingTrail(Projectile, altParameters, p => Ease, SwingTrail.BasicSwingShaderParams));
		renderer.CreateTrail(Projectile, new SwingTrail(Projectile, parameters, p => Ease, SwingTrail.BasicSwingShaderParams));
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
		Projectile.scale = 0.5f;
	}

	public override void AI()
	{
		var owner = Main.player[Projectile.owner];
		float progress = Projectile.direction == -1 ? 1f - Ease : Ease;
		int direction = Projectile.velocity.X > 0 ? 1 : -1;

		Projectile.scale = MathHelper.Min(Projectile.scale + 0.05f, _meleeScale);

		if (ActivelySpinning)
		{
			const int spinLimit = 180;

			if (!owner.channel || Counter > spinLimit)
			{
				_released = true;
				Counter = 0; //Causes ClientSpawn to be called again

				if (!Main.dedServ)
					TrailSystem.ProjectileRenderer.DissolveTrail(Projectile);
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

		Player.CompositeArmStretchAmount stretch = Player.CompositeArmStretchAmount.Full;

		if (UseStyle is Style.Jab)
			stretch = (int)((float)Counter / SwingTime * 4f) switch
			{
				2 => Player.CompositeArmStretchAmount.Quarter,
				3 => Player.CompositeArmStretchAmount.ThreeQuarters,
				4 => Player.CompositeArmStretchAmount.Full,
				_ => Player.CompositeArmStretchAmount.None
			};

		if (!UpdateCollision())
			Projectile.rotation = Projectile.velocity.ToRotation() - SwingArc / 2 + SwingArc * progress;

		Projectile.spriteDirection = Projectile.direction = owner.direction = direction;
		Projectile.Center = ActivelySpinning ? owner.Center : owner.GetFrontHandPosition(stretch, Projectile.rotation - 1.57f);

		owner.SetCompositeArmFront(true, stretch, Projectile.rotation - 1.57f);
		owner.SetCompositeArmBack(true, stretch, Projectile.rotation - 1.57f);
		owner.heldProj = Projectile.whoAmI;

		if (Counter == 0)
			ClientSpawn(owner);

		Counter++;

		if (ActivelySpinning || Counter < SwingTime - 1)
			owner.itemAnimation = owner.itemTime = Projectile.timeLeft = 2;

		if (owner.dead)
			Projectile.Kill();
	}

	/// <returns> Whether a tile collision has occurred. </returns>
	private bool UpdateCollision()
	{
		if (UseStyle is Style.Spin && _released && !_collided && Counter > 10 && SolidCollision()) //One-time hit effects
		{
			_collided = true;

			TrailSystem.ProjectileRenderer.DissolveTrail(Projectile);
			Collision.HitTiles(GetEnd(20), Vector2.Zero, Projectile.width, Projectile.height);

			SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact with { Pitch = 0.5f }, GetEnd());

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
			CreateTrail(TrailSystem.ProjectileRenderer);

			if (UseStyle is Style.Jab)
			{
				ParticleHandler.SpawnParticle(new BoNoiseCone(Projectile, Projectile.Center + Projectile.velocity * 50f, Projectile.velocity * 5, 120, 100, Projectile.rotation, (int)(SwingTime * 0.8f), 0.8f));
				SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing, Projectile.Center);
			}
		}

		_meleeScale = owner.GetAdjustedItemScale(owner.HeldItem);
		Projectile.localNPCHitCooldown = ActivelySpinning ? 25 : -1;
	}

	public override void OnKill(int timeLeft)
	{
		if (Main.myPlayer == Projectile.owner)
		{
			if (UseStyle is not Style.Jab)
				BoStaff.HitCombo = Projectile.numHits == 0 || UseStyle is Style.Spin ? 0 : BoStaff.HitCombo + 1;

			var owner = Main.player[Projectile.owner];
			if (UseStyle is Style.Jab && owner.controlUseTile)
			{
				var velocity = owner.DirectionTo(Main.MouseWorld);
				Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, velocity.RotatedByRandom(0.1f), Type, Projectile.damage, Projectile.knockBack, Projectile.owner, 0, 0, (int)Style.Jab);
			}
		}
	}

	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.HitDirectionOverride = target.Center.X - Projectile.Center.X < 0 ? -1 : 1;

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		var position = target.Hitbox.ClosestPointInRect(GetEnd());
		Color color = new(168, 83, 72);

		for (int i = 0; i < 3; i++)
		{
			var velocity = (Projectile.DirectionTo(position) * Main.rand.NextFloat(5f, 10f)).RotatedByRandom(1.5f);

			ParticleHandler.SpawnParticle(new ImpactLine(position, velocity, color * 2, new Vector2(0.5f, 1f) * Main.rand.NextFloat(0.1f, 1.5f),
				Main.rand.Next(15, 20), 0.8f) { UseLightColor = true, NoLight = true });
		}

		if (UseStyle is Style.Jab)
		{
			ParticleHandler.SpawnParticle(new TexturedPulseCircle(position, Color.Gray, 0.7f, 140, 20, "supPerlin", Vector2.One, EaseFunction.EaseCubicOut).WithSkew(0.8f, Projectile.rotation).UsesLightColor());

			SoundEngine.PlaySound(SoundID.DrumCymbal1 with { Pitch = 0.9f, Volume = 0.2f }, position);
			SoundEngine.PlaySound(SoundID.DrumTamaSnare, position);

			target.velocity += Projectile.velocity * 10 * target.knockBackResist;
		}
		else
		{
			SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing with { Pitch = 0.9f }, position);

			Vector2 velocity = Projectile.velocity.RotatedBy(MathHelper.PiOver2 * (Math.Sign(SwingArc) * Projectile.direction));
			ParticleHandler.SpawnParticle(new BoNoiseCone(null, position, velocity * 5f, 200, 100, velocity.ToRotation(), 20, 0.8f));
		}
	}

	public override bool? CanCutTiles() => _collided ? false : null;
	public override void CutTiles() => Projectile.PlotTileCut(Reach, Projectile.width);

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		int lineWidth = 30;
		var endCenter = GetEnd() + Projectile.Size / 2;
		float collisionPoint = 0f;

		return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, endCenter, lineWidth, ref collisionPoint);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		var texture = TextureAssets.Projectile[Type].Value;
		Rectangle source = texture.Frame();
		Vector2 origin = new(source.Width - (float)Reach, source.Height / 2);

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, source, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
		return false;
	}
}