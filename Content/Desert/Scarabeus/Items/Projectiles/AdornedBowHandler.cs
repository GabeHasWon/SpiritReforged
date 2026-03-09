using AssGen;
using MonoMod.Utils;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Tiles;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.CameraModifiers;
using static Microsoft.Xna.Framework.MathHelper;
using static SpiritReforged.Common.Easing.EaseFunction;
using static SpiritReforged.Common.Visuals.DrawHelpers;

namespace SpiritReforged.Content.Desert.Scarabeus.Items.Projectiles;

// global projectile for visuals and effects attached to power shot arrows
// TODO: Change naming? AdornedGlobalProjectile may be preferred here.

public class AdornedArrowDeathTrail : ModProjectile
{
	public override string Texture => AssetLoader.EmptyTexture;

	public const int TrailLength = 12;
	public Vector2[] _oldPositions;

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

		//var bloom = AssetLoader.LoadedTextures["Bloom"].Value;

		//Main.EntitySpriteDraw(bloom, Projectile.Center - (Projectile.rotation - PiOver2).ToRotationVector2() * 5f - Main.screenPosition, null, Color.White with { A = 0 } * 0.33f, Projectile.rotation, bloom.Size() / 2, 0.35f, SpriteEffects.None);

		for (int i = TrailLength - 1; i >= 0; i--)
		{
			var texture = TextureAssets.Projectile[ProjectileID.HallowBossRainbowStreak].Value;

			float lerp = 1f - i / (float)(TrailLength - 1);
			var position = _oldPositions[i] - Main.screenPosition;
			var scale = new Vector2(.5f * lerp, 1) * Projectile.scale;

			float fadeOut = Projectile.timeLeft / 20f;

			Color fadeColor = Color.Lerp(Color.LightSteelBlue, Color.White, lerp).Additive() * fadeOut;

			var drawPos = Vector2.Lerp(position, _oldPositions[0] - Main.screenPosition, 0.33f);
			var drawColor = fadeColor * EaseFunction.EaseQuadIn.Ease(lerp) * 0.5f;
			Main.EntitySpriteDraw(solid, drawPos, null, drawColor, Projectile.rotation, solid.Size() / 2, new Vector2(Projectile.scale), SpriteEffects.None);

			Main.EntitySpriteDraw(texture, position, null, fadeColor with { A = 0 }, Projectile.rotation, texture.Size() / 2, scale, SpriteEffects.None);
		}

		return false;
	}
}

public class AdornedArrowHandler : GlobalProjectile
{
	public override bool InstancePerEntity => true;

	public bool active; // whether or not to give the projectile effects

	public const int TrailLength = 12;
	private readonly Vector2[] _oldPositions = new Vector2[TrailLength];

	private int _flashTimer = 15;

	private Color[] PrismaticColors = new Color[3]; // base colors
	private Color[] PrismaticActiveColors = new Color[3]; // colors that lerp between the base colors
	private int PrismaticTimer;
	private float MaxPrismaticTimer;
	private void InitializeColors()
	{
		PrismaticColors[0] = new Color(255, 0, 70 + 25 * Main.rand.Next(5)); // Magenta to Purple
		PrismaticColors[1] = new Color(0, 255, 255 - 25 * Main.rand.Next(5)); // Cyan to Green
		PrismaticColors[2] = new Color(255, 255 - 25 * Main.rand.Next(5), 0); // Yellow to Orange

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

	public override void OnSpawn(Projectile projectile, IEntitySource source)
	{
		InitializeColors();
	}

	public override void AI(Projectile Projectile)
	{
		if (!active)
			return;

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

		for (int i = TrailLength - 1; i > 0; i--)
			_oldPositions[i] = _oldPositions[i - 1];

		_oldPositions[0] = Projectile.Center + Projectile.velocity * 0.5f;

		//if (!Main.dedServ)
		//	CreateTrail(Projectile, TrailSystem.ProjectileRenderer);
	}

	/*public  void CreateTrail(Projectile Projectile, ProjectileTrailRenderer renderer)
	{
		var position = new EntityTrailPosition(Projectile);

		renderer.CreateTrail(Projectile, new VertexTrail(new RainbowTrail(), new RoundCap(), position, new ImageShader(AssetLoader.LoadedTextures["Lightning"].Value, new Vector2(0.1f, 0.1f), 0.1f, 0.1f), 40, 200));
	}*/

	// can be moved to helper class
	internal static Color MulticolorLerp(float increment, params Color[] colors)
	{
		increment %= 0.999f;
		int currentColorIndex = (int)(increment * colors.Length);
		Color color = colors[currentColorIndex];
		Color nextColor = colors[(currentColorIndex + 1) % colors.Length];
		return Color.Lerp(color, nextColor, increment * colors.Length % 1f);
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

		//var bloom = AssetLoader.LoadedTextures["Bloom"].Value;

		//Main.EntitySpriteDraw(bloom, Projectile.Center - (Projectile.rotation - PiOver2).ToRotationVector2() * 5f - Main.screenPosition, null, Color.White with { A = 0 } * 0.33f, Projectile.rotation, bloom.Size() / 2, 0.35f, SpriteEffects.None);

		for (int i = TrailLength - 1; i >= 0; i--)
		{
			var texture = TextureAssets.Projectile[ProjectileID.HallowBossRainbowStreak].Value;

			float lerp = 1f - i / (float)(TrailLength - 1);
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
			int count = 0;
			int maxHits = 3;

			foreach (NPC n in Main.npc.Where(n => projectile.Distance(n.Center) < 300f).OrderBy(n => projectile.Distance(n.Center))) // loop through closest npcs
			{
				float degrees = 60f;

				Vector2 _pDir = projectile.velocity.SafeNormalize(Vector2.Zero);
				Vector2 _tDir = projectile.DirectionTo(n.Center).SafeNormalize(Vector2.Zero);

				bool conalCheck = Vector2.Dot(_pDir, _tDir) >= (float)Math.Cos(ToRadians(degrees) / 2f);

				if (n != target && n.CanBeChasedBy() && n.active && conalCheck)
				{
					Vector2 velocity = projectile.DirectionTo(n.Center);

					//PreNewProjectile.New(projectile.GetSource_OnHit(n), n.Center, Vector2.Zero, ModContent.ProjectileType<AdornedFlash>(), (int)(projectile.damage * 0.66f), 0f, projectile.owner, preSpawnAction: (Projectile p) =>
					//{
					//	(p.ModProjectile as AdornedFlash).originalCenter = projectile.Center;
					//});

					//
					count++;
				}

				if (count >= maxHits)
					break;
			}

			SoundEngine.PlaySound(SoundID.Item29 with { PitchVariance = 0.15f, Volume = 0.33f }, target.Center);

			Projectile p = Projectile.NewProjectileDirect(projectile.GetSource_OnHit(target), projectile.Center, Vector2.Zero, ModContent.ProjectileType<AdornedFlash>(), (int)(projectile.damage * 0.66f), 0f, projectile.owner);

			p.rotation = projectile.velocity.ToRotation();
			p.spriteDirection = projectile.direction;

			static void DelegateAction(Particle p) => p.Velocity *= 0.9f;

			for (int i = 0; i < 4; i++)
			{
				Color c = PrismaticColors[Main.rand.Next(3)];

				static void ColorAction(Particle p)
				{
					p.Velocity *= 0.95f;
					Color light = Main.rand.Next(new Color[] { Color.Green, Color.Cyan, Color.Orange });
					Lighting.AddLight(p.Position, light.ToVector3() * MathHelper.Lerp(0.25f, 0f, p.TimeActive / (float)p.MaxTime));
				}

				static void DelegateAction_2(Particle p)
				{
					p.Velocity *= 0.975f;
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

				/*ParticleHandler.SpawnParticle(new LingeringStarParticle(
					target.Center,
					projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.65f),
					Color.White.Additive() * 0.5f,
					c,
					Main.rand.NextFloat(0.1f, 0.4f),
					Main.rand.Next(40, 70),
					0.75f,
					DelegateAction_2)
					);*/

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
						PrismaticColors[_idx2].Additive(), scale, maxTime, DelegateAction));
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
				PreNewProjectile.New(projectile.GetSource_Death(), projectile.Center, Vector2.Zero, ModContent.ProjectileType<AdornedArrowDeathTrail>(), 0, 0, projectile.owner, preSpawnAction: (Projectile p) =>
				{
					(p.ModProjectile as AdornedArrowDeathTrail)._oldPositions = _oldPositions;
					(p.ModProjectile as AdornedArrowDeathTrail)._arrowType = projectile.type;
					p.rotation = projectile.rotation;
				});
			}
		}
	}
}
