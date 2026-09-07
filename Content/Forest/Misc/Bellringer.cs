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
		private Vector2 _bellVelocity;

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
				Color = Color.White,
				SecondaryColor = Color.SandyBrown,
				TrailLength = 0.33f,
				Intensity = 0.75f,
			};

			renderer.CreateTrail(Projectile, new SwingTrail(Projectile, parameters, GetSwingProgressStatic, SwingTrail.BasicSwingShaderParams));
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
				}

				ParticleHandler.SpawnParticle(new PulseCircle(position, Color.Goldenrod.Additive(), Color.PaleVioletRed.Additive(), 0.12f, scale * 400, duration));
				ParticleHandler.SpawnParticle(new PulseCircle(position, Color.White.Additive(), 0.05f, scale * 400, duration));
			}

			SoundEngine.PlaySound(SoundID.DD2_SkeletonHurt with { Pitch = 0.5f }, Projectile.Center);
			_bellVelocity += Projectile.velocity * 20;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			var position = Vector2.Lerp(Projectile.Center, target.Center, 0.6f);

			for (int i = 0; i < (FullCharge ? 3 : 1); i++)
			{
				ParticleHandler.SpawnParticle(new CompositeSmoke(position + Main.rand.NextVector2Circular(30, 30), -Vector2.UnitY, Color.SaddleBrown, 60, false, false));
				ParticleHandler.SpawnParticle(new SmallCompositeSmoke(position + Main.rand.NextVector2Circular(30, 30), -Vector2.UnitY, Color.SandyBrown, 60, false, false));
			}
		}

		public override bool OverrideDraw(SpriteBatch spriteBatch, Texture2D texture, Color lightColor, Vector2 handPosition, Vector2 drawPosition)
		{
			float trailOpacity = 1;
			if (AllowedAftertrailDraw(ref trailOpacity))
				DrawAftertrail(texture, lightColor * trailOpacity, drawPosition);

			Rectangle poleSource = texture.Frame(1, Main.projFrames[Type], 0, 0, 0, -2);
			Rectangle bellSource = new(56, 120, 62, 62);

			float bellRotation = (_bellVelocity.X + _bellVelocity.Y) / 15f;
			Vector2 bellOrigin = (Effects == SpriteEffects.FlipHorizontally) ? new Vector2(bellSource.Width, bellSource.Height) : new Vector2(0, bellSource.Height);
			Vector2 bellPosition = handPosition + (new Vector2(46, -46) * TotalScale).RotatedBy((Effects == SpriteEffects.FlipHorizontally) ? Projectile.rotation - MathHelper.PiOver2 : Projectile.rotation) - Main.screenPosition;

			Main.EntitySpriteDraw(texture, bellPosition, bellSource, Projectile.GetAlpha(lightColor), Projectile.rotation + bellRotation, bellOrigin, TotalScale, Effects, 0);
			Main.EntitySpriteDraw(texture, drawPosition, poleSource, Projectile.GetAlpha(lightColor), Projectile.rotation, HoldPoint, TotalScale, Effects, 0);

			//Flash when fully charged
			if (CheckAIState(AIStates.CHARGING) && _flickerTime > 0)
			{
				Texture2D flash = TextureColorCache.ColorSolid(texture, Color.White);
				float alpha = EaseFunction.EaseQuadIn.Ease(EaseFunction.EaseSine.Ease(_flickerTime / (float)MAX_FLICKERTIME));
				var color = Color.Lerp(Color.RosyBrown, Color.White, alpha * alpha).Additive();

				Main.EntitySpriteDraw(flash, bellPosition, bellSource, color * alpha * 2, Projectile.rotation + bellRotation, bellOrigin, TotalScale, Effects, 0);
				Main.EntitySpriteDraw(flash, drawPosition, poleSource, color * alpha * 2, Projectile.rotation, HoldPoint, TotalScale, Effects, 0);
			}

			_bellVelocity += new Vector2(-Math.Abs(Owner.velocity.X), Owner.velocity.Y) * Owner.direction / 20f;
			_bellVelocity *= 0.95f;

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