using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using System.Linq;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.Scarabeus.Items.Projectiles;

public class AdornedFlash : ModProjectile
{
	public override string Texture => AssetLoader.EmptyTexture;

	// TODO: use better sound
	internal static SoundStyle FlashHit = SoundID.Item29 with { PitchVariance = 0.2f, Volume = 0.2f };
	
	public int _maxShines;
	public int[] shineColors; // indices of PrismaticColors
	private int PrismaticTimer;

	private float MaxPrismaticTimer;
	public float[] shineRotations;

	public Vector2 originalCenter;
	public Vector2[] shineScales;

	private Color[] PrismaticColors; // base colors
	private Color[] PrismaticActiveColors = new Color[3]; // colors that lerp between the base colors	
	public override void Load()
	{
		if (Main.dedServ)
			return;

		On_Main.DrawCachedProjs += DrawLight;
	}

	private void DrawLight(On_Main.orig_DrawCachedProjs orig, Main self, List<int> projCache, bool startSpriteBatch)
	{
		SpriteBatch sb = Main.spriteBatch;

		orig(self, projCache, startSpriteBatch);

		if (projCache.Equals(Main.instance.DrawCacheProjsBehindNPCs))
		{
			List<Projectile> flashes = new List<Projectile>();

			sb.Begin(default, default, Main.DefaultSamplerState, default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
			
			foreach (Projectile p in Main.projectile.Where(p => p.active && p.type == Type))
			{
				flashes.Add(p);
				(p.ModProjectile as AdornedFlash).PreDrawNonPreMult();
			}

			sb.End();
			sb.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointClamp,
					DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

			foreach (Projectile p in flashes)
			{
				(p.ModProjectile as AdornedFlash).DrawPreMult();
			}

			sb.End();
			sb.Begin(default, default, Main.DefaultSamplerState, default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

			foreach (Projectile p in flashes)
			{
				(p.ModProjectile as AdornedFlash).DrawPostPreMult();
			}

			sb.End();
		}
	}

	public override void SetDefaults()
	{
		Projectile.Size = new(30);
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Ranged;

		Projectile.tileCollide = false;
		Projectile.friendly = true;
		Projectile.penetrate = 3;

		Projectile.stopsDealingDamageAfterPenetrateHits = true;

		Projectile.timeLeft = 50;
		Projectile.hide = true;

		// only hit a given npc once
		Projectile.localNPCHitCooldown = -1;
		Projectile.usesLocalNPCImmunity = true;
	}

	public override bool ShouldUpdatePosition() => false;

	public override void AI()
	{
		// initialization check
		if (PrismaticColors is null)
		{
			InitializeColors();

			_maxShines = Main.rand.Next(4, 8) * 2;

			shineScales = new Vector2[_maxShines];
			shineRotations = new float[_maxShines];
			shineColors = new int[_maxShines];

			for (int i = 0; i < _maxShines; i++)
			{
				shineScales[i] = new Vector2(Main.rand.NextFloat(0.25f, 0.5f), Main.rand.NextFloat(0.1f, 0.25f));
				shineRotations[i] = Main.rand.NextFloat(-0.5f, 0.5f);
				shineColors[i] = (int)(3 * (i / (float)_maxShines));
			}
		}

		if (PrismaticTimer > 0)
		{
			FadeColors();
			PrismaticTimer--;
		}

		Projectile.velocity = Projectile.rotation.ToRotationVector2();

		if (Projectile.timeLeft > 40)
			Lighting.AddLight(Projectile.Center, AdornedBowGlobalProjectile.MulticolorLerp(Projectile.timeLeft / 50f, PrismaticColors).ToVector3() * (Projectile.timeLeft / 50f));
	}

	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => behindNPCs.Add(index);

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		static void DecelerateAction(Particle p) => p.Velocity *= 0.925f;

		SoundEngine.PlaySound(FlashHit, target.Center);

		Color c = PrismaticColors[Main.rand.Next(3)];

		static void ColorAction(Particle p)
		{
			p.Velocity *= 0.925f;
			Color light = Main.rand.Next(new Color[] { Color.Green, Color.Cyan, Color.Orange });
			Lighting.AddLight(p.Position, light.ToVector3() * MathHelper.Lerp(0.25f, 0f, p.TimeActive / (float)p.MaxTime));
		}

		for (int i = 0; i < 2; i++)
		{
			ParticleHandler.SpawnParticle(new SharpStarParticle(
			target.Center,
			Projectile.velocity.RotatedByRandom(1f) * Main.rand.NextFloat(5f, 7f),
			Color.White.Additive(),
			c.Additive(),
			Main.rand.NextFloat(0.05f, 0.25f),
			Main.rand.Next(10, 40),
			0.2f,
			ColorAction)
			);

			for (int j = 0; j < 2; j++)
			{
				Vector2 velocity = Projectile.velocity.RotatedByRandom(0.9f) * Main.rand.NextFloat(4f, 8f);
				float scale = Main.rand.NextFloat(0.3f, 1f);
				int maxTime = Main.rand.Next(25, 50);

				int _idx1 = j;
				int _idx2 = j + 1;

				if (_idx2 > 2)
					_idx2 = 0;

				ParticleHandler.SpawnParticle(new PixelBloom(target.Center, velocity, PrismaticColors[_idx1].Additive(), PrismaticColors[_idx2].Additive(), scale, maxTime, DecelerateAction));
			}
		}	
	}

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		Vector2 rotation = Projectile.rotation.ToRotationVector2();
		Vector2 direction = Projectile.DirectionTo(targetHitbox.Center.ToVector2());

		return Vector2.Dot(rotation, direction) >= Math.Cos(MathHelper.ToRadians(90f) / 2f) && Projectile.Distance(targetHitbox.Center.ToVector2()) < 200f;
	}

	// for batching, see DrawLight above
	public void PreDrawNonPreMult()
	{
		var bloom = AssetLoader.LoadedTextures["Bloom"].Value;
		var star = AssetLoader.LoadedTextures["StarChromatic"].Value;

		float fade = EaseBuilder.EaseQuinticIn.Ease(Projectile.timeLeft / 50f);

		Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, PrismaticActiveColors[0].Additive() * fade,
			0f, bloom.Size() / 2f, 0.5f * MathHelper.Lerp(1.05f, 0.85f, EaseBuilder.EaseQuinticIn.Ease(Projectile.timeLeft / 50f)), 0f);

		Main.EntitySpriteDraw(star, Projectile.Center - Main.screenPosition, null, Color.White.Additive() * fade,
			0f, star.Size() / 2f, 0.1f * MathHelper.Lerp(1.05f, 0.85f, EaseBuilder.EaseQuinticIn.Ease(Projectile.timeLeft / 50f)), 0f);
	}

	public void DrawPreMult()
	{
		float fade = EaseBuilder.EaseQuinticIn.Ease(Projectile.timeLeft / 50f);

		var shine = AssetLoader.LoadedTextures["Shine"].Value;

		for (int i = 0; i < _maxShines; i++)
		{
			Main.EntitySpriteDraw(shine, Projectile.Center - Main.screenPosition, null, PrismaticActiveColors[shineColors[i]] * 0.5f * fade,
				Projectile.rotation + shineRotations[i] + MathHelper.PiOver2, new Vector2(shine.Width / 2, shine.Height),
				shineScales[i] * MathHelper.Lerp(1.05f, 0.85f, EaseBuilder.EaseQuinticIn.Ease(Projectile.timeLeft / 50f)), SpriteEffects.None);
		}
	}

	public void DrawPostPreMult()
	{
		float fade = EaseBuilder.EaseQuinticIn.Ease(Projectile.timeLeft / 50f);

		var shineAlpha = AssetLoader.LoadedTextures["ShineAlpha"].Value;

		for (int i = 0; i < _maxShines; i++)
		{
			Main.EntitySpriteDraw(shineAlpha, Projectile.Center - Main.screenPosition, null, PrismaticActiveColors[shineColors[i]].Additive() * fade * 0.65f,
				Projectile.rotation + shineRotations[i] + MathHelper.PiOver2, new Vector2(shineAlpha.Width / 2, shineAlpha.Height),
				shineScales[i] * MathHelper.Lerp(1.05f, 0.85f, EaseBuilder.EaseQuinticIn.Ease(Projectile.timeLeft / 50f)), SpriteEffects.None);
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{

		
		

		Main.spriteBatch.End();
		Main.spriteBatch.Begin(default, default, default, default, default, null, Main.GameViewMatrix.TransformationMatrix);	

		return false;
	}

	private void InitializeColors()
	{
		PrismaticColors = AdornedBowGlobalProjectile.GetPrismaticColors();

		PrismaticActiveColors[0] = PrismaticColors[0];
		PrismaticActiveColors[1] = PrismaticColors[1];
		PrismaticActiveColors[2] = PrismaticColors[2];

		PrismaticTimer = 20;
		MaxPrismaticTimer = 20;
	}

	private void FadeColors()
	{
		PrismaticActiveColors[0] = Color.Lerp(PrismaticColors[0], PrismaticColors[1], PrismaticTimer / MaxPrismaticTimer);
		PrismaticActiveColors[1] = Color.Lerp(PrismaticColors[1], PrismaticColors[2], PrismaticTimer / MaxPrismaticTimer);
		PrismaticActiveColors[2] = Color.Lerp(PrismaticColors[2], PrismaticColors[0], PrismaticTimer / MaxPrismaticTimer);
	}
}
