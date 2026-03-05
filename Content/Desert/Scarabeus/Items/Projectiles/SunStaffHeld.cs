using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Particles;
using System.IO;
using static Terraria.Player;

namespace SpiritReforged.Content.Desert.Scarabeus.Items.Projectiles;

[AutoloadGlowmask("255,255,255", false)]
public class SunStaffHeld : ModProjectile
{
	private const int RISE_TIME = 30;
	private const int FLASH_TIME = 20;
	
	private float AiTimer { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }
	private float RiseProgress { get => Projectile.ai[1]; set => Projectile.ai[1] = value; }
	private float FlashTimer { get => Projectile.ai[2]; set => Projectile.ai[2] = value; }

	private bool _stoppedChannel = false;

	public override bool IsLoadingEnabled(Mod mod) => false;

	public override void SetDefaults()
	{
		Projectile.width = 46;
		Projectile.height = 46;
		Projectile.DamageType = DamageClass.Magic;
		Projectile.friendly = true;
		Projectile.aiStyle = -1;
		Projectile.ignoreWater = true;
		Projectile.tileCollide = false;
		DrawHeldProjInFrontOfHeldItemAndArms = false;
	}

	public override bool? CanDamage() => false;

	public override void AI()
	{
		if(!Projectile.TryGetOwner(out Player owner) || owner.dead)
		{
			Projectile.Kill();
			return;
		}

		owner.heldProj = Projectile.whoAmI;
		owner.itemAnimation = 2;
		owner.itemTime = 2;
		Projectile.timeLeft = 2;
		if (owner.whoAmI == Main.myPlayer && !_stoppedChannel)
		{
			int tempDirection = owner.direction;
			owner.direction = Main.MouseWorld.X >= owner.MountedCenter.X ? 1 : -1;
			if(tempDirection != owner.direction && Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendData(MessageID.SyncPlayer, -1, -1, null, owner.whoAmI);
		}

		StaffMovement();

		int sunSpawnTime = RISE_TIME / 4;
		int sunOrbID = ModContent.ProjectileType<SunOrb>();
		if (AiTimer == sunSpawnTime && !_stoppedChannel)
		{
			Vector2 origin = owner.Center - Vector2.UnitY * 100;
			Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), origin, Vector2.Zero, sunOrbID, Projectile.damage, Projectile.knockBack, Projectile.owner, 0, 0, Projectile.whoAmI);
			FlashTimer = FLASH_TIME;
			Projectile.netUpdate = true;
		}

		//Failsafe if the sun orb despawns, somehow
		if (AiTimer > sunSpawnTime && !_stoppedChannel && owner.ownedProjectileCounts[sunOrbID] == 0)
			StartStaffLowering();

		Projectile.rotation = MathHelper.Lerp(-MathHelper.PiOver4, -3 * MathHelper.PiOver4, RiseProgress);
		if (owner.direction < 0)
			Projectile.rotation = MathHelper.Lerp(-MathHelper.PiOver4, -3 * MathHelper.PiOver4, 1 - RiseProgress) + MathHelper.Pi;

		owner.SetCompositeArmFront(true, CompositeArmStretchAmount.Full, Projectile.rotation);
		Projectile.Center = owner.GetFrontHandPosition(CompositeArmStretchAmount.Full, Projectile.rotation);

		AiTimer++;
		FlashTimer = Math.Max(--FlashTimer, 0);
	}

	private void StaffMovement()
	{
		float progress;
		if (!_stoppedChannel)
		{
			progress = AiTimer / RISE_TIME;
			progress = Math.Min(progress, 1);
		}
		else
		{
			progress = 1 - AiTimer / RISE_TIME;
			progress = Math.Max(progress, 0);
			if (progress == 0)
				Projectile.Kill();
		}

		RiseProgress = EaseFunction.EaseOutBack().Ease(progress);
	}

	//Called by the sun orb projectile- so both do their stopped channelling behavior together
	public void StartStaffLowering()
	{
		Projectile.TryGetOwner(out Player owner);
		_stoppedChannel = true;
		AiTimer = 0;
		Projectile.netUpdate = true;
		FlashTimer = FLASH_TIME; 
		
		for (int i = 0; i < 5; i++)
		{
			float particleCenterRot = Projectile.rotation - (owner.direction < 0 ? MathHelper.Pi : 0);
			Vector2 particleCenter = Projectile.Center + GetStaffTipPos(0.6f, particleCenterRot);
			particleCenter += Projectile.width / 2 * Vector2.UnitX * owner.direction;
			particleCenter += Main.rand.NextVector2Circular(5, 8);

			Vector2 particleVel = Main.rand.NextVector2Circular(1, 1) - Vector2.UnitY / 2;
			var particleColor = new Color(250, 167, 32, 0);
			float scale = Main.rand.NextFloat(0.6f, 0.8f);
			int lifeTime = Main.rand.Next(30, 40);
			static void delegateAction(Particle p)
			{
				p.Velocity.Y -= 0.07f * EaseFunction.EaseQuadOut.Ease(p.TimeActive / (float)p.MaxTime);
				p.Velocity.X *= 0.95f;
			}

			ParticleHandler.SpawnParticle(new GlowParticle(particleCenter, particleVel, particleColor, scale, lifeTime, 3, delegateAction).OverrideDrawLayer(ParticleLayer.AbovePlayer));
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Projectile.TryGetOwner(out Player owner);
		Texture2D staffTex = TextureAssets.Projectile[Type].Value;
		GlowmaskProjectile.ProjIdToGlowmask.TryGetValue(Type, out GlowmaskInfo staffGlowAsset);
		Texture2D staffGlow = staffGlowAsset.Glowmask.Value;
		Texture2D starTex = AssetLoader.LoadedTextures["Star"].Value;
		Texture2D bloomTex = AssetLoader.LoadedTextures["Bloom"].Value;

		//Calculations
		float strength = 0.5f;
		Color glowColor = new Color(250, 167, 32, 0) * strength;
		float flashProgress = FlashTimer / FLASH_TIME;
		float diagonalOrigin = 0.2f;
		Vector2 origin = staffTex.Size() * new Vector2(diagonalOrigin, 1 - diagonalOrigin);
		SpriteEffects flip = SpriteEffects.None;
		float rotationFlip = MathHelper.Lerp(Projectile.rotation, 0, 0.9f) - MathHelper.PiOver4;
		Vector2 staffTipPos = GetStaffTipPos(0.6f, rotationFlip - MathHelper.PiOver4);
		if (owner.direction < 0)
		{
			rotationFlip += MathHelper.PiOver2;
			flip = SpriteEffects.FlipHorizontally;
			origin = staffTex.Size() * new Vector2(1 - diagonalOrigin);
			staffTipPos = GetStaffTipPos(0.6f, rotationFlip + MathHelper.PiOver4 - MathHelper.Pi);
		}

		Vector2 staffPos = owner.GetFrontHandPosition(CompositeArmStretchAmount.Full, Projectile.rotation) - Main.screenPosition + Vector2.UnitY * owner.gfxOffY;
		float staffScale = MathHelper.Lerp(RiseProgress, 1, 0.33f);

		//Draw a star underneath the staff when it flashes after releasing m1, that rapidly decreases in scale
		if(_stoppedChannel)
		{
			Vector2 starScale = new Vector2(EaseFunction.EaseQuadOut.Ease(flashProgress), 0.5f) * flashProgress * 0.8f;
			float starRot = rotationFlip + MathHelper.PiOver4;

			if(owner.direction < 0)
				starRot += MathHelper.PiOver2;

			Main.spriteBatch.Draw(starTex, staffPos + staffTipPos, null, glowColor, starRot, starTex.Size() / 2, starScale, SpriteEffects.None, 0);
		}

		//Staff texture and glowmask
		Main.spriteBatch.Draw(staffTex, staffPos, null, lightColor, rotationFlip, origin, staffScale, flip, 1f);
		Main.spriteBatch.Draw(staffGlow, staffPos, null, glowColor * flashProgress, rotationFlip, origin, staffScale, flip, 1f);

		//Bloom-like glowmask drawing
		float numGlow = 6;
		float distance = MathHelper.Lerp(2, 4, flashProgress);
		float opacity = MathHelper.Lerp(1, 3, flashProgress) / numGlow;
		for (int i = 0; i < numGlow; i++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / numGlow) * distance;
			Main.spriteBatch.Draw(staffGlow, staffPos + offset, null, glowColor * opacity, rotationFlip, origin, staffScale, flip, 1f);
		}

		//Actual bloom above everything else while flashing
		if(_stoppedChannel)
			Main.spriteBatch.Draw(bloomTex, staffPos + staffTipPos, null, glowColor * EaseFunction.EaseQuadOut.Ease(flashProgress) * 2, 0, bloomTex.Size() / 2, 0.25f, SpriteEffects.None, 0);

		return false;
	}

	private Vector2 GetStaffTipPos(float distFromOrigin, float rotation) => Vector2.UnitX.RotatedBy(rotation) * (float)Math.Sqrt(2 * Math.Pow(Projectile.width, 2)) * distFromOrigin;

	public override void SendExtraAI(BinaryWriter writer) => writer.Write(_stoppedChannel);

	public override void ReceiveExtraAI(BinaryReader reader) => _stoppedChannel = reader.ReadBoolean();
}