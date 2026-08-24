using SpiritReforged.Common.BuffCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Ocean.Items.Reefhunter.Particles;
using SpiritReforged.Content.Particles;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Ocean.Items.JellyfishStaff;

public class JellyfishMinionColors // stores colors for each variant
{
	public Color BaseColor = Color.White;
	public Color LightningStart = Color.White;
	public Color LightningEnd = Color.White;
	public Color AuraBase = Color.White;
	public Color AuraLight = Color.White;
	public Color ParticleOne = Color.White;
	public Color ParticleTwo = Color.White;
	public Color SmokeColor = Color.White;

	public JellyfishMinionColors(bool pink, bool green = false) // will have pink colors if pink.. yeah
	{
		if (pink)
		{
			BaseColor = new(255, 133, 242);
			LightningStart = new(254, 92, 237);
			LightningEnd = new(255, 132, 229);
			AuraBase = new(231, 0, 131);
			AuraLight = new(255, 213, 249);
			SmokeColor = new(225, 197, 222);
			ParticleOne = new(255, 133, 242);
			ParticleTwo = Color.White;
		}
		else
		{
			BaseColor = new(119, 146, 225);
			LightningStart = new(93, 134, 253);
			LightningEnd = new(155, 255, 255);
			AuraBase = new(0, 216, 255);
			AuraLight = new(213, 251, 255);
			SmokeColor = new(183, 192, 225);
			ParticleOne = new(119, 146, 225);
			ParticleTwo = Color.White;
		}

		if (green)
		{
			BaseColor = new(92, 254, 109);
			LightningStart = new(125, 255, 125);
			LightningEnd = new(180, 255, 190);
			AuraBase = new(0, 213, 160);
			AuraLight = new(213, 255, 237);
			SmokeColor = new(183, 220, 201);
			ParticleOne = new(133, 255, 146);
			ParticleTwo = Color.White;
		}
	}
}

[AutoloadMinionBuff()]
[AutoloadGlowmask("255, 255, 255", false)]
public class JellyfishMinion : BaseMinion
{
	private const int CHARGE_TIME = 90; // Time it takes to charge
	private const int MAX_ATTACK_TIMER = 25;
	private const int MAX_DASH_TIMER = 25;

	public static int SHOOT_RANGE { get; set; } = 400; //Static because it's used by the bolt class

	public static readonly Asset<Texture2D> BaseTexture = DrawHelpers.RequestLocal<JellyfishMinion>("JellyfishMinion", false);
	public static readonly Asset<Texture2D> OutlineTexture = DrawHelpers.RequestLocal<JellyfishMinion>("JellyfishMinion_Outline", false);
	public static readonly Asset<Texture2D> AttackTexture = DrawHelpers.RequestLocal<JellyfishMinion>("JellyfishMinion_Attack", false);
	public static readonly Asset<Texture2D> AttackAura = DrawHelpers.RequestLocal<JellyfishMinion>("JellyfishMinion_AttackAura", false);

	public JellyfishMinion() : base(600, 800, new Vector2(28, 28)) { }

	public bool IsPink = false;
	public bool IsGreen = false;
	private ref float AiTimer => ref Projectile.ai[0];
	private ref float AttackTimer => ref Projectile.ai[1];
	private ref float ChargeTimer => ref Projectile.ai[2];

	private int attackFrame;
	private int dashTimer; // used for drawing after images

	private JellyfishMinionColors JellyColors;

	public override void AbstractSetStaticDefaults()
	{
		Main.projFrames[Type] = 4;
		ProjectileID.Sets.TrailCacheLength[Type] = 10;
		ProjectileID.Sets.TrailingMode[Type] = 2;
	}

	public override void OnSpawn(IEntitySource source)
	{
		IsPink = Main.rand.NextBool(3);

		if (Main.hardMode && Main.rand.NextBool(5))
		{
			IsPink = false;
			IsGreen = true;
		}

		Projectile.velocity += Main.rand.NextVector2Circular(3f, 3f);
		Projectile.netUpdate = true;

		JellyColors = new JellyfishMinionColors(IsPink, IsGreen);
	}

	public override void IdleMovement(Player player)
	{
		if (ChargeTimer > 0)
			ChargeTimer--;

		const int maxFloatDistance = 50 * 50;
		const int minDashDistance = 60 * 60;
		const int maxDistFromPlayer = 600 * 600;

		Vector2 idlePosition = player.Center;

		float playerDist = Vector2.DistanceSquared(idlePosition, Projectile.Center);

		Projectile.tileCollide = false;

		if (playerDist > minDashDistance)
		{
			// if really far, give them a push
			if (playerDist > maxDistFromPlayer - 360 * 360)
				Projectile.velocity += Projectile.DirectionTo(player.Center) * 0.3f;

			AiTimer++;

			if (AiTimer > 30)
			{
				Dash(Player.Center, MathHelper.Lerp(15f, 6f, 1f - playerDist / maxDistFromPlayer));

				AiTimer = -30 - Main.rand.Next(20);
			}
			else
			{
				Projectile.velocity *= 0.98f;
			}

			Projectile.rotation = Utils.AngleLerp(Projectile.rotation, Projectile.velocity.ToRotation() + MathHelper.PiOver2, 0.05f);
		}
		else if (AiTimer > 0 && playerDist > maxFloatDistance)
		{
			Vector2 toIdlePosition = idlePosition - Projectile.Center;

			if (toIdlePosition.Length() < 0.0001f)
				toIdlePosition = Vector2.Zero;
			else
			{
				float speed = MathHelper.Lerp(4f, 1f, 1f - playerDist / minDashDistance);

				toIdlePosition.Normalize();
				toIdlePosition *= speed;
			}

			Projectile.velocity = (Projectile.velocity * 15f + toIdlePosition) / 16f;

			Projectile.rotation = Utils.AngleLerp(Projectile.rotation, Projectile.velocity.ToRotation() + MathHelper.PiOver2, 0.05f);
		}
		else
		{
			if (playerDist < 400)
				Projectile.velocity += Main.rand.NextVector2CircularEdge(0.25f, 0.25f) + -Vector2.UnitY * 0.3f;

			AiTimer++;

			Projectile.velocity *= 0.95f;

			Projectile.rotation = Utils.AngleLerp(Projectile.rotation, 0, 0.15f);

			if (player.Center.Y - 20 < Projectile.Center.Y) // only bounce when below slightly above the players head to avoid bouncing to high up
			{
				Projectile.velocity.Y += 0.02f;

				if (AiTimer % 30 == 0)
					Projectile.velocity.Y = -Main.rand.NextFloat(0.4f, 0.6f);
			}
			else
				Projectile.velocity.Y += 0.01f;
		}

		if (playerDist > maxDistFromPlayer)
		{
			Projectile.Center = player.Center;
			Projectile.netUpdate = true;
		}
	}

	public override void TargettingBehavior(Player player, NPC target)
	{
		const float chargeDist = 350 * 350; // how close it has to be to start charging its attack
		const float attackDist = 200 * 200; // how close it has to be to actually attack
		const float minFloatDist = 250 * 250; // how close it has to be to float towards a target
		const float maxFloatDist = 80 * 80; // the closest it can get to continue floating

		float targetDist = Projectile.DistanceSQ(target.Center);

		Projectile.tileCollide = false;

		if (targetDist < chargeDist)
			if (ChargeTimer <= CHARGE_TIME)
				ChargeTimer++;
		else
			ChargeTimer--;

		if (targetDist > minFloatDist)
		{
			if (targetDist > minFloatDist - 50 * 50)
				Projectile.velocity += Projectile.DirectionTo(target.Center) * 0.3f;

			AiTimer++;

			if (AiTimer > 30)
			{
				Dash(target.Center, 9f);

				AiTimer = -20 - Main.rand.Next(20);
			}
			else
			{
				Projectile.velocity *= 0.97f;
			}

			Projectile.rotation = Utils.AngleLerp(Projectile.rotation, Projectile.velocity.ToRotation() + MathHelper.PiOver2, 0.05f);
		}
		else
		{
			Projectile.velocity *= 0.98f;

			Projectile.rotation = Utils.AngleLerp(Projectile.rotation, 0, 0.15f);

			if (targetDist > maxFloatDist)
				Projectile.velocity += Projectile.DirectionTo(target.Center) * 0.15f;
			else
			{
				Projectile.velocity *= 0.95f;

				Projectile.velocity.Y += 0.03f;

				if (ChargeTimer % 30 == 0)
					Projectile.velocity.Y = -Main.rand.NextFloat(0.4f, 0.6f);
			}
				
			if (AiTimer > 0)
				AiTimer = 0;
		}

		if (ChargeTimer >= CHARGE_TIME && targetDist < attackDist)
		{
			Vector2 aimDirection = Projectile.DirectionTo(target.Center);
			float ySpeed = 1f;
			float xSpeed = 0.3f;

			AttackTimer = MAX_ATTACK_TIMER;

			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/ElectricZap") with { Pitch = -.55f, Volume = .55f, MaxInstances = 3 }, Projectile.Center);
			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/ElectricZap2") with { Pitch = -.65f, Volume = .35f, MaxInstances = 3 }, Projectile.Center);

			for (int i = 0; i < 5; i++)
			{
				Vector2 vel = Main.rand.NextVector2Circular(6f, 6f);
				Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(5f, 5f);
				ParticleHandler.SpawnParticle(new BloomParticle(pos, vel, JellyColors.ParticleOne.Additive() * 0.5f, 0.2f, 90, extraUpdateAction: DecelerateAction));
				ParticleHandler.SpawnParticle(new GlowParticle(pos, vel, JellyColors.ParticleTwo.Additive(), 0.2f, 90, extraUpdateAction: DecelerateAction));

				static void DecelerateAction(Particle p) => p.Velocity *= 0.93f;

				ParticleHandler.SpawnParticle(new LightningBoltParticle(Projectile.Center, Main.rand.NextVector2Circular(5f, 5f), JellyColors.LightningStart, JellyColors.LightningEnd.Additive(), 0f, 0.6f, 40));
			}

			if (Projectile.owner == Main.myPlayer)
			{
				int color = IsPink ? 1 : 0;
				if (IsGreen)
					color = 2;

				PreNewProjectile.New(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<JellyfishBolt>(), Projectile.damage, Projectile.knockBack, Projectile.owner, target.whoAmI, color, 0);

				Projectile.netUpdate = true;
			}

			Projectile.velocity = 0.66f * new Vector2(-xSpeed * aimDirection.X, -ySpeed);
			Projectile.velocity -= aimDirection * 3f;

			ChargeTimer = 0 - Main.rand.Next(20); //slight randomization here so minions dont sync up too hard

			if (player.HasMinionAttackTargetNPC && _targetNPC.whoAmI != player.MinionAttackTargetNPC)
				_targetNPC = null; // can retarget into players minion target between shots
		}
	}

	void Dash(Vector2 targetPosition, float strength = 10f, float randomization = 0.15f)
	{
		Projectile.velocity = Projectile.DirectionTo(targetPosition).RotatedByRandom(randomization) * Main.rand.NextFloat(strength, strength * 1.25f);
		Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

		dashTimer = MAX_DASH_TIMER;

		SoundEngine.PlaySound(SoundID.SplashWeak with { PitchVariance = 0.3f }, Projectile.Center);

		for (int i = 0; i < 3; i++)
		{
			ParticleHandler.SpawnParticle(new BubbleParticle(Projectile.Center + Main.rand.NextVector2Circular(15f, 15f), -Projectile.velocity * 0.2f, Main.rand.NextFloat(0.12f, 0.26f), Main.rand.Next(20, 40)));

			Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(15f, 15f), DustID.Water, -Projectile.velocity * 0.2f, 55, default, 0.7f).noGravity = true;
		}
	}

	public override bool DoAutoFrameUpdate(ref int framespersecond, ref int startframe, ref int endframe)
	{
		framespersecond = 8;
		return true;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		var texture = BaseTexture.Value;
		var outlineTexture = OutlineTexture.Value;

		var attackTexture = AttackTexture.Value;
		var attackAura = AttackAura.Value;

		Color drawColor = JellyColors.BaseColor;
		Color auraColor = JellyColors.AuraBase;
		Color auraColorLight = JellyColors.AuraLight;

		if (dashTimer > 0)
		{
			float fadeOut = dashTimer / (float)MAX_DASH_TIMER;

			float count = Projectile.oldPos.Length;

			for (int i = 0; i < (int)count; i++)
			{
				float lerp = 1f - i / count;

				var frame = texture.Frame(1, 4, 0, Projectile.frame);

				Main.spriteBatch.Draw(texture, Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition, frame, JellyColors.SmokeColor.Additive() * 0.2f * lerp * fadeOut, Projectile.rotation, frame.Size() / 2f, Projectile.scale, 0f, 0f);
			}
		}

		if (AttackTimer > 0)
		{
			float fadeOut = 1f;
			if (AttackTimer < 10)
				fadeOut = AttackTimer / 10f;

			Vector2 shake = Main.rand.NextVector2CircularEdge(0.66f, 0.66f) * fadeOut;

			var frame = attackAura.Frame(1, 3, 0, attackFrame);

			Vector2 pos = Projectile.Center + shake - Main.screenPosition;

			Main.spriteBatch.Draw(attackAura, pos + new Vector2(0, -8).RotatedBy(Projectile.rotation), frame, auraColor * fadeOut, Projectile.rotation, frame.Size() / 2f, Projectile.scale, 0f, 0f);
			Main.spriteBatch.Draw(attackAura, pos + new Vector2(0, -8).RotatedBy(Projectile.rotation), frame, auraColorLight.Additive() * fadeOut, Projectile.rotation, frame.Size() / 2f, Projectile.scale, 0f, 0f);

			frame = attackTexture.Frame(1, 3, 0, attackFrame);

			Main.spriteBatch.Draw(attackTexture, pos, frame, drawColor, Projectile.rotation, frame.Size() / 2f, Projectile.scale, 0f, 0f);
			Main.spriteBatch.Draw(attackTexture, pos, frame, drawColor.Additive(), Projectile.rotation, frame.Size() / 2f, Projectile.scale, 0f, 0f);
			
			Main.spriteBatch.Draw(attackTexture, pos, frame, auraColorLight.Additive() * 0.5f * fadeOut, Projectile.rotation, frame.Size() / 2f, Projectile.scale, 0f, 0f);

			return false;
		}

		var texFrame = texture.Frame(1, 4, 0, Projectile.frame);

		Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, texFrame, drawColor, Projectile.rotation, texFrame.Size() / 2f, Projectile.scale, 0f, 0f);
		Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, texFrame, drawColor.Additive(), Projectile.rotation, texFrame.Size() / 2f, Projectile.scale, 0f, 0f);

		if (ChargeTimer > 0)
		{
			float fadeIn = ChargeTimer / CHARGE_TIME;

			var frame = outlineTexture.Frame(1, 4, 0, Projectile.frame);

			Main.spriteBatch.Draw(outlineTexture, Projectile.Center - Main.screenPosition, frame, auraColorLight.Additive() * fadeIn, Projectile.rotation, frame.Size() / 2f, Projectile.scale, 0f, 0f);
		}

		return false;
	}

	public override void PostAI()
	{
		if (AttackTimer > 0)
		{
			AttackTimer--;
			if (AttackTimer % 5 == 0)
			{
				if (++attackFrame > 2)
					attackFrame = 0;
			}	
		}

		if (dashTimer > 0)
			dashTimer--;

		Lighting.AddLight(Projectile.Center, JellyColors.BaseColor.ToVector3() * .25f);

		if (_targetNPC is not null && AttackTimer <= 0)
		{
			float fadeIn = AiTimer / CHARGE_TIME;

			Lighting.AddLight(Projectile.Center, JellyColors.AuraBase.ToVector3() * fadeIn * 0.33f);
		}

		foreach (var p in Main.ActiveProjectiles) //Avoid grouping up
		{
			if (p.whoAmI != Projectile.whoAmI && p.type == Projectile.type && p.owner == Projectile.owner && p.Hitbox.Intersects(Projectile.Hitbox))
				Projectile.velocity += Projectile.DirectionFrom(p.Center) / 20;
		}
	}

	public override bool MinionContactDamage() => false;

	public override void SendExtraAI(BinaryWriter writer) => writer.Write(IsPink);
	public override void ReceiveExtraAI(BinaryReader reader) => IsPink = reader.ReadBoolean();
}