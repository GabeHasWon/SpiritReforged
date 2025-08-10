using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Content.Ocean.Items.Reefhunter.Particles;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Ocean.Items.Reefhunter.Projectiles;

public class Cannonbubble : ModProjectile
{
	public const float MAX_SPEED = 15f; //Initial projectile velocity, used in item shootspeed
	private const int MAX_TIMELEFT = 360;
	public static Color RINGCOLOR { get; set; } = new Color(202, 252, 255, 200) * 0.5f;

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
		ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
	}

	public override void SetDefaults()
	{
		Projectile.width = 24;
		Projectile.height = 24;
		Projectile.DamageType = DamageClass.Ranged;
		Projectile.friendly = true;
		Projectile.penetrate = 5;
		Projectile.timeLeft = MAX_TIMELEFT;
		Projectile.aiStyle = 0;
		Projectile.scale = Main.rand.NextFloat(0.9f, 1.1f);
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = 30;
	}

	private ref float JiggleStrength => ref Projectile.ai[0];
	private ref float JiggleTime => ref Projectile.ai[1];

	public override void AI()
	{
		Projectile.velocity *= 0.975f;
		Projectile.rotation = Projectile.velocity.ToRotation();

		const float JiggleDecay = 0.99f; //Exponentially slows down in speed
		JiggleStrength *= JiggleDecay;
		JiggleTime += JiggleStrength;

		const float heightDeviation = 0.25f;
		const float sinePeriod = 100; //1.66 seconds
		Projectile.position.Y += (float)Math.Sin(MathHelper.TwoPi * Projectile.timeLeft / sinePeriod) * heightDeviation;

		if (Projectile.wet)
			Projectile.velocity.Y -= 0.08f;

		foreach (var p in Main.ActiveProjectiles)
		{
			if (p.type == Projectile.type && p.whoAmI != Projectile.whoAmI && p.Hitbox.Intersects(Projectile.Hitbox))
				BubbleCollision(p);
		}

		Projectile.TryShimmerBounce();
	}

	public override bool PreDraw(ref Color lightColor)
	{
		var outline = TextureAssets.Projectile[Projectile.type].Value;
		Vector2 squishScale = BubbleSquishScale(0.35f, 0.1f);
		List<SquarePrimitive> bubbleTrail = [];
		var primDimensions = new Vector2(Projectile.width * squishScale.X, -Projectile.height * squishScale.Y);

		for (int i = ProjectileID.Sets.TrailCacheLength[Projectile.type] - 1; i > 0; i--)
		{
			float progress = 1 - i / (float)ProjectileID.Sets.TrailCacheLength[Projectile.type];
			float trailOpacity = progress * GetSpeedRatio(2);

			var square = new SquarePrimitive()
			{
				Color = lightColor * trailOpacity,
				Height = primDimensions.X * progress,
				Length = primDimensions.Y * progress,
				Position = Projectile.oldPos[i] + Projectile.Size/2 - Main.screenPosition,
				Rotation = MathHelper.TwoPi + MathHelper.PiOver2 + Projectile.oldRot[i]
			};
			bubbleTrail.Add(square);
		}

		bubbleTrail.Add(new SquarePrimitive()
		{
			Color = lightColor,
			Height = primDimensions.X,
			Length = primDimensions.Y,
			Position = Projectile.Center - Main.screenPosition,
			Rotation = MathHelper.TwoPi + MathHelper.PiOver2 + Projectile.rotation
		});

		Effect bubbleEffect = AssetLoader.LoadedShaders["TextureMap"].Value;
		bubbleEffect.Parameters["uTexture"].SetValue(outline);
		bubbleEffect.Parameters["rotation"].SetValue(MathHelper.TwoPi + Projectile.rotation);
		PrimitiveRenderer.DrawPrimitiveShapeBatched(bubbleTrail.ToArray(), bubbleEffect);

		return false;
	}

	//Find the scale vector by which to draw the bubble's outline and interior with
	private Vector2 BubbleSquishScale(float velDelta, float jiggleDelta)
	{
		float squishAmount = GetSpeedRatio() * velDelta; //velocity based
		const float sineSpeed = 5 * MathHelper.Pi;
		squishAmount += (float)Math.Sin(sineSpeed * JiggleTime / 60) * jiggleDelta * JiggleStrength; //jiggling based

		float timeModifier = Projectile.timeLeft / (float)MAX_TIMELEFT;
		timeModifier = 1f - (float)Math.Pow(1 - timeModifier, 30) / 2;
		return new Vector2(1 + squishAmount, 1 - squishAmount) * Projectile.scale * timeModifier;
	}

	private float GetSpeedRatio(float exponent = 1) => (float)Math.Pow(MathHelper.Min(Projectile.velocity.Length() / MAX_SPEED, 1), exponent);

	public override void OnKill(int timeLeft)
	{
		if (Main.dedServ)
			return;

		int dustCount = Main.rand.Next(7, 12);

		SoundEngine.PlaySound(SoundID.Item54, Projectile.Center);
		SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/Impact_LightPop") with { PitchVariance = 0.4f, Pitch = 1.3f, Volume = .6f, MaxInstances = 10 }, Projectile.Center);

		//ParticleHandler.SpawnParticle(new BubblePop(Projectile.Center, Projectile.scale * 0.35f, 0.8f, 30));
		ParticleHandler.SpawnParticle(new PulseCircle(Projectile.Center, RINGCOLOR, RINGCOLOR * 0.5f, 0.3f, 150 * Projectile.scale, 50, EaseFunction.EaseCircularOut).WithSkew(0.85f, -MathHelper.PiOver2).UsesLightColor());

		for(int i = -1; i <= 1; i += 2)
			ParticleHandler.SpawnParticle(new PulseCircle(Projectile.Center, RINGCOLOR, RINGCOLOR * 0.5f, 0.3f, 80 * Projectile.scale, 40, EaseFunction.EaseCircularOut).WithSkew(Main.rand.NextFloat(), Main.rand.NextFloat(MathHelper.TwoPi)).UsesLightColor());

		for (int i = 0; i < 2; i++)
			ParticleHandler.SpawnParticle(new BubbleParticle(Projectile.Center, Main.rand.NextVec2CircularEven(1, 1), Main.rand.NextFloat(0.3f, 0.4f), 30));

		for (int i = 0; i < 6; i++) 
			ParticleHandler.SpawnParticle(new BubbleParticle(Projectile.Center, Main.rand.NextVec2CircularEven(2, 2), Main.rand.NextFloat(0.1f, 0.2f), 40));
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		NPCCollision(target, 3, 0.5f, 2f);
		OnCollideExtra();
	}

	private void NPCCollision(Entity target, float stoppedSpeed, float lowAddedVelMod, float highAddedVelMod)
	{
		var lastTargetHitboxX = new Rectangle((int)(target.position.X - target.velocity.X), (int)target.position.Y, target.width, target.height);
		var lastTargetHitboxY = new Rectangle((int)target.position.X, (int)(target.position.Y - target.velocity.Y), target.width, target.height);
		var lastProjHitboxX = new Rectangle((int)(Projectile.position.X - Projectile.velocity.X), (int)Projectile.position.Y, Projectile.width, Projectile.height);
		var lastProjHitboxY = new Rectangle((int)Projectile.position.X, (int)(Projectile.position.Y - Projectile.velocity.Y), Projectile.width, Projectile.height);

		//Check for x collision by checking if the hitboxes intersect using the last tick's x positions, repeat for y collision using last tick's y positions
		//There's likely a more accurate method without detouring projectile npc collision? Not sure at the moment, working backwards from tile collision logic
		bool collideX = !lastTargetHitboxX.Intersects(lastProjHitboxX);
		bool collideY = !lastTargetHitboxY.Intersects(lastProjHitboxY);

		//Reverse velocity based on collision direction, 
		//add target velocity if it was in the opposite direction of projectile movement (ricocheting off of target), 
		//or if projectile velocity is slow enough (pushed by target)
		bool projStopped = Projectile.velocity.Length() < stoppedSpeed;
		if (collideX)
		{
			Projectile.velocity.X *= -1;

			if (projStopped)
				Projectile.velocity.X += target.velocity.X * highAddedVelMod;

			if (Math.Sign(target.velocity.X) == Math.Abs(Projectile.velocity.X))
				Projectile.velocity.X += target.velocity.X * lowAddedVelMod;
		}

		if (collideY)
		{
			Projectile.velocity.Y *= -1;

			if (projStopped)
				Projectile.velocity.Y += target.velocity.Y * highAddedVelMod;

			else if (Math.Sign(target.velocity.Y) == Math.Sign(Projectile.velocity.Y))
				Projectile.velocity.Y += target.velocity.Y * lowAddedVelMod;
		}

		Projectile.velocity = Projectile.velocity.RotatedBy(Main.rand.NextFloat(MathHelper.Pi / 8, MathHelper.Pi / 4) * (Main.rand.NextBool() ? -1 : 1));

		Projectile.netUpdate = true;
	}

	//Causes bubbles to ricochet off each other, and pushes them outside of each other if they overlap
	private void BubbleCollision(Projectile otherBubble)
	{
		var otherBubbleModProj = otherBubble.ModProjectile as Cannonbubble;
		while (otherBubble.Hitbox.Intersects(Projectile.Hitbox))
			Projectile.Center += Projectile.DirectionFrom(otherBubble.Center); //Push out if stuck inside

		//2 dimensional moving circle collision formula- via https://www.vobarian.com/collisions/
		//Simplified here: assuming all masses are equal, therefore normal scalar velocity post-collision is simply equal to the normal scalar velocity of the other vector pre-collision
		Vector2 normal = Projectile.DirectionFrom(otherBubble.Center);
		Vector2 tangent = normal.RotatedBy(MathHelper.PiOver2);
		float scalarTangentThis = Projectile.velocity.X * tangent.X + Projectile.velocity.Y * tangent.Y;
		float scalarTangentOther = otherBubble.velocity.X * tangent.X + otherBubble.velocity.Y * tangent.Y;
		float scalarNormalThis = Projectile.velocity.X * normal.X + Projectile.velocity.Y * normal.Y;
		float scalarNormalOther = otherBubble.velocity.X * normal.X + otherBubble.velocity.Y * normal.Y;

		Projectile.velocity = scalarTangentThis * tangent + scalarNormalOther * normal;
		otherBubble.velocity = scalarTangentOther * tangent + scalarNormalThis * normal;
		float speedMultiplier = Math.Max(GetSpeedRatio(0.75f), otherBubbleModProj.GetSpeedRatio(0.75f)); //Less dust at low collision speed
		if (speedMultiplier >= 0.2f)
			OnCollideExtra(speedMultiplier);

		(otherBubble.ModProjectile as Cannonbubble).JiggleStrength = 1;
	}

	private void OnCollideExtra(float strength = 1f)
	{
		JiggleStrength = 1;

		SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/Impact_LightPop") with { PitchVariance = 0.8f, Pitch = -1.2f, Volume = .5f, MaxInstances = 10 }, Projectile.Center);

		if (!Main.dedServ)
		{
			Color color = RINGCOLOR * EaseFunction.EaseCubicOut.Ease(strength);
			ParticleHandler.SpawnParticle(new PulseCircle(Projectile.Center, color, color * 0.5f, 0.6f, 100 * Projectile.scale * strength, 50, EaseFunction.EaseCircularOut).WithSkew(0.85f, Projectile.velocity.ToRotation() - MathHelper.Pi).UsesLightColor());
			SoundEngine.PlaySound(SoundID.Item54 with { PitchVariance = 0.3f, Volume = 0.5f * strength }, Projectile.Center);
		}
	}

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		Projectile.Bounce(oldVelocity);
		Projectile.penetrate--;
		OnCollideExtra();
		return false;
	}
}
