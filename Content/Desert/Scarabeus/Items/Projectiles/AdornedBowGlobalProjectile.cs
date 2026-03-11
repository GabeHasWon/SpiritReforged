using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Ziggurat.Vanity;
using Terraria.Audio;
using Terraria.DataStructures;
using static Microsoft.Xna.Framework.MathHelper;
using static SpiritReforged.Common.Easing.EaseFunction;

namespace SpiritReforged.Content.Desert.Scarabeus.Items.Projectiles;

// This class serves as the death trail for any adorned bow projectiles
// We cannot keep the data on the projectile itself because the projectile dies, this is a weird workaround that kind of takes a projectile slot-
// But its better than messing with death logic
public class AdornedArrowDeathTrail : ModProjectile
{
	// TODO: use better sound
	public override string Texture => AssetLoader.EmptyTexture;

	public const int TrailLength = 12;
	public Vector2[] _oldPositions;

	public Color[] PrimsaticColors = new Color[3];

	public int _arrowType;

	public override void SetDefaults()
	{
		Projectile.tileCollide = false;
		Projectile.timeLeft = 20;
	}

	public override void AI()
	{
		for (int i = TrailLength - 1; i > 0; i--)
			_oldPositions[i] = _oldPositions[i - 1];

		_oldPositions[0] = Projectile.Center;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		//Partially adapted from JinxBow/JinxBowShot

		//Load texture if not already loaded
		Main.instance.LoadProjectile(ProjectileID.HallowBossRainbowStreak);

		var defaultTexture = TextureAssets.Projectile[_arrowType].Value;
		Texture2D solid = TextureColorCache.ColorSolid(defaultTexture, Color.LightSkyBlue);

		for (int i = TrailLength - 1; i >= 0; i--)
		{
			var texture = TextureAssets.Projectile[ProjectileID.HallowBossRainbowStreak].Value;

			float lerp = 1f - i / (float)(TrailLength - 1);
			var position = _oldPositions[i] - Main.screenPosition;
			var scale = new Vector2(.5f * lerp, 1) * Projectile.scale;

			float fadeOut = Projectile.timeLeft / 20f;
			
			Color fadeColor = AdornedBowGlobalProjectile.MulticolorLerp(fadeOut, PrimsaticColors) * fadeOut;

			var drawPos = Vector2.Lerp(position, _oldPositions[0] - Main.screenPosition, 0.33f);
			var drawColor = fadeColor * EaseFunction.EaseQuadIn.Ease(lerp) * 0.5f;
			Main.EntitySpriteDraw(solid, drawPos, null, drawColor, Projectile.rotation, solid.Size() / 2, new Vector2(Projectile.scale), SpriteEffects.None);

			Main.EntitySpriteDraw(texture, position, null, fadeColor with { A = 0 }, Projectile.rotation, texture.Size() / 2, scale, SpriteEffects.None);
		}

		return false;
	}
}

// global projectile for visuals and effects attached to power shot arrows
public class AdornedBowGlobalProjectile : GlobalProjectile
{
	public const int MAX_TRAIL_LENGTH = 12;
	public override bool InstancePerEntity => true;

	internal static SoundStyle FlashHit = SoundID.Item29 with { PitchVariance = 0.15f, Volume = 0.33f };

	private readonly Vector2[] _oldPositions = new Vector2[MAX_TRAIL_LENGTH];

	public bool active; // whether or not to give the projectile effects

	private int _flashTimer = 15;
	private int PrismaticTimer;

	private float MaxPrismaticTimer;

	private Color[] PrismaticColors; // base colors
	private Color[] PrismaticActiveColors = new Color[3]; // colors that lerp between the base colors
	public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.friendly && entity.DamageType == DamageClass.Ranged;
	public override void AI(Projectile Projectile)
	{
		if (!active)
			return;

		if (PrismaticColors is null)
			InitializeColors();

		if (PrismaticTimer > 0)
		{
			PrismaticTimer--;

			FadeColors();

			Lighting.AddLight(Projectile.Center, MulticolorLerp(PrismaticTimer / MaxPrismaticTimer, PrismaticColors).ToVector3() * 0.5f * (PrismaticTimer / MaxPrismaticTimer));

			if (Main.rand.NextBool(20))
			{
				static void DelegateAction(Particle p) => p.Velocity *= 0.9f;

				Color c = PrismaticColors[Main.rand.Next(3)];

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
	}
	public override bool PreDraw(Projectile Projectile, ref Color lightColor)
	{
		if (!active)
			return base.PreDraw(Projectile, ref lightColor);

		//Partially adapted from JinxBow/JinxBowShot

		//Load texture if not already loaded
		Main.instance.LoadProjectile(ProjectileID.HallowBossRainbowStreak);

		var defaultTexture = TextureAssets.Projectile[Projectile.type].Value;
		Texture2D solid = TextureColorCache.ColorSolid(defaultTexture, Color.White);

		for (int i = MAX_TRAIL_LENGTH - 1; i >= 0; i--)
		{
			var texture = TextureAssets.Projectile[ProjectileID.HallowBossRainbowStreak].Value;

			float lerp = 1f - i / (float)(MAX_TRAIL_LENGTH - 1);
			var position = _oldPositions[i] - Main.screenPosition;
			var scale = new Vector2(.5f * lerp, 1) * Projectile.scale;

			Color fadeColor = Color.Lerp(Color.LightSteelBlue, Color.White, lerp).Additive();

			if (PrismaticTimer > 0)
				fadeColor = Color.Lerp(MulticolorLerp(lerp, PrismaticActiveColors), fadeColor, EaseBuilder.EaseCircularIn.Ease(1f - PrismaticTimer / MaxPrismaticTimer));

			if (i == 0)
			{
				texture = defaultTexture;
				scale = new(Projectile.scale);

				//Draw border around the main image
				for (int j = 0; j < 4; j++)
				{
					Vector2 offset = new Vector2(-2 * -Projectile.direction, 0).RotatedBy(Projectile.rotation - PiOver2) + Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
					var drawColor = MulticolorLerp(PrismaticTimer / MaxPrismaticTimer, PrismaticActiveColors);
					if (PrismaticTimer <= 0)
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

		return base.PreDraw(Projectile, ref lightColor);
	}

	public override void PostDraw(Projectile Projectile, Color lightColor)
	{
		if (active && _flashTimer > 0)
		{
			var star = AssetLoader.LoadedTextures["StarChromatic"].Value;

			float fade = EaseQuinticOut.Ease(_flashTimer / 15f);

			Main.EntitySpriteDraw(star, Projectile.Center + new Vector2(Projectile.width, 0).RotatedBy(Projectile.rotation - PiOver2) - Main.screenPosition, null, Color.White with { A = 0 } * fade, Pi * EaseBuilder.EaseQuinticIn.Ease(fade), star.Size() / 2, 0.0325f, SpriteEffects.None);
		}
	}

	public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (active)
		{
			SoundEngine.PlaySound(FlashHit, target.Center);

			var p = Projectile.NewProjectileDirect(projectile.GetSource_OnHit(target), projectile.Center, Vector2.Zero, ModContent.ProjectileType<AdornedFlash>(), (int)(projectile.damage * 0.66f), 0f, projectile.owner);

			p.rotation = projectile.velocity.ToRotation();
			p.spriteDirection = projectile.direction;

			static void DecelerateAction(Particle p) => p.Velocity *= 0.9f;

			for (int i = 0; i < 4; i++)
			{
				Color c = PrismaticColors[Main.rand.Next(3)];

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

					ParticleHandler.SpawnParticle(new PixelBloom(target.Center, velocity, PrismaticColors[_idx1].Additive(),
						PrismaticColors[_idx2].Additive(), scale, maxTime, DecelerateAction));
				}	
			}
		}
	}

	public override void OnKill(Projectile projectile, int timeLeft)
	{
		if (active)
		{
			static void DelegateAction(Particle p) => p.Velocity *= 0.875f;

			for (int i = 0; i < 3; i++)
			{
				Vector2 velocity = Main.rand.NextVector2CircularEdge(4f, 4f);
				float scale = Main.rand.NextFloat(0.5f, 1f);
				int lifeTime = Main.rand.Next(25, 50);
				
				ParticleHandler.SpawnParticle(new PixelBloom(projectile.Center, velocity, PrismaticColors[Main.rand.Next(3)].Additive(), scale, lifeTime, DelegateAction));
				ParticleHandler.SpawnParticle(new PixelBloom(projectile.Center, velocity, Color.White.Additive(), scale, lifeTime, DelegateAction));
				
				Color c = PrismaticColors[Main.rand.Next(3)];

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

				PreNewProjectile.New(projectile.GetSource_Death(), projectile.Center, Vector2.Zero, ModContent.ProjectileType<AdornedArrowDeathTrail>(), 0, 0, projectile.owner, preSpawnAction: (Projectile p) =>
				{
					var deathTrail = (p.ModProjectile as AdornedArrowDeathTrail);

					deathTrail.PrimsaticColors = PrismaticColors;
					deathTrail._oldPositions = _oldPositions;
					deathTrail._arrowType = projectile.type;
					p.rotation = projectile.rotation;
				});
			}
		}
	}
	public static Color MulticolorLerp(float increment, params Color[] colors)
	{
		increment %= 0.999f;
		int currentColorIndex = (int)(increment * colors.Length);
		Color color = colors[currentColorIndex];
		Color nextColor = colors[(currentColorIndex + 1) % colors.Length];
		return Color.Lerp(color, nextColor, increment * colors.Length % 1f);
	}

	public static Color[] GetPrismaticColors()
	{
		var colors = new Color[3];

		colors[0] = new Color(255, 0, 70 + 25 * Main.rand.Next(5)); // Magenta to Purple
		colors[1] = new Color(0, 255, 255 - 25 * Main.rand.Next(5)); // Cyan to Green
		colors[2] = new Color(255, 255 - 25 * Main.rand.Next(5), 0); // Yellow to Orange

		return colors;
	}

	private void InitializeColors()
	{
		PrismaticColors = GetPrismaticColors();

		PrismaticActiveColors[0] = PrismaticColors[0];
		PrismaticActiveColors[1] = PrismaticColors[1];
		PrismaticActiveColors[2] = PrismaticColors[2];

		PrismaticTimer = 50;
		MaxPrismaticTimer = 50;
	}

	private void FadeColors()
	{
		PrismaticActiveColors[0] = Color.Lerp(PrismaticColors[0], PrismaticColors[1], PrismaticTimer / MaxPrismaticTimer);
		PrismaticActiveColors[1] = Color.Lerp(PrismaticColors[1], PrismaticColors[2], PrismaticTimer / MaxPrismaticTimer);
		PrismaticActiveColors[2] = Color.Lerp(PrismaticColors[2], PrismaticColors[0], PrismaticTimer / MaxPrismaticTimer);
	}
}
