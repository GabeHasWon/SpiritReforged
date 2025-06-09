using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Particles;
using static SpiritReforged.Common.Easing.EaseFunction;
using static Microsoft.Xna.Framework.MathHelper;
using System.IO;

namespace SpiritReforged.Content.Desert.Scarabeus.Items.Projectiles;

[AutoloadGlowmask("255,255,255", false)]
public class SunOrb : ModProjectile
{
	private const int GROWTIME = 30;
	private const int FLASHTIME = 70;
	private const int NUMHITS = 4;

	private const float CAN_HIT_THRESHOLD = 0.66f; //What % of the flash progress is needed to actually do damage

	private float AiTimer { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }
	private float FlashTimer { get => Projectile.ai[1]; set => Projectile.ai[1] = value; }
	private float ParentProjID => Projectile.ai[2];

	private Vector2 _offset;
	private Vector2 _mousePos;
	private Vector3 _rayScale;

	private bool _stoppedChannel = false;
	private bool _initialized = false;

	public override bool IsLoadingEnabled(Mod mod) => false;

	public override void SetDefaults()
	{
		Projectile.width = 34;
		Projectile.height = 34;
		Projectile.DamageType = DamageClass.Magic;
		Projectile.friendly = true;
		Projectile.penetrate = -1;
		Projectile.aiStyle = 0;
		Projectile.scale = 0;
		Projectile.ignoreWater = true;
		Projectile.tileCollide = false;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = (int)((FLASHTIME / NUMHITS) * (1 - CAN_HIT_THRESHOLD)) + 1;
	}

	public override bool? CanCutTiles() => false;

	public override void AI()
	{
		Projectile.TryGetOwner(out Player owner);
		Projectile.timeLeft = 2;

		if (owner.dead || !IsParentActive(out SunStaffHeld sunStaff) && !_stoppedChannel)
		{
			Projectile.Kill();
			return;
		}

		if (!_initialized)
		{
			Projectile.netUpdate = true;
			_offset = Projectile.Center - owner.Center;
			_initialized = true;
		}

		//Only change the stored mouse position on the owner's client- and only do it if the player is still channeling
		if(Main.myPlayer == owner.whoAmI && !_stoppedChannel)
		{
			_mousePos = Vector2.Lerp(_mousePos, Main.MouseWorld - owner.Center, 0.07f);
			Projectile.netUpdate = true;
		}

		//Move the orb slightly towards the mouse so you can control it a little
		Vector2 mouseOffset = Vector2.Normalize(_mousePos) * Min(_mousePos.Length() / 5, 40) * new Vector2(0.5f, 1f);

		Projectile.Center = Vector2.Lerp(Projectile.Center, owner.MountedCenter + _offset + owner.velocity + mouseOffset, 0.2f);

		GrowShrink();
		GetRayDimensions(out float rayHeight, out _, out float rayDist);

		float earlyChannelStopTime = GROWTIME * 0.8f;
		if (!owner.channel && !_stoppedChannel && AiTimer >= earlyChannelStopTime)
		{
			_stoppedChannel = true;
			Projectile.netUpdate = true;
			FlashTimer = FLASHTIME;
			sunStaff.StartStaffLowering();
		}

		//Create light at the sun's position and following the ray
		float lightStrength = Projectile.scale / 2 + GetFlashProgress / 2;
		Lighting.AddLight(Projectile.Center, Color.Goldenrod.ToVector3() * lightStrength);
		float numLight = 10;
		for(int i = 0; i < numLight; i++)
		{
			Vector2 pos = new Vector2(rayDist, rayHeight) * i / numLight;
			Lighting.AddLight(Projectile.Center + pos, Color.Goldenrod.ToVector3() * lightStrength * 0.5f);
		}

		AiTimer++;
	}

	private bool IsParentActive(out SunStaffHeld sunStaff)
	{
		sunStaff = null;
		Projectile parent = Main.projectile[(int)ParentProjID];
		if (!parent.active || parent.ModProjectile == null)
			return false;

		if(parent.ModProjectile is SunStaffHeld staff)
		{
			sunStaff = staff;
			return true;
		}

		return false;
	}

	private void GrowShrink()
	{
		float progress;
		if (!_stoppedChannel) //Grow if still channeling
		{
			progress = AiTimer / GROWTIME;
			progress = Min(progress, 1);

			Projectile.scale = MultistepEase(EaseCircularIn, EaseOutBack(2), 0.2f).Ease(progress);
			_rayScale = new Vector3(EaseQuadOut.Ease(Min(progress, 1)) * Projectile.scale);
			_rayScale.X *= Projectile.scale;
		}
		else
		{
			FlashTimer = Max(FlashTimer - 1, 0);
			progress = GetFlashProgress;
			progress = Max(progress, 0);
			if (progress == 0)
				Projectile.Kill();

			Projectile.scale = MultistepEase(EaseCubicIn, EaseCubicOut, 0.2f).Ease(progress) * Lerp(1, 1.3f, progress);
			_rayScale = new Vector3(EaseQuadOut.Ease(Min(Projectile.scale, 1)));
			_rayScale.X *= Lerp(0.5f, 1.2f, EaseCircularIn.Ease(progress)) * Projectile.scale;
		}
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (Main.dedServ)
			return;

		ParticleHandler.SpawnParticle(new LightBurst(target.Center, Main.rand.NextFloatDirection(), Color.LightGoldenrodYellow, 0.6f, 30));

		int numSmoke = 6;
		for (int i = 0; i < numSmoke; i++)
		{
			Color smokeColor = Color.LightGoldenrodYellow.Additive() * 0.3f;
			float progress = i / (float)numSmoke;
			float scale = Lerp(0.08f, 0.04f, progress);
			var vel = Vector2.Lerp(-Vector2.UnitY, -Vector2.UnitY * 4, progress);
			ParticleHandler.SpawnParticle(new SmokeCloud(target.Center, vel, smokeColor, scale, EaseQuadOut, 40, false));
		}

		for (int i = 0; i < 6; i++)
		{
			Vector2 particleCenter = target.Center + Main.rand.NextVector2Circular(15, 15);
			Vector2 particleVel = -Vector2.UnitY.RotatedByRandom(PiOver4) * Main.rand.NextFloat(1, 4);
			Color lightColor = Color.LightGoldenrodYellow.Additive();
			Color darkColor = Color.Lerp(Color.LightGoldenrodYellow, Color.OrangeRed, 0.5f).Additive() * 0.5f;
			float scale = Main.rand.NextFloat(0.6f, 1f);
			int lifeTime = Main.rand.Next(20, 40);
			static void delegateAction(Particle p)
			{
				p.Velocity = p.Velocity.RotatedByRandom(0.1f);
				p.Velocity *= 0.97f;
				p.Velocity.X *= 0.92f;
			}

			ParticleHandler.SpawnParticle(new GlowParticle(particleCenter, particleVel, lightColor, darkColor, scale, lifeTime, 5, delegateAction));
		}
	}

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		if (GetFlashProgress < CAN_HIT_THRESHOLD)
			return false;

		GetRayDimensions(out float rayHeight, out float rayWidth, out float rayDist);
		var tip = Projectile.Center.ToPoint();
		int numCalcs = 20;

		//This def can be done more efficiently but for now this just uses a summation to estimate the hitbox
		for(int i = 0; i < numCalcs; i++)
		{
			float progress = i / (float)numCalcs;
			int curHeight = (int)(progress * rayHeight);
			int curWidth = (int)Lerp(Projectile.width, rayWidth, progress);
			int curPosOffset = (int)(progress * rayDist);
			int rectangleHeight = (int)(rayHeight / numCalcs);

			var collisionLine = new Rectangle(tip.X - (curWidth / 2) + curPosOffset, tip.Y + curHeight, curWidth, rectangleHeight);
			if (collisionLine.Intersects(targetHitbox))
				return true;
		}

		return false;
	}

	private void GetRayDimensions(out float rayHeight, out float rayWidth, out float rayDist)
	{
		Projectile.TryGetOwner(out Player owner);
		Vector2 projCenter = Projectile.Center - owner.Center;
		float mouseAngle = projCenter.AngleTo(_mousePos) + Pi;

		//Constrain the angle to be downwards from the sun- can't ever be exactly horizontal, because the below while loop would be infinite
		float minAngle = -0.01f;
		if (mouseAngle < PiOver2)
			mouseAngle = minAngle;
		else if (mouseAngle < Pi)
			mouseAngle = Pi + minAngle;
		mouseAngle += Pi;

		float maxDist = 230f;
		//Set the ray's height to be at least reach the player from the base offset- then extend it based on mouse position, up to the set max distance
		rayHeight = -Min(_offset.Y, Max(-_mousePos.Y + _offset.Y, -maxDist));

		//Get a distance unit used for calculating how far the beam goes- then extend it until that distance unit matches the beam's height
		Vector2 mouseDirection = mouseAngle.ToRotationVector2() * maxDist;
		while (mouseDirection.Y < -rayHeight)
			mouseDirection += mouseAngle.ToRotationVector2();

		//Constrain the ray's distance
		rayDist = Clamp(mouseDirection.X, -maxDist, maxDist);

		//Scale the width based on how far the ray is from the player
		var widthRange = new Vector2(100, 200);
		rayWidth = Lerp(widthRange.X, widthRange.Y, Math.Abs(rayDist) / maxDist);
	}

	private float GetFlashProgress => FlashTimer / FLASHTIME;

	public override bool PreDraw(ref Color _)
	{
		float strength(float lerpAmount) => Lerp(GetFlashProgress, 1, lerpAmount);
		Color lightColor(byte alpha = 100, float lerpAmount = 0.25f) => Color.LightGoldenrodYellow.Additive(alpha) * strength(lerpAmount);
		Color darkColor(float lerpAmount = 0.25f) => new Color(250, 167, 32, 200) * strength(lerpAmount);

		DrawStar(lightColor(), darkColor());
		DrawBigRay(Color.Lerp(lightColor(), darkColor(1), GetFlashProgress / 5), darkColor());
		DrawSun(lightColor(200, 1), darkColor(0.5f));

		return false;
	}

	//Beware: Hyper specific shader parameter setting with 1 billion different easings below!
	private void DrawBigRay(Color rayColor, Color darkRayColor)
	{
		Effect effect = AssetLoader.LoadedShaders["LightRay"];
		GetRayDimensions(out float rayHeight, out float rayWidth, out float rayDist);

		effect.Parameters["uTexture"].SetValue(AssetLoader.LoadedTextures["FlameTrail"].Value);
		float scrollAmount = EaseCircularIn.Ease(GetFlashProgress) * 0.4f;
		effect.Parameters["scroll"].SetValue(new Vector2(0, scrollAmount));
		effect.Parameters["textureStretch"].SetValue(new Vector2(4, 1) * 0.05f);
		effect.Parameters["texExponentRange"].SetValue(new Vector2(1, 0.25f));
		effect.Parameters["flipCoords"].SetValue(true);

		float easedScale = EaseCircularIn.Ease(Clamp(Projectile.scale, 0, 1));
		float easedFlashProgress = EaseCubicIn.Ease(GetFlashProgress);
		effect.Parameters["finalIntensityMod"].SetValue(1 * (1 + easedFlashProgress/2) * easedScale * Projectile.scale);
		effect.Parameters["textureStrength"].SetValue(easedFlashProgress); //Don't display texture while not flashing
		effect.Parameters["finalExponent"].SetValue(2f);

		effect.Parameters["uColor"].SetValue(rayColor.ToVector4());

		effect.Parameters["uColor2"].SetValue(darkRayColor.ToVector4());

		var rayVisualStretch = new Vector3(2.5f, 1.3f, 1.15f); //Stretch the primitive to better fit the actual hitbox, since shader makes it innaccurate
		rayVisualStretch *= _rayScale;

		var rayFinalDimensions = new Vector3(rayWidth * rayVisualStretch.X, rayHeight * rayVisualStretch.Y, rayDist * rayVisualStretch.Z);

		float sunWidth = 30 * Projectile.scale * Lerp(_rayScale.X, 1, 0.5f);
		effect.Parameters["taperRatio"].SetValue(sunWidth / rayFinalDimensions.X);

		var square = new SquarePrimitive
		{
			Color = Color.White,
			Height = rayFinalDimensions.Y,
			Length = rayFinalDimensions.X,
			BottomPosOffset = rayFinalDimensions.Z
		};

		square.SetTopPosition(Projectile.Center - Main.screenPosition);

		PrimitiveRenderer.DrawPrimitiveShape(square, effect);
	}

	private void DrawSun(Color lightColor, Color darkRayColor)
	{ 
		Effect effect = AssetLoader.LoadedShaders["SunOrb"];
		effect.Parameters["lightColor"].SetValue(lightColor.ToVector4());
		effect.Parameters["darkColor"].SetValue(darkRayColor.ToVector4());
		effect.Parameters["uTexture"].SetValue(AssetLoader.LoadedTextures["Extra_49"].Value);
		effect.Parameters["intensity"].SetValue((1.5f + EaseCircularIn.Ease(GetFlashProgress)) * EaseQuadOut.Ease(Projectile.scale));

		//Subtle swirly noise around the main orb- unneccessary flourish but I like it
		float time = AiTimer / 60f;
		effect.Parameters["noiseTexture"].SetValue(AssetLoader.LoadedTextures["swirlNoise"].Value);
		effect.Parameters["scroll"].SetValue(new Vector2(-time / 6, -time / 2));
		effect.Parameters["textureStretch"].SetValue(new Vector2(1, 0.5f));

		//Godrays around the orb, intensity dramatically increases when the orb flashes
		effect.Parameters["rayTexture"].SetValue(AssetLoader.LoadedTextures["vnoise"].Value);
		effect.Parameters["rayScroll"].SetValue(new Vector2(time / 6, -3f * time + EaseCircularIn.Ease(GetFlashProgress) * 2.5f));
		effect.Parameters["rayStretch"].SetValue(new Vector2(1, 0.03f));
		float rayIntensity = Max(3 * EaseCircularIn.Ease(EaseQuadIn.Ease(GetFlashProgress)), 0.15f) * EaseCircularIn.Ease(Min(Projectile.scale, 1));
		effect.Parameters["rayIntensity"].SetValue(rayIntensity);

		var square = new SquarePrimitive
		{
			Color = Color.White,
			Height = 60 * Projectile.scale,
			Length = 60 * Projectile.scale,
			Position = Projectile.Center - Main.screenPosition
		};

		PrimitiveRenderer.DrawPrimitiveShape(square, effect);
	}

	private void DrawStar(Color lightColor, Color darkColor)
	{
		Texture2D starTex = AssetLoader.LoadedTextures["Star"].Value;
		var center = Projectile.Center - Main.screenPosition - new Vector2(Projectile.scale);
		float maxSize = 0.6f * Projectile.scale;
		float easedFlashProgress = EaseCircularIn.Ease(GetFlashProgress);

		Vector2 scale = new Vector2(1f, 0.5f) * Lerp(0, maxSize, easedFlashProgress);
		var origin = starTex.Size() / 2;
		Color color = Projectile.GetAlpha(Color.Lerp(lightColor, darkColor, easedFlashProgress).Additive());
		Main.spriteBatch.Draw(starTex, center, null, color, 0, origin, scale, SpriteEffects.None, 0);
	}

	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => overPlayers.Add(index);

	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		if (target.type is NPCID.Vampire or NPCID.VampireBat)
			modifiers.SourceDamage *= 100f; //silly
	}

	public override void SendExtraAI(BinaryWriter writer)
	{
		writer.WriteVector2(_offset);
		writer.WriteVector2(_mousePos);
		writer.Write(_stoppedChannel);
		writer.Write(_initialized);
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
		_offset = reader.ReadVector2();
		_mousePos = reader.ReadVector2();
		_stoppedChannel = reader.ReadBoolean();
		_initialized = reader.ReadBoolean();
	}
}