using Microsoft.Xna.Framework.Graphics;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.MathHelpers;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria;
using Terraria.Audio;

namespace SpiritReforged.Content.Forest.JinxBow;

public class JinxBowMinion() : BaseMinion(600, 600, new Vector2(12, 12))
{
	private struct ArrowData()
	{
		public int type;
		public int damage;
		public float knockBack;
		public float shootSpeed;
		public Color brightColor;
	}

	private readonly int attackCooldown = 60;
	private readonly int bounceTime = 30;

	private bool _isDoingEmpoweredShot = false;

	private int _empoweredShotTarget = -1;
	private int _bounceTimer = 0;

	private float _storedRotation = 0;

	private Vector2 _storedPosition = Vector2.Zero;
	private Vector2 _storedOffset = Vector2.Zero;

	private ArrowData _selectedArrow;

	private ref float AiTimer => ref Projectile.ai[0];

	public override void AbstractSetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = 5;
		ProjectileID.Sets.TrailingMode[Type] = 0;
	}

	public override void AbstractSetDefaults() => Projectile.minionSlots = 0f;

	public override bool PreAI()
	{
		Player mp = Main.player[Projectile.owner];
		if (mp.HasAccessory<JinxBow>())
			Projectile.timeLeft = 2;

		SetArrowData(mp);

		return true;
	}

	public override void AI()
	{
		if (_isDoingEmpoweredShot)
			EmpoweredShot(Main.player[Projectile.owner], Main.npc[_empoweredShotTarget]);
		else
			base.AI();

		AiTimer = Math.Max(0, AiTimer - 1);
		_bounceTimer = Math.Max(0, _bounceTimer - 1);

	}

	public void EmpoweredShot(Player player, NPC target)
	{
		void EndAttack()
		{
			_isDoingEmpoweredShot = false;
			AiTimer = attackCooldown + bounceTime;
			_empoweredShotTarget = -1;
			Projectile.netUpdate = true;
		}

		if (!target.active)
		{
			EndAttack();
			return;
		}

		int targetDirection = player.DirectionTo(target.Center).X > 0 ? 1 : -1;

		float desiredRotation = -MathHelper.PiOver4;
		if (targetDirection < 0)
			desiredRotation -= MathHelper.PiOver2;

		_storedRotation = _storedRotation.AngleLerp(desiredRotation, 0.3f);
		Projectile.rotation = _storedRotation;

		var desiredPos = new Vector2((int)player.MountedCenter.X, (int)player.MountedCenter.Y - 40 + player.gfxOffY);
		desiredPos += desiredPos.DirectionTo(target.Center) * 20;

		_storedPosition = Vector2.Lerp(_storedPosition + Projectile.Size / 2, desiredPos, 0.2f) - Projectile.Size / 2;
		_storedOffset *= 0.94f;
		Projectile.position = _storedPosition + _storedOffset;

		if (AiTimer <= 0)
		{
			AiTimer = attackCooldown + bounceTime;

			Vector2 arrowVelocity = Vector2.UnitX.RotatedBy(desiredRotation + MathHelper.PiOver2 * targetDirection) * _selectedArrow.shootSpeed;
			Vector2 arrowPos = target.Center - arrowVelocity * 15;

			_bounceTimer = bounceTime;

			PreNewProjectile.New(Projectile.GetSource_FromThis(), arrowPos, arrowVelocity, _selectedArrow.type, _selectedArrow.damage, _selectedArrow.knockBack, Projectile.owner, preSpawnAction: p =>
			{
				p.DamageType = DamageClass.Summon;

				p.minion = true;
				p.GetGlobalProjectile<JinxBowShot>().IsJinxbowShot = true;
			});

			Vector2 visualArrowVelocity = Vector2.UnitX.RotatedBy(desiredRotation) * _selectedArrow.shootSpeed;
			_storedOffset -= visualArrowVelocity * 1.5f;

			if (!Main.dedServ)
			{
				SoundEngine.PlaySound(SoundID.DD2_PhantomPhoenixShot with { Pitch = 1.25f }, Projectile.Center);

				ParticleHandler.SpawnParticle(new ImpactLinePrim(Projectile.Center + visualArrowVelocity, visualArrowVelocity * 0.5f, _selectedArrow.brightColor.Additive() * 0.66f, new(1, 4), 12, 1)); 
				ParticleHandler.SpawnParticle(new ImpactLinePrim(arrowPos, arrowVelocity * 0.3f, _selectedArrow.brightColor.Additive() * 0.66f, new(1, 4), 15, 1));
			}

			EndAttack();
		}
	}

	public override bool? CanDamage() => false;

	public override void IdleMovement(Player player)
	{
		var desiredPos = new Vector2((int)player.MountedCenter.X - player.direction * 30, (int)player.MountedCenter.Y - 28 + (float)Math.Sin(Main.GameUpdateCount / 30f) * 5 + player.gfxOffY);

		AiTimer = attackCooldown;
		_bounceTimer = 0;
		Projectile.frame = 0;
		float rotationOffset = player.direction == -1 ? MathHelper.Pi : 0;
		Projectile.velocity = Vector2.Zero;

		float lockOnSpeed = 0.25f;
		lockOnSpeed += 0.75f * Math.Min(Vector2.Distance(Projectile.Center, desiredPos) / 500f, 1);

		_storedOffset = Vector2.Zero;
		_storedPosition = Vector2.Lerp(Projectile.Center, desiredPos, lockOnSpeed) - Projectile.Size / 2;
		Projectile.position = _storedPosition;

		Vector2 oldPosDifference = (Projectile.position - Projectile.oldPosition);
		oldPosDifference.Y *= player.direction;

		_storedRotation = _storedRotation.AngleLerp(rotationOffset + oldPosDifference.X * 0.06f + oldPosDifference.Y * 0.1f, 0.2f);
		_storedRotation = _storedRotation.AngleLerp(rotationOffset, 0.2f);
		Projectile.rotation = _storedRotation;
	}

	public override void TargettingBehavior(Player player, NPC target)
	{
		_storedRotation = Utils.AngleLerp(_storedRotation, Projectile.AngleTo(target.Center), 0.2f);
		Projectile.rotation = _storedRotation;

		var desiredPos = new Vector2((int)player.MountedCenter.X, (int)player.MountedCenter.Y - 40 + player.gfxOffY);
		desiredPos += desiredPos.DirectionTo(target.Center) * 20;

		_storedPosition = Vector2.Lerp(_storedPosition + Projectile.Size / 2, desiredPos, 0.2f) - Projectile.Size / 2;
		_storedOffset *= 0.94f;
		Projectile.position = _storedPosition + _storedOffset;

		if (AiTimer <= 0)
		{
			AiTimer = attackCooldown + bounceTime;

			float ticksFromTarget = Projectile.Distance(target.Center) / _selectedArrow.shootSpeed;
			Vector2 arrowVelocity = Projectile.DirectionTo(target.Center + target.velocity * ticksFromTarget / 2) * _selectedArrow.shootSpeed;

			_bounceTimer = bounceTime;

			PreNewProjectile.New(Projectile.GetSource_FromThis(), Projectile.Center, arrowVelocity, _selectedArrow.type, _selectedArrow.damage, _selectedArrow.knockBack, Projectile.owner, preSpawnAction: p => 
			{
				p.DamageType = DamageClass.Summon; 

				//Can't really change the static projectile id set of minion shots, so this is the only way to make whip effects work (why is this still the case with summon damage class existing)
				p.minion = true;
				p.GetGlobalProjectile<JinxBowShot>().IsJinxbowShot = true;
			});

			_storedOffset -= arrowVelocity * 1.5f;

			if(!Main.dedServ)
			{
				SoundEngine.PlaySound(SoundID.DD2_PhantomPhoenixShot with { Pitch = 1.25f }, Projectile.Center);

				ParticleHandler.SpawnParticle(new ImpactLinePrim(Projectile.Center + arrowVelocity * 2f, arrowVelocity * 0.3f, _selectedArrow.brightColor.Additive() * 0.66f, new(0.66f, 3f), 10, 1));
			}
		}
	}

	public void DoEmpoweredShot(NPC target)
	{
		_isDoingEmpoweredShot = true;
		_empoweredShotTarget = target.whoAmI;
		AiTimer = attackCooldown;
		_bounceTimer = 0;
		Projectile.netUpdate = true;
	}

	private void SetArrowData(Player player)
	{
		FindAmmo(player, AmmoID.Arrow, out int? projToFire, out int? ammoDamage, out float? ammoKB, out float? ammoVel);
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
	}

	private static void FindAmmo(Player owner, int ammoID, out int? projToFire, out int? ammoDamage, out float? ammoKB, out float? ammoVel)
	{
		const int ammoInventoryStart = 54;
		const int ammoInventoryEnd = 58;

		projToFire = null;
		ammoDamage = null;
		ammoKB = null;
		ammoVel = null;

		for(int i = ammoInventoryStart; i < ammoInventoryEnd; i++)
		{
			Item selectedItem = owner.inventory[i];
			if (selectedItem.ammo == ammoID && selectedItem.stack > 0)
			{
				projToFire = selectedItem.shoot;
				ammoDamage = selectedItem.damage;
				ammoKB = selectedItem.knockBack;
				ammoVel = selectedItem.shootSpeed;
				return;
			}
		}

		for(int i = 0; i < ammoInventoryStart; i++)
		{
			Item selectedItem = owner.inventory[i];
			if (selectedItem.ammo == ammoID && selectedItem.stack > 0)
			{
				projToFire = selectedItem.shoot;
				ammoDamage = selectedItem.damage;
				ammoKB = selectedItem.knockBack;
				ammoVel = selectedItem.shootSpeed;
				return;
			}
		}
	}

	public override bool DoAutoFrameUpdate(ref int framesPerSecond, ref int startFrame, ref int endFrame) => false;

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D projTex = TextureAssets.Projectile[Type].Value;
		float shootProgress = _targetNPC != null ? MathHelper.Max(1 - AiTimer / attackCooldown, 0) : 0;
		float bounceProgress = 1 - (float)_bounceTimer / bounceTime;

		//Draw string
		float stringLength = 16;
		float maxDrawback = 12;
		Vector2 stringOrigin = new(4, 19);
		Color stringColor = Projectile.GetAlpha(Color.LightGray).MultiplyRGBA(lightColor);
		Color nonRefLightColor = lightColor;

		BowDraw(shootProgress, bounceProgress, Projectile.rotation, stringLength, maxDrawback, Projectile.Center, projTex.Size(), stringOrigin, stringColor, delegate (Vector2 stringCenter, float easedCharge)
		{
			if (_targetNPC != null && AiTimer < attackCooldown)
			{
				Texture2D arrowTex = TextureAssets.Projectile[_selectedArrow.type].Value;
				Texture2D arrowSolid = TextureColorCache.ColorSolid(arrowTex, Color.Lavender);

				Vector2 arrowPos = stringCenter;
				arrowPos -= (projTex.Size() / 2).RotatedBy(Projectile.rotation);
				arrowPos += Projectile.Center - Main.screenPosition;
				var arrowOrigin = new Vector2(arrowTex.Width / 2, arrowTex.Height);

				Color glowColor = _selectedArrow.brightColor;
				glowColor = Color.Lerp(glowColor, Color.Lavender, 0.33f).Additive(100);
				glowColor *= EaseFunction.EaseQuadOut.Ease(easedCharge);

				for (int i = 0; i < 12; i++)
				{
					Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / 12f) * 2;
					Main.EntitySpriteDraw(arrowSolid, arrowPos + offset, null, glowColor * 0.15f, Projectile.rotation + MathHelper.PiOver2, arrowOrigin, Projectile.scale, SpriteEffects.None);
				}

				Main.EntitySpriteDraw(arrowTex, arrowPos, null, Projectile.GetAlpha(nonRefLightColor).Additive(200) * easedCharge, Projectile.rotation + MathHelper.PiOver2, arrowOrigin, Projectile.scale, SpriteEffects.None, 0);
				Main.EntitySpriteDraw(arrowSolid, arrowPos, null, glowColor * EaseFunction.EaseCubicIn.Ease(1 - shootProgress), Projectile.rotation + MathHelper.PiOver2, arrowOrigin, Projectile.scale, SpriteEffects.None);
			}
		});

		//Draw proj
		Projectile.QuickDraw();

		return false;
	}

	public static void BowDraw(float curDrawbackProgress, float bounceProgress, float rotation, float stringLength, float maxDrawback, Vector2 drawPosition, Vector2 bowSize, Vector2 stringOrigin, Color stringColor, Action<Vector2, float> arrowDrawHook)
	{
		float stringHalfLength = stringLength / 2;
		const float stringScale = 2;

		float easedCharge = EaseFunction.EaseCircularOut.Ease(curDrawbackProgress);
		float curDrawback = easedCharge + (1 - EaseFunction.EaseOutElastic().Ease(bounceProgress)) * (1 - easedCharge);
		curDrawback *= maxDrawback;

		var pointTop = new Vector2(stringOrigin.X, stringOrigin.Y - stringHalfLength);
		var pointMiddle = new Vector2(stringOrigin.X - curDrawback, stringOrigin.Y);
		var pointBottom = new Vector2(stringOrigin.X, stringOrigin.Y + stringHalfLength);
		int splineIterations = 30;
		Vector2[] spline = Spline.CreateSpline([pointTop, pointMiddle, pointBottom], splineIterations);
		for (int i = 0; i < splineIterations; i++)
		{
			var pixelPos = spline[i];

			pixelPos = pixelPos.RotatedBy(rotation);
			pixelPos -= (bowSize / 2).RotatedBy(rotation);
			pixelPos += drawPosition - Main.screenPosition;

			Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, pixelPos, new Rectangle(0, 0, 1, 1), stringColor, rotation, Vector2.Zero, stringScale, SpriteEffects.None, 0);
		}

		arrowDrawHook(pointMiddle.RotatedBy(rotation), easedCharge);
	}
}