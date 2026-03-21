using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using System.Linq;
using Terraria.Audio;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items.Projectiles;

public class AdornedFlash : ModProjectile
{
	public const int MAX_PRISMATIC_TIMER = 20;

	public override string Texture => AssetLoader.EmptyTexture;

	// TODO: use better sound
	public static readonly SoundStyle FlashHit = SoundID.Item29 with { PitchVariance = 0.2f, Volume = 0.2f };

	private readonly AdornedBow.PrismaticPalette _primaryPalette = new();
	private readonly AdornedBow.PrismaticPalette _secondaryPalette = new();

	private int _prismaticTimer = 20;
	private int _maxShines;
	private int[] _shineColors; // indices of PrismaticColors
	private float[] _shineRotations;
	private Vector2[] _shineScales;

	public override void Load()
	{
		if (Main.dedServ)
			return;

		On_Main.DrawCachedProjs += DrawLight;
	}

	private static void DrawLight(On_Main.orig_DrawCachedProjs orig, Main self, List<int> projCache, bool startSpriteBatch)
	{
		SpriteBatch sb = Main.spriteBatch;

		orig(self, projCache, startSpriteBatch);

		if (projCache.Equals(Main.instance.DrawCacheProjsBehindNPCs))
		{
			var flashes = new List<Projectile>();

			sb.Begin(default, default, Main.DefaultSamplerState, default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
			
			foreach (Projectile p in Main.projectile.Where(p => p.active && p.type == ModContent.ProjectileType<AdornedFlash>()))
			{
				flashes.Add(p);
				(p.ModProjectile as AdornedFlash).PreDrawNonPreMult();
			}

			sb.End();
			sb.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

			foreach (Projectile p in flashes)
				(p.ModProjectile as AdornedFlash).DrawPreMult();

			sb.End();
			sb.Begin(default, default, Main.DefaultSamplerState, default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

			foreach (Projectile p in flashes)
				(p.ModProjectile as AdornedFlash).DrawPostPreMult();

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
		if (_maxShines == 0)
		{
			_maxShines = Main.rand.Next(4, 8) * 2;
			_shineScales = new Vector2[_maxShines];
			_shineRotations = new float[_maxShines];
			_shineColors = new int[_maxShines];

			for (int i = 0; i < _maxShines; i++)
			{
				_shineScales[i] = new Vector2(Main.rand.NextFloat(0.25f, 0.5f), Main.rand.NextFloat(0.1f, 0.25f));
				_shineRotations[i] = Main.rand.NextFloat(-0.5f, 0.5f);
				_shineColors[i] = (int)(3 * (i / (float)_maxShines));
			}
		} //One-time effects

		if (_prismaticTimer > 0)
		{
			_secondaryPalette.FadeColors(_primaryPalette.Colors, _prismaticTimer / (float)MAX_PRISMATIC_TIMER);
			_prismaticTimer--;
		}

		Projectile.velocity = Projectile.rotation.ToRotationVector2();

		if (Projectile.timeLeft > 40)
			Lighting.AddLight(Projectile.Center, AdornedBowGlobalProjectile.MulticolorLerp(Projectile.timeLeft / 50f, _primaryPalette.Colors).ToVector3() * (Projectile.timeLeft / 50f));
	}

	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => behindNPCs.Add(index);

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		SoundEngine.PlaySound(FlashHit, target.Center);
		Color c = _primaryPalette.Colors[Main.rand.Next(3)];

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

				ParticleHandler.SpawnParticle(new PixelBloom(target.Center, velocity, _primaryPalette.Colors[_idx1].Additive(), _primaryPalette.Colors[_idx2].Additive(), scale, maxTime, DecelerateAction));
			}
		}

		static void DecelerateAction(Particle p) => p.Velocity *= 0.925f;

		static void ColorAction(Particle p)
		{
			p.Velocity *= 0.925f;
			Color light = Main.rand.Next(new Color[] { Color.Green, Color.Cyan, Color.Orange });
			Lighting.AddLight(p.Position, light.ToVector3() * MathHelper.Lerp(0.25f, 0f, p.TimeActive / (float)p.MaxTime));
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
		Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;
		Texture2D star = AssetLoader.LoadedTextures["StarChromatic"].Value;
		float fade = EaseFunction.EaseQuinticIn.Ease(Projectile.timeLeft / 50f);

		Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, _secondaryPalette.Colors[0].Additive() * fade,
			0f, bloom.Size() / 2f, 0.5f * MathHelper.Lerp(1.05f, 0.85f, EaseFunction.EaseQuinticIn.Ease(Projectile.timeLeft / 50f)), 0f);

		Main.EntitySpriteDraw(star, Projectile.Center - Main.screenPosition, null, Color.White.Additive() * fade,
			0f, star.Size() / 2f, 0.1f * MathHelper.Lerp(1.05f, 0.85f, EaseFunction.EaseQuinticIn.Ease(Projectile.timeLeft / 50f)), 0f);
	}

	public void DrawPreMult()
	{
		float fade = EaseFunction.EaseQuinticIn.Ease(Projectile.timeLeft / 50f);
		Texture2D shine = AssetLoader.LoadedTextures["Shine"].Value;

		for (int i = 0; i < _maxShines; i++)
			Main.EntitySpriteDraw(shine, Projectile.Center - Main.screenPosition, null, _secondaryPalette.Colors[_shineColors[i]] * 0.5f * fade,
				Projectile.rotation + _shineRotations[i] + MathHelper.PiOver2, new Vector2(shine.Width / 2, shine.Height),
				_shineScales[i] * MathHelper.Lerp(1.05f, 0.85f, EaseFunction.EaseQuinticIn.Ease(Projectile.timeLeft / 50f)), SpriteEffects.None);
	}

	public void DrawPostPreMult()
	{
		float fade = EaseFunction.EaseQuinticIn.Ease(Projectile.timeLeft / 50f);
		Texture2D shineAlpha = AssetLoader.LoadedTextures["ShineAlpha"].Value;

		for (int i = 0; i < _maxShines; i++)
			Main.EntitySpriteDraw(shineAlpha, Projectile.Center - Main.screenPosition, null, _secondaryPalette.Colors[_shineColors[i]].Additive() * fade * 0.65f,
				Projectile.rotation + _shineRotations[i] + MathHelper.PiOver2, new Vector2(shineAlpha.Width / 2, shineAlpha.Height),
				_shineScales[i] * MathHelper.Lerp(1.05f, 0.85f, EaseFunction.EaseQuinticIn.Ease(Projectile.timeLeft / 50f)), SpriteEffects.None);
	}
}