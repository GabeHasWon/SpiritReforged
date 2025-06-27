using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using System.IO;
using Terraria.Audio;

namespace SpiritReforged.Content.Forest.JinxBow;

public class JinxBowMinion() : BaseMinion(600, 800, new Vector2(12, 12))
{
	public const int MARK_COOLDOWN = 600;
	public const float MARK_LINGER_RATIO = 0.5f;

	private const int FIRE_TIME = 60;
	private const int COOLDOWN_TIME = 30;

	public static Color JinxbowCyan = Color.Lerp(Color.LightCyan, Color.Cyan, 0.5f).Additive(100);

	private struct ArrowData()
	{
		public int type;
		public int damage;
		public float knockBack;
		public float shootSpeed;
		public Color brightColor;
	}

	private bool _isDoingEmpoweredShot = false;
	private bool _hasDoneEmpoweredShot = false;

	public int EmpoweredShotTarget { get; set; } = -1;

	private Vector2 _storedPosition = Vector2.Zero;
	private Vector2 _storedOffset = Vector2.Zero;

	private ArrowData _selectedArrow;

	public ref float AiTimer => ref Projectile.ai[0];
	public ref float BounceTimer => ref Projectile.ai[1];
	public ref float StoredRotation => ref Projectile.ai[2];

	public float MarkCooldown { get; set; } = 0;

	public override void AbstractSetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = 5;
		ProjectileID.Sets.TrailingMode[Type] = 0;
	}

	public override void AbstractSetDefaults() => Projectile.minionSlots = 0f;

	public override bool? CanDamage() => false;

	public override bool DoAutoFrameUpdate(ref int framesPerSecond, ref int startFrame, ref int endFrame) => false;

	public override bool PreAI()
	{
		Player mp = Main.player[Projectile.owner];
		if (mp.HasEquip<JinxBow>())
			Projectile.timeLeft = 2;

		SetArrowData(mp);

		return true;
	}

	public override void AI()
	{
		if (EmpoweredShotTarget != -1) //Has a target
			EmpoweredShotBehavior(Main.player[Projectile.owner], Main.npc[EmpoweredShotTarget]);
		else
			base.AI();

		AiTimer = Math.Max(0, AiTimer - 1);
		BounceTimer = Math.Max(0, BounceTimer - 1);
		MarkCooldown = Math.Max(0, MarkCooldown - 1);
	}

	public void EmpoweredShotBehavior(Player player, NPC target)
	{
		if (!_isDoingEmpoweredShot)
		{
			DoEmpoweredShot(target);
			_isDoingEmpoweredShot = true;
		}

		if (!target.active)
		{
			EndAttack();
			return;
		}

		//Aim diagonally upwards in direction of target rather than directly at target
		int targetDirection = player.DirectionTo(target.Center).X > 0 ? 1 : -1;

		float desiredRotation = -MathHelper.PiOver4;
		if (targetDirection < 0)
			desiredRotation -= MathHelper.PiOver2;

		StoredRotation = StoredRotation.AngleLerp(desiredRotation, 0.3f);
		Projectile.rotation = StoredRotation;

		var desiredPos = new Vector2((int)player.MountedCenter.X, (int)player.MountedCenter.Y - 40 + player.gfxOffY);
		desiredPos += desiredPos.DirectionTo(target.Center) * 20;

		_storedPosition = Vector2.Lerp(_storedPosition + Projectile.Size / 2, desiredPos, 0.2f) - Projectile.Size / 2;
		_storedOffset *= 0.94f;
		Projectile.position = _storedPosition + _storedOffset;

		//Fire projectile diagonally downwards towards target from the air
		if (AiTimer <= 0)
		{
			AiTimer = FIRE_TIME + COOLDOWN_TIME;
			BounceTimer = COOLDOWN_TIME;

			Vector2 arrowVelocity = Vector2.UnitX.RotatedBy(desiredRotation + MathHelper.PiOver2 * targetDirection) * _selectedArrow.shootSpeed;

			if (Projectile.owner == Main.myPlayer) //Only ever spawn projectiles on the owning client
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, arrowVelocity, _selectedArrow.type, _selectedArrow.damage, _selectedArrow.knockBack, Projectile.owner, target.whoAmI);

			Vector2 visualArrowVelocity = Vector2.UnitX.RotatedBy(desiredRotation) * _selectedArrow.shootSpeed;
			_storedOffset -= visualArrowVelocity;

			if (!Main.dedServ)
				EmpoweredShotFX(visualArrowVelocity);

			_hasDoneEmpoweredShot = true;
			Projectile.netUpdate = true;
		}

		//Glow particles swirl in on arrow
		if (!_hasDoneEmpoweredShot && !Main.dedServ && AiTimer % 3 == 0 && AiTimer > 30)
		{
			float scale = Main.rand.NextFloat(0.3f, 0.7f);
			int lifeTime = Main.rand.Next(20, 30);
			Vector2 offset = Main.rand.NextVector2Unit() * Main.rand.NextFloat(50, 60);
			static void DelegateAction(Particle p, Projectile owner, Vector2 offset)
			{
				if (!owner.active)
					p.Kill();

				float easedProgress = EaseFunction.EaseQuadIn.Ease(1 - p.Progress);
				p.Position = owner.Center + offset.RotatedBy(MathHelper.Pi * easedProgress * 0.66f) * easedProgress;
			}

			ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center + offset, Vector2.Zero, Color.Cyan.Additive(), scale, lifeTime, 4, p => DelegateAction(p, Projectile, offset)));
			ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center + offset, Vector2.Zero, Color.LightGoldenrodYellow.Additive(), scale, lifeTime, 4, p => DelegateAction(p, Projectile, offset)));
		}

		//Only end attack if the bounce timer is 0 after already firing, making it function as a cooldown before returning to normal behavior
		if (_hasDoneEmpoweredShot && BounceTimer == 0)
			EndAttack();

		void EndAttack()
		{
			_isDoingEmpoweredShot = false;
			_hasDoneEmpoweredShot = false;
			AiTimer = FIRE_TIME;
			EmpoweredShotTarget = -1;
			Projectile.netUpdate = true;
		}
	}

	public override void IdleMovement(Player player)
	{
		var desiredPos = new Vector2((int)player.MountedCenter.X - player.direction * 30, (int)player.MountedCenter.Y - 28 + (float)Math.Sin(Main.GameUpdateCount / 30f) * 5 + player.gfxOffY);

		AiTimer = FIRE_TIME;
		BounceTimer = 0;
		Projectile.frame = 0;
		float rotationOffset = player.direction == -1 ? MathHelper.Pi : 0;
		Projectile.velocity = Vector2.Zero;

		float lockOnSpeed = 0.25f;
		lockOnSpeed += 0.75f * Math.Min(Vector2.Distance(Projectile.Center, desiredPos) / 500f, 1);

		_storedOffset = Vector2.Zero;
		_storedPosition = Vector2.Lerp(Projectile.Center, desiredPos, lockOnSpeed) - Projectile.Size / 2;
		Projectile.position = _storedPosition;

		Vector2 oldPosDifference = Projectile.position - Projectile.oldPosition;
		oldPosDifference.Y *= player.direction;

		StoredRotation = StoredRotation.AngleLerp(rotationOffset + oldPosDifference.X * 0.06f + oldPosDifference.Y * 0.1f, 0.2f);
		StoredRotation = StoredRotation.AngleLerp(rotationOffset, 0.2f);
		Projectile.rotation = StoredRotation;
	}

	public override void TargettingBehavior(Player player, NPC target)
	{
		StoredRotation = Utils.AngleLerp(StoredRotation, Projectile.AngleTo(target.Center), 0.2f);
		Projectile.rotation = StoredRotation;

		var desiredPos = new Vector2((int)player.MountedCenter.X, (int)player.MountedCenter.Y - 40 + player.gfxOffY);
		desiredPos += desiredPos.DirectionTo(target.Center) * 20;

		_storedPosition = Vector2.Lerp(_storedPosition + Projectile.Size / 2, desiredPos, 0.2f) - Projectile.Size / 2;
		_storedOffset *= 0.94f;
		Projectile.position = _storedPosition + _storedOffset;

		if (AiTimer <= 0)
		{
			AiTimer = FIRE_TIME + COOLDOWN_TIME;

			float ticksFromTarget = Projectile.Distance(target.Center) / _selectedArrow.shootSpeed;
			Vector2 arrowVelocity = Projectile.DirectionTo(target.Center + target.velocity * ticksFromTarget / 2) * _selectedArrow.shootSpeed;

			BounceTimer = COOLDOWN_TIME;

			if (Projectile.owner == Main.myPlayer)
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, arrowVelocity, _selectedArrow.type, _selectedArrow.damage, _selectedArrow.knockBack, Projectile.owner);

			_storedOffset -= arrowVelocity;

			if (!Main.dedServ)
			{
				SoundEngine.PlaySound(SoundID.DD2_BallistaTowerShot with { Pitch = 1.25f }, Projectile.Center);
				ParticleHandler.SpawnParticle(new ImpactLinePrim(Projectile.Center + arrowVelocity * 2f, arrowVelocity * 0.3f, _selectedArrow.brightColor.Additive() * 0.66f, new(0.66f, 3f), 10, 1));
			}
		}
	}

	/// <summary> Handles initial empowered shot visuals and effects. Does <b>NOT</b> set <see cref="EmpoweredShotTarget"/>. </summary>
	private void DoEmpoweredShot(NPC target)
	{
		AiTimer = FIRE_TIME;
		BounceTimer = 0;

		if (!Main.dedServ)
		{
			ParticleHandler.SpawnParticle(new ImpactLinePrim(target.Center, Vector2.Zero, JinxbowCyan, new(0.75f, 3), 12, 1));
			ParticleHandler.SpawnParticle(new LightBurst(target.Center, Main.rand.NextFloatDirection(), JinxbowCyan, 0.66f, 20));

			for (int i = 0; i < 10; i++)
			{
				Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(0.5f, 4);
				float scale = Main.rand.NextFloat(0.3f, 0.7f);
				int lifeTime = Main.rand.Next(12, 40);
				static void DelegateAction(Particle p) => p.Velocity *= 0.9f;

				ParticleHandler.SpawnParticle(new GlowParticle(target.Center, velocity, Color.Cyan.Additive(), scale, lifeTime, 1, DelegateAction));
				ParticleHandler.SpawnParticle(new GlowParticle(target.Center, velocity, Color.White.Additive(), scale, lifeTime, 1, DelegateAction));
			}

			ParticleHandler.SpawnParticle(new TexturedPulseCircle(target.Center, JinxbowCyan, 0.8f, 150, 20, "Star2", new(2, 1), EaseFunction.EaseCircularOut));
		}
	}

	private void SetArrowData(Player player)
	{
		BowHelpers.FindAmmo(player, AmmoID.Arrow, out int? projToFire, out int? ammoDamage, out float? ammoKB, out float? ammoVel, 1);
		int type = projToFire ?? ProjectileID.WoodenArrowFriendly;
		float speed = 10 + ammoVel ?? 0;
		float knockBack = Projectile.knockBack + (ammoKB ?? 0);
		int damage = Projectile.damage + (ammoDamage ?? 0);

		_selectedArrow = new()
		{
			type = type,
			damage = damage,
			shootSpeed = speed,
			knockBack = knockBack
		};

		if (!Main.dedServ)
		{
			Main.instance.LoadProjectile(type);
			Texture2D arrowTex = TextureAssets.Projectile[type].Value;
			_selectedArrow.brightColor = TextureColorCache.GetBrightestColor(arrowTex);
		}

		if (_isDoingEmpoweredShot)
		{
			_selectedArrow.damage = (int)(_selectedArrow.damage * 1.33f);
			_selectedArrow.type = ModContent.ProjectileType<JinxArrow>();
			_selectedArrow.brightColor = JinxbowCyan;
		}
	}

	private void EmpoweredShotFX(Vector2 visualArrowVelocity)
	{
		SoundEngine.PlaySound(SoundID.DD2_BallistaTowerShot with { Pitch = 1.25f }, Projectile.Center);
		SoundEngine.PlaySound(SoundID.Item125 with { Pitch = 1.25f }, Projectile.Center);

		//Visuals from arrow fire position
		Color particleColor = JinxbowCyan;
		Vector2 particleSpawn = Projectile.Center + visualArrowVelocity;
		float ringRotation = visualArrowVelocity.ToRotation() + MathHelper.Pi;

		ParticleHandler.SpawnParticle(new ImpactLinePrim(particleSpawn, visualArrowVelocity / 2, particleColor, new(0.75f, 4), 16, 0.9f));

		JinxArrowRing(particleSpawn, -visualArrowVelocity / 60, 120, ringRotation);
		JinxArrowRing(Projectile.Center + visualArrowVelocity / 2, visualArrowVelocity / 60, 100, ringRotation);
	}

	public static void JinxArrowRing(Vector2 spawnPos, Vector2 velocity, float size, float rotation, float skew = 0.8f)
	{
		Color particleColor = JinxbowCyan;
		Particle p = new TexturedPulseCircle(spawnPos, particleColor, 0.8f, size, 16, "swirlNoise2", new(2, 0.5f), EaseFunction.EaseCircularOut, false, 0.3f).WithSkew(skew, rotation);
		p.Velocity = velocity;
		ParticleHandler.SpawnParticle(p);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D projTex = TextureAssets.Projectile[Type].Value;
		Texture2D starTex = AssetLoader.LoadedTextures["Star2"].Value;
		float shootProgress = _targetNPC != null ? MathHelper.Max(1 - AiTimer / FIRE_TIME, 0) : 0;
		float bounceProgress = 1 - (float)BounceTimer / COOLDOWN_TIME;

		//Draw string
		float stringLength = 16;
		float maxDrawback = 12;
		Vector2 stringOrigin = new(4, 19);
		Color stringColor = Projectile.GetAlpha(Color.LightGray).MultiplyRGBA(lightColor);
		Color nonRefLightColor = lightColor;

		BowHelpers.BowDraw(shootProgress, bounceProgress, Projectile.rotation, stringLength, maxDrawback, Projectile.Center, projTex.Size(), stringOrigin, stringColor, delegate (Vector2 stringCenter, float easedCharge)
		{
			if ((_targetNPC != null || _isDoingEmpoweredShot) && AiTimer < FIRE_TIME)
			{
				Texture2D arrowTex = TextureAssets.Projectile[_selectedArrow.type].Value;
				Texture2D arrowSolid = TextureColorCache.ColorSolid(arrowTex, Color.LightCyan);

				Vector2 arrowPos = stringCenter;
				arrowPos -= (projTex.Size() / 2).RotatedBy(Projectile.rotation);
				arrowPos += Projectile.Center - Main.screenPosition;
				var arrowOrigin = new Vector2(arrowTex.Width / 2, arrowTex.Height);

				Color solidColor = Projectile.GetAlpha(nonRefLightColor).Additive(200) * easedCharge;
				Color glowColor = _selectedArrow.brightColor;
				glowColor = Color.Lerp(glowColor, Color.LightCyan, 0.33f).Additive(100);
				glowColor *= EaseFunction.EaseQuadOut.Ease(easedCharge);

				Rectangle drawRect = arrowTex.Bounds;

				//Make it look like the arrow is being magically formed in the bow by reducing draw rectangle height until fully charged, and color adjustments
				if(_isDoingEmpoweredShot)
				{
					glowColor *= easedCharge;
					solidColor = glowColor;
					drawRect.Height = (int)(drawRect.Height * easedCharge);
				}

				//Glowy blur behind the arrow
				for (int i = 0; i < 12; i++)
				{
					Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / 12f) * 2;
					if (_isDoingEmpoweredShot)
						offset /= 2;

					Main.EntitySpriteDraw(arrowSolid, arrowPos + offset, drawRect, glowColor * 0.15f, Projectile.rotation + MathHelper.PiOver2, arrowOrigin, Projectile.scale, SpriteEffects.None);
				}

				//Draw solid arrow and white flash above the arrow when spawned in
				Color arrowColor = Projectile.GetAlpha(nonRefLightColor);
				if (_isDoingEmpoweredShot)
					arrowColor = Color.White.Additive(200) * easedCharge;

				Main.EntitySpriteDraw(arrowTex, arrowPos, drawRect, arrowColor * easedCharge, Projectile.rotation + MathHelper.PiOver2, arrowOrigin, Projectile.scale, SpriteEffects.None, 0);
				Main.EntitySpriteDraw(arrowSolid, arrowPos, drawRect, glowColor * EaseFunction.EaseCubicIn.Ease(1 - shootProgress), Projectile.rotation + MathHelper.PiOver2, arrowOrigin, Projectile.scale, SpriteEffects.None);

				//During empowered shot, do a blur star at the arrow's head
				if (_isDoingEmpoweredShot)
				{
					var arrowHead = arrowPos + Vector2.UnitX.RotatedBy(Projectile.rotation) * (arrowTex.Height - 5);
					var starScale = new Vector2(2 * easedCharge, 1) * 0.07f;
					Color starColor = glowColor.Additive() * shootProgress;

					Main.EntitySpriteDraw(starTex, arrowHead, null, starColor, 0, starTex.Size() / 2, starScale, SpriteEffects.None);
					Main.EntitySpriteDraw(starTex, arrowHead, null, starColor * 0.66f, 0, starTex.Size() / 2, starScale, SpriteEffects.None);
					Main.EntitySpriteDraw(starTex, arrowHead, null, starColor, 0, starTex.Size() / 2, starScale / 2, SpriteEffects.None);
				}
			}
		});

		//Draw proj
		Projectile.QuickDraw();

		return false;
	}

	public override void SendExtraAI(BinaryWriter writer) => writer.Write((short)EmpoweredShotTarget);
	public override void ReceiveExtraAI(BinaryReader reader) => EmpoweredShotTarget = reader.ReadInt16();
}