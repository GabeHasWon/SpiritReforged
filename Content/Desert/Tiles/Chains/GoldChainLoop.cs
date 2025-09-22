/*using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;

namespace SpiritReforged.Content.Desert.Tiles.Chains;

public class GoldChainLoop : ChainLoop
{
	public class GoldPhysicsChain : PhysicsChain
	{
		public static readonly Asset<Texture2D> Censer = DrawHelpers.RequestLocal(typeof(ChainLoop), "Censer", false);

		public override void AI()
		{
			base.AI();

			if (Main.rand.NextBool(3))
			{
				var spawn = Main.rand.NextVector2FromRectangle(Projectile.Hitbox);
				float scale = Main.rand.NextFloat(0.5f, 1.5f);
				var velocity = (Vector2.UnitY * -1f).RotatedBy(Math.Sin(Main.timeForVisualEffects / 20f) / 3);

				ParticleHandler.SpawnParticle(new SteamParticle(spawn, velocity, scale, 60, ParticleLayer.AbovePlayer) { Color = Color.White * 0.2f });
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			bool value = base.PreDraw(ref lightColor);

			var texture = Censer.Value;
			float rotation = ((chain == null) ? 0 : chain.EndRotation) + MathHelper.PiOver2;

			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), rotation, texture.Size() / 2, Projectile.scale, default);
			return value;
		}
	}

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		ChainType = ModContent.ProjectileType<GoldPhysicsChain>();
	}
}*/