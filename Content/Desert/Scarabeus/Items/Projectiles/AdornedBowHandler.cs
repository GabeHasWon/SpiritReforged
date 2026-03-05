using AssGen;
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
using System.Linq;
using Terraria.DataStructures;
using Terraria.Graphics.CameraModifiers;
using static Microsoft.Xna.Framework.MathHelper;
using static SpiritReforged.Common.Easing.EaseFunction;

namespace SpiritReforged.Content.Desert.Scarabeus.Items.Projectiles;

// global projectile for visuals and effects attached to power shot arrows
// TODO: Change naming? AdornedGlobalProjectile may be preferred here.
public class AdornedArrowHandler : GlobalProjectile
{
	public override bool InstancePerEntity => true;

	public bool active; // whether or not to give the projectile effects

	public const int TrailLength = 12;
	private readonly Vector2[] _oldPositions = new Vector2[TrailLength];

	private int _flashTimer = 15;

	public override void AI(Projectile Projectile)
	{
		if (!active)
			return;

		if (_flashTimer > 0)
		{
			_flashTimer--;

			Vector2 velocity = Projectile.velocity.SafeNormalize(Vector2.One);
			velocity *= -6f;

			float progress = _flashTimer / 15f;

			Color c = Color.Lerp(Color.LightGoldenrodYellow, Color.DarkCyan, 1f - progress).Additive();
			float scale = 1f * progress;

			ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center, velocity.RotatedBy(-0.33f), c, scale, 40, 1, p => p.Velocity *= 0.9f));
			ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center, velocity.RotatedBy(-0.33f), Color.White.Additive(), scale, 20, 1, p => p.Velocity *= 0.9f));

			ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center, velocity.RotatedBy(0.33f), c, scale, 40, 1, p => p.Velocity *= 0.9f));
			ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center, velocity.RotatedBy(0.33f), Color.White.Additive(), scale, 20, 1, p => p.Velocity *= 0.9f));
		}

		for (int i = TrailLength - 1; i > 0; i--)
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
		Texture2D solid = TextureColorCache.ColorSolid(defaultTexture, Color.LightSkyBlue);
		var brightest = TextureColorCache.GetBrightestColor(defaultTexture);

		var bloom = AssetLoader.LoadedTextures["Bloom"].Value;

		Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, Color.DarkCyan with { A = 0 } * 0.33f, Projectile.rotation, bloom.Size() / 2, 0.35f, SpriteEffects.None);

		for (int i = TrailLength - 1; i >= 0; i--)
		{
			var texture = TextureAssets.Projectile[ProjectileID.HallowBossRainbowStreak].Value;

			float lerp = 1f - i / (float)(TrailLength - 1);
			var position = _oldPositions[i] - Main.screenPosition;
			var scale = new Vector2(.5f * lerp, 1) * Projectile.scale;

			Color fadeColor = Color.Lerp(Color.LightSteelBlue, Color.DarkCyan, lerp).Additive(50);

			if (_flashTimer > 0)
				fadeColor = Color.Lerp(Color.LightYellow, fadeColor, 1f - _flashTimer / 15f);

			if (i == 0)
			{
				texture = defaultTexture;
				scale = new(Projectile.scale);

				//Draw border around the main image
				for (int j = 0; j < 12; j++)
				{
					Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 12f) * 2;
					var drawColor = fadeColor * 0.33f;
					Main.EntitySpriteDraw(solid, position + offset, null, drawColor, Projectile.rotation, texture.Size() / 2, scale, SpriteEffects.None);
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
			var star = AssetLoader.LoadedTextures["Star"].Value;

			float fade = EaseQuinticOut.Ease(_flashTimer / 15f);

			Main.EntitySpriteDraw(star, Projectile.Center - Main.screenPosition, null, Color.Yellow with { A = 0 } * fade, TwoPi * fade, star.Size() / 2, 0.25f * fade, SpriteEffects.None);

			Main.EntitySpriteDraw(star, Projectile.Center - Main.screenPosition, null, Color.White with { A = 0 } * fade, TwoPi * fade, star.Size() / 2, 0.225f * fade, SpriteEffects.None);
		}
	}

	public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (active)
		{
			int count = 0;
			int maxHits = 3;

			foreach (NPC n in Main.npc.OrderBy(n => projectile.Distance(n.Center))) // loop through closest npcs
			{
				bool infrontOfArrow = projectile.rotation + Math.Abs(projectile.DirectionTo(n.Center).ToRotation()) < 1f;

				Main.NewText(projectile.rotation + " " + Math.Abs(projectile.DirectionTo(n.Center).ToRotation()));

				if (n != target && n.CanBeChasedBy() && n.active && infrontOfArrow && projectile.Distance(n.Center) < 300f)
				{
					Vector2 velocity = projectile.DirectionTo(n.Center);

					PreNewProjectile.New(projectile.GetSource_OnHit(n), n.Center, Vector2.Zero, ModContent.ProjectileType<AdornedFlash>(), 10, 0f, projectile.owner, preSpawnAction: (Projectile p) =>
					{
						(p.ModProjectile as AdornedFlash).originalCenter = projectile.Center;
					});

					count++;
				}

				if (count >= maxHits)
					break;
			}
		}
	}

	public override void OnKill(Projectile projectile, int timeLeft)
	{
		if (active)
		{
			for (int i = 0; i < 7; i++)
			{
				Vector2 velocity = Main.rand.NextVector2CircularEdge(4f, 4f);
				float scale = Main.rand.NextFloat(0.5f, 1f);
				int lifeTime = Main.rand.Next(25, 50);
				static void DelegateAction(Particle p) => p.Velocity *= 0.875f;

				ParticleHandler.SpawnParticle(new GlowParticle(projectile.Center, velocity, Color.Cyan.Additive(), scale, lifeTime, 1, DelegateAction));
				ParticleHandler.SpawnParticle(new GlowParticle(projectile.Center, velocity, Color.White.Additive(), scale, lifeTime, 1, DelegateAction));
			}

			ParticleHandler.SpawnParticle(new LightBurst(projectile.Center, Main.rand.NextFloatDirection(), Color.LightGoldenrodYellow.Additive(), 0.66f, 35));
			ParticleHandler.SpawnParticle(new LightBurst(projectile.Center, Main.rand.NextFloatDirection(), Color.White.Additive() * 0.5f, 0.66f, 35));
		}
	}
}
