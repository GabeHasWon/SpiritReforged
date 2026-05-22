using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items.Projectiles;

// This class serves as the death trail for any adorned bow projectiles
// We cannot keep the data on the projectile itself because the projectile dies, this is a weird workaround that kind of takes a projectile slot-
// But its better than messing with death logic
public class AdornedArrowDeathTrail : ModProjectile
{
	public const int TrailLength = 12;

	public override string Texture => AssetLoader.EmptyTexture;

	public Vector2[] oldPositions = new Vector2[TrailLength];
	public Color[] prismaticColors = new Color[3];
	public int arrowType;

	public override void SetDefaults()
	{
		Projectile.tileCollide = false;
		Projectile.timeLeft = 20;
	}

	public override void AI()
	{
		for (int i = TrailLength - 1; i > 0; i--)
			oldPositions[i] = oldPositions[i - 1];

		oldPositions[0] = Projectile.Center;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		//Partially adapted from JinxBow/JinxBowShot

		//Load texture if not already loaded
		Main.instance.LoadProjectile(ProjectileID.HallowBossRainbowStreak);
		Texture2D solid = TextureColorCache.ColorSolid(TextureAssets.Projectile[arrowType].Value, Color.LightSkyBlue);
		Texture2D rainbowTexture = TextureAssets.Projectile[ProjectileID.HallowBossRainbowStreak].Value;

		for (int i = TrailLength - 1; i >= 0; i--)
		{
			float lerp = 1f - i / (float)(TrailLength - 1);
			var position = oldPositions[i] - Main.screenPosition;
			var scale = new Vector2(.5f * lerp, 1) * Projectile.scale;

			float fadeOut = Projectile.timeLeft / 20f;
			Color fadeColor = AdornedBowGlobalProjectile.MulticolorLerp(fadeOut, prismaticColors) * fadeOut;

			var drawPos = Vector2.Lerp(position, oldPositions[0] - Main.screenPosition, 0.33f);
			Color drawColor = fadeColor * EaseFunction.EaseQuadIn.Ease(lerp) * 0.5f;
			Main.EntitySpriteDraw(solid, drawPos, null, drawColor, Projectile.rotation, solid.Size() / 2, new Vector2(Projectile.scale), SpriteEffects.None);

			Main.EntitySpriteDraw(rainbowTexture, position, null, fadeColor with { A = 0 }, Projectile.rotation, rainbowTexture.Size() / 2, scale, SpriteEffects.None);
		}

		return false;
	}
}

// global projectile for visuals and effects attached to power shot arrows
public class AdornedBowGlobalProjectile : GlobalProjectile
{
	public const int MAX_TRAIL_LENGTH = 12;
	public const int MAX_PRISMATIC_TIMER = 50;

	public override bool InstancePerEntity => true;

	public float PrismaticProgress => _prismaticTimer / (float)MAX_PRISMATIC_TIMER;

	public static readonly SoundStyle FlashHit = SoundID.Item29 with { PitchVariance = 0.15f, Volume = 0.33f };

	public bool active; // whether or not to give the projectile effects

	private readonly Vector2[] _oldPositions = new Vector2[MAX_TRAIL_LENGTH];
	private readonly AdornedBow.PrismaticPalette _primaryPalette = new();
	private readonly AdornedBow.PrismaticPalette _secondaryPalette = new();

	private bool _hitNPC = false;
	private int _prismaticTimer = 50;

	public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.friendly && entity.DamageType == DamageClass.Ranged;

	public override void AI(Projectile Projectile)
	{
		if (!active)
			return;

		if (_prismaticTimer > 0)
		{
			_prismaticTimer--;

			_secondaryPalette.FadeColors(_primaryPalette.Colors, PrismaticProgress);
			Lighting.AddLight(Projectile.Center, MulticolorLerp(PrismaticProgress, _primaryPalette.Colors).ToVector3() * 0.5f * PrismaticProgress);

			if (Main.rand.NextBool(20))
			{
				Color c = _primaryPalette.Colors[Main.rand.Next(3)];

				ParticleHandler.SpawnParticle(new SharpStarParticle(
					Projectile.Center + Main.rand.NextVector2Circular(20f, 20f),
					Projectile.velocity.RotatedByRandom(0.2f),
					Color.White,
					c,
					Main.rand.NextFloat(0.1f, 0.2f),
					Main.rand.Next(30, 50),
					0.25f,
					DelegateAction)
					);
			}
		}

		for (int i = MAX_TRAIL_LENGTH - 1; i > 0; i--)
			_oldPositions[i] = _oldPositions[i - 1];

		_oldPositions[0] = Projectile.Center + Projectile.velocity * 0.5f;

		static void DelegateAction(Particle p) => p.Velocity *= 0.9f;
	}

	public override bool PreDraw(Projectile Projectile, ref Color lightColor)
	{
		if (!active)
			return true;

		//Partially adapted from JinxBow/JinxBowShot

		//Load texture if not already loaded
		Main.instance.LoadProjectile(ProjectileID.HallowBossRainbowStreak);

		var defaultTexture = TextureAssets.Projectile[Projectile.type].Value;
		Texture2D solid = TextureColorCache.ColorSolid(defaultTexture, Color.White);
		var texture = TextureAssets.Projectile[ProjectileID.HallowBossRainbowStreak].Value;

		for (int i = MAX_TRAIL_LENGTH - 1; i >= 0; i--)
		{
			float lerp = 1f - i / (float)(MAX_TRAIL_LENGTH - 1);
			var position = _oldPositions[i] - Main.screenPosition;
			var scale = new Vector2(.5f * lerp, 1) * Projectile.scale;

			Color fadeColor = Color.Lerp(Color.LightSteelBlue, Color.White, lerp).Additive();

			if (_prismaticTimer > 0)
				fadeColor = Color.Lerp(MulticolorLerp(lerp, _secondaryPalette.Colors), fadeColor, EaseFunction.EaseCircularIn.Ease(1f - PrismaticProgress));

			if (i == 0)
			{
				texture = defaultTexture;
				scale = new(Projectile.scale);

				//Draw border around the main image
				for (int j = 0; j < 4; j++)
				{
					Vector2 offset = new Vector2(-2 * -Projectile.direction, 0).RotatedBy(Projectile.rotation - MathHelper.PiOver2) + Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
					Color drawColor = MulticolorLerp(PrismaticProgress, _secondaryPalette.Colors);

					if (_prismaticTimer <= 0)
						drawColor = Color.LightSteelBlue;

					Main.EntitySpriteDraw(solid, position + offset, null, drawColor, Projectile.rotation, texture.Size() / 2, scale, SpriteEffects.None);
					Main.EntitySpriteDraw(solid, position + offset, null, Color.White.Additive() * 0.5f, Projectile.rotation, texture.Size() / 2, scale, SpriteEffects.None);
				}
			}
			else //Otherwise draw as trail
			{
				var drawPos = Vector2.Lerp(position, _oldPositions[0] - Main.screenPosition, 0.33f);
				var drawColor = fadeColor * EaseFunction.EaseQuadIn.Ease(lerp) * 0.5f;
				Main.EntitySpriteDraw(solid, drawPos, null, drawColor, Projectile.rotation, solid.Size() / 2, new Vector2(Projectile.scale), SpriteEffects.None);
			}

			Main.EntitySpriteDraw(texture, position, null, fadeColor with { A = 0 }, Projectile.rotation, texture.Size() / 2, scale, SpriteEffects.None);
		}

		return true;
	}

	public override void PostDraw(Projectile Projectile, Color lightColor)
	{
		if (active)
		{
			var star = AssetLoader.LoadedTextures["StarChromatic"].Value;
			float fade = EaseFunction.EaseQuinticOut.Ease(1f);
			Vector2 position = Projectile.Center + new Vector2(Projectile.width, 0).RotatedBy(Projectile.rotation - MathHelper.PiOver2) - Main.screenPosition;

			Main.EntitySpriteDraw(star, position, null, Color.White with { A = 0 } * fade, MathHelper.Pi * EaseFunction.EaseQuinticIn.Ease(fade), star.Size() / 2, 0.0325f, SpriteEffects.None);
		}
	}

	public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (!active || _hitNPC)
			return;

		SoundEngine.PlaySound(FlashHit, target.Center);

		int type = ModContent.ProjectileType<AdornedFlash>();
		var p = Projectile.NewProjectileDirect(projectile.GetSource_OnHit(target), projectile.Center, Vector2.Zero, type, (int)(projectile.damage * 0.66f), 0f, projectile.owner, target.whoAmI);

		p.rotation = projectile.velocity.ToRotation();
		p.spriteDirection = projectile.direction;

		for (int i = 0; i < 4; i++)
		{
			Color c = _primaryPalette.Colors[Main.rand.Next(3)];

			static void ColorAction(Particle p)
			{
				p.Velocity *= 0.95f;
				Color light = Main.rand.Next([Color.Green, Color.Cyan, Color.Orange]);
				Lighting.AddLight(p.Position, light.ToVector3() * MathHelper.Lerp(0.25f, 0f, p.TimeActive / (float)p.MaxTime));
			}

			ParticleHandler.SpawnParticle(new SharpStarParticle(
				target.Center,
				projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.3f, 0.6f),
				Color.White.Additive(),
				c,
				Main.rand.NextFloat(0.1f, 0.4f),
				Main.rand.Next(40, 70),
				0.5f,
				ColorAction)
				);

			for (int j = 0; j < 2; j++)
			{
				Vector2 velocity = projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.4f, 0.8f);
				float scale = Main.rand.NextFloat(0.3f, 1f);
				int maxTime = Main.rand.Next(25, 50);

				int _idx1 = j;
				int _idx2 = j + 1;

				if (_idx2 > 2)
					_idx2 = 0;

				ParticleHandler.SpawnParticle(new PixelBloom(target.Center, velocity, _primaryPalette.Colors[_idx1].Additive(),
					_primaryPalette.Colors[_idx2].Additive(), scale, maxTime, DecelerateAction));
			}
		}

		static void DecelerateAction(Particle p) => p.Velocity *= 0.9f;

		_hitNPC = true; // only make flash effect once;
	}

	public override void OnKill(Projectile projectile, int timeLeft)
	{
		if (!active)
			return;

		for (int i = 0; i < 3; i++)
		{
			Vector2 velocity = Main.rand.NextVector2CircularEdge(4f, 4f);
			float scale = Main.rand.NextFloat(0.5f, 1f);
			int lifeTime = Main.rand.Next(25, 50);

			ParticleHandler.SpawnParticle(new PixelBloom(projectile.Center, velocity, _primaryPalette.Colors[Main.rand.Next(3)].Additive(), scale, lifeTime, DelegateAction));
			ParticleHandler.SpawnParticle(new PixelBloom(projectile.Center, velocity, Color.White.Additive(), scale, lifeTime, DelegateAction));

			Color c = _primaryPalette.Colors[Main.rand.Next(3)];

			ParticleHandler.SpawnParticle(new SharpStarParticle(
				projectile.Center,
				Main.rand.NextVector2CircularEdge(4f, 4f),
				Color.White,
				c,
				Main.rand.NextFloat(0.1f, 0.4f),
				Main.rand.Next(40, 70),
				0.75f,
				DelegateAction)
				);
		}

		if (Main.myPlayer == projectile.owner)
		{
			// ik this is slightly jank :p  

			PreNewProjectile.New(projectile.GetSource_Death(), projectile.Center, Vector2.Zero, ModContent.ProjectileType<AdornedArrowDeathTrail>(), 0, 0, projectile.owner, preSpawnAction: (p) =>
			{
				var deathTrail = p.ModProjectile as AdornedArrowDeathTrail;

				deathTrail.prismaticColors = _primaryPalette.Colors;
				deathTrail.oldPositions = _oldPositions;
				deathTrail.arrowType = projectile.type;
				p.rotation = projectile.rotation;
			});
		}

		static void DelegateAction(Particle p) => p.Velocity *= 0.875f;
	}

	public static Color MulticolorLerp(float increment, params Color[] colors)
	{
		increment %= 0.999f;
		int currentColorIndex = (int)(increment * colors.Length);
		Color color = colors[currentColorIndex];
		Color nextColor = colors[(currentColorIndex + 1) % colors.Length];
		return Color.Lerp(color, nextColor, increment * colors.Length % 1f);
	}
}