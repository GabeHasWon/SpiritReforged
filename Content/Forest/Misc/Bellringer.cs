using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Forest.Misc;

public class Bellringer : ClubItem
{
	public class BellringerProj : BaseClubProj
	{
		public static readonly SoundStyle Bell = new("SpiritReforged/Assets/SFX/Item/BellGong");

		public const int SHOCKWAVE_SCALE = 250;
		private float _bellRotation;

		public override float WindupTimeRatio => 0.8f;

		public BellringerProj() : base(new Vector2(118, 118)) { }

		public override void SafeSetStaticDefaults() => Main.projFrames[Type] = 2;

		public override void OnSwingStart()
		{
			if (!Main.dedServ)
				CreateTrail(TrailSystem.ProjectileRenderer);
		}

		public void CreateTrail(ProjectileTrailRenderer renderer)
		{
			float trailDist = 52 * MeleeSizeModifier;
			float trailWidth = 60 * MeleeSizeModifier;
			float angleRangeMod = 1f;
			float rotOffset = 0;

			if (FullCharge)
			{
				trailDist *= 1.1f;
				trailWidth *= 1.1f;
				angleRangeMod = 1.2f;
				rotOffset = -MathHelper.PiOver4 / 2;
			}

			SwingTrailParameters parameters = new(AngleRange * angleRangeMod, -HoldAngle_Final + rotOffset, trailDist, trailWidth)
			{
				Color = new Color(90, 70, 40),
				SecondaryColor = Color.Black * 0.8f,
				TrailLength = 0.2f,
				Intensity = 1,
			};

			renderer.CreateTrail(Projectile, new SwingTrail(Projectile, parameters, GetSwingProgressStatic, SwingTrail.BasicSwingShaderParams));
			renderer.CreateTrail(Projectile, new SwingTrail(Projectile, parameters with { Distance = trailDist + 30, Width = trailWidth + 10 }, GetSwingProgressStatic, SwingTrail.BasicSwingShaderParams));
		}

		public override void SafeAI()
		{
			float delta = Projectile.rotation - Projectile.oldRot[1];

			_bellRotation = _bellRotation + delta * 1.5f - Owner.velocity.X / 150f + Owner.velocity.Y / 100f * Owner.direction;
			_bellRotation = Utils.AngleLerp(_bellRotation, Projectile.rotation, 0.1f);
		}

		public override void OnSmash(Vector2 position)
		{
			const int duration = 25;

			TrailSystem.ProjectileRenderer.DissolveTrail(Projectile);
			Collision.HitTiles(Projectile.position, Vector2.UnitY, Projectile.width, Projectile.height);

			if (!Main.dedServ)
			{
				float scale = Charge;

				for (int i = 0; i < 8; i++)
				{
					Vector2 velocity = Main.rand.NextVector2CircularEdge(5, 5) * scale;
					float height = Main.rand.NextFloat(1.5f, 3.0f);

					ParticleHandler.SpawnParticle(new ImpactLine(position, velocity, Color.Goldenrod.Additive(), new Vector2(1, height) * scale, duration));
					ParticleHandler.SpawnParticle(new ImpactLine(position, velocity, Color.White.Additive(), new Vector2(1, height) * 0.5f * scale, duration));

					Vector2 emberVelocity = Main.rand.NextVector2Circular(5, 5) * scale;
					ParticleHandler.SpawnParticle(new EmberParticle(position, emberVelocity, Color.White.Additive(100), Color.Brown.Additive(100), 0.5f, duration * 2, 2));
				}

				float scaleModifier = (SHOCKWAVE_SCALE + 10) * 2;
				ParticleHandler.SpawnParticle(new PulseCircle(position, Color.Goldenrod.Additive() * 0.3f, 0.12f, scale * scaleModifier, duration));
				ParticleHandler.SpawnParticle(new PulseCircle(position, Color.White.Additive(), 0.03f, scale * scaleModifier, duration));

				ParticleHandler.SpawnParticle(new PulseCircle(position, Color.PaleVioletRed.Additive() * 0.2f, 0.05f, scale * scaleModifier, duration + 10));

				for (int i = 0; i < (FullCharge ? 6 : 2); i++)
				{
					ParticleHandler.SpawnParticle(new CompositeSmoke(position + Main.rand.NextVector2Circular(30, 30), -Vector2.UnitY, Color.SaddleBrown, 60, false, false));
					ParticleHandler.SpawnParticle(new SmallCompositeSmoke(position + Main.rand.NextVector2Circular(30, 30), -Vector2.UnitY, Color.SandyBrown, 60, false, false));
				}

				SoundEngine.PlaySound(SoundID.DD2_SkeletonHurt with { Pitch = 0.5f }, Projectile.Center);
				SoundEngine.PlaySound(Bell with { PitchVariance = 0.2f, Volume = Charge }, Projectile.Center);
			}
		}

		public override bool? CanDamage() => CheckAIState(AIStates.CHARGING) ? false : null;

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if (CheckAIState(AIStates.POST_SMASH))
			{
				Vector2 center = projHitbox.Center();
				int scale = (int)(SHOCKWAVE_SCALE * Charge);

				Rectangle inflatedArea = new((int)center.X - scale / 2, (int)center.Y - scale / 2, scale, scale);

				return targetHitbox.Intersects(inflatedArea);
			}
			else
			{
				return base.Colliding(projHitbox, targetHitbox);
			}
		}

		public override bool OverrideDraw(SpriteBatch spriteBatch, Texture2D texture, Color lightColor, Vector2 handPosition, Vector2 drawPosition)
		{
			float trailOpacity = 1;
			if (AllowedAftertrailDraw(ref trailOpacity))
				DrawAftertrail(texture, lightColor * trailOpacity, drawPosition);

			Texture2D white = TextureColorCache.ColorSolid(texture, Color.White);
			Rectangle poleSource = texture.Frame(1, Main.projFrames[Type], 0, 0, 0, -2);
			Rectangle bellSource = new(56, 120, 62, 62);

			float bellRotation = _bellRotation;
			Vector2 bellOrigin = (Effects == SpriteEffects.FlipHorizontally) ? new Vector2(bellSource.Width, bellSource.Height) : new Vector2(0, bellSource.Height);
			Vector2 bellPosition = handPosition + (new Vector2(46, -46) * TotalScale).RotatedBy((Effects == SpriteEffects.FlipHorizontally) ? Projectile.rotation - MathHelper.PiOver2 : Projectile.rotation) - Main.screenPosition;

			if (!CheckAIState(AIStates.POST_SMASH))
			{
				DrawHelpers.DrawOutline(default, default, default, default, (offset) =>
					Main.EntitySpriteDraw(white, bellPosition + offset, bellSource, Projectile.GetAlpha(Color.Goldenrod.Additive(100)) * Charge * 0.25f, bellRotation, bellOrigin, TotalScale, Effects, 0));
			}

			Main.EntitySpriteDraw(texture, bellPosition, bellSource, Projectile.GetAlpha(lightColor), bellRotation, bellOrigin, TotalScale, Effects, 0);
			Main.EntitySpriteDraw(texture, drawPosition, poleSource, Projectile.GetAlpha(lightColor), Projectile.rotation, HoldPoint, TotalScale, Effects, 0);

			//Flash when fully charged
			if (CheckAIState(AIStates.CHARGING) && _flickerTime > 0)
			{
				
				float alpha = EaseFunction.EaseQuadIn.Ease(EaseFunction.EaseSine.Ease(_flickerTime / (float)MAX_FLICKERTIME));
				var color = Color.Lerp(Color.RosyBrown, Color.White, alpha * alpha).Additive();

				Main.EntitySpriteDraw(white, bellPosition, bellSource, color * alpha * 2, bellRotation, bellOrigin, TotalScale, Effects, 0);
				Main.EntitySpriteDraw(white, drawPosition, poleSource, color * alpha * 2, Projectile.rotation, HoldPoint, TotalScale, Effects, 0);
			}

			return true;
		}
	}

	internal override float DamageScaling => 2f;
	internal override float KnockbackScaling => 1.4f;

	public override void SafeSetDefaults()
	{
		Item.damage = 50;
		Item.knockBack = 7;
		Item.width = 60;
		Item.height = 60;
		Item.crit = 4;
		Item.value = Item.sellPrice(0, 1, 50, 0);
		Item.rare = ItemRarityID.Orange;
		Item.shoot = ModContent.ProjectileType<BellringerProj>();

		ChargeTime = 30;
		SwingTime = 24;
	}

	//public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.SoulofMight, 10).AddIngredient(ItemID.BeeWax, 14).AddIngredient(ItemID.Stinger, 8).AddTile(TileID.MythrilAnvil).Register();
}