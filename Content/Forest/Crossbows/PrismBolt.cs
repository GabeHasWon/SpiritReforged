using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Desert.ScarabBoss.Items.Projectiles;
using Terraria;

namespace SpiritReforged.Content.Forest.Crossbows;

public class PrismBolt : ModItem
{
	public class RainbowColorTrail : ITrailColor
	{
		public Color GetColourAt(float distanceFromStart, float trailLength, List<Vector2> points, Vector2 curPoint)
		{
			Color color = Main.hslToRgb(((float)Main.timeForVisualEffects / 100f + distanceFromStart * trailLength) % 1, 1, 0.7f);
			return color;
		}
	}

	public class PrismBoltProjectile : ModProjectile, IDrawPixelated
	{
		public override LocalizedText DisplayName => ModContent.GetInstance<PrismBolt>().DisplayName;

		public override string Texture => ModContent.GetInstance<PrismBolt>().Texture;

		private VertexTrail[] _trails;

		public override void SetDefaults()
		{
			Projectile.CloneDefaults(ProjectileID.WoodenArrowFriendly);
			Projectile.extraUpdates = 1;
			Projectile.penetrate = 2;
			Projectile.aiStyle = -1;
			Projectile.stopsDealingDamageAfterPenetrateHits = true;
		}

		public override void AI()
		{
			if (!Main.dedServ)
			{
				_trails ??=
					[
						new VertexTrail(new RainbowColorTrail(), new RoundCap(), new EntityTrailPosition(Projectile), new DefaultShader(), 6 * Projectile.scale, 75),
						new VertexTrail(new RainbowColorTrail(), new RoundCap(), new EntityTrailPosition(Projectile), new DefaultShader(), 8 * Projectile.scale, 85) { Opacity = 0.5f}
					];

				foreach (VertexTrail trail in _trails)
					trail.Update();
			}

			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

			if (++Projectile.ai[0] > 60)
				Projectile.velocity.Y += 0.1f;
		}

		public override void OnKill(int timeLeft)
		{
			Vector2 velocity = Projectile.velocity * 0.2f;

			for (int i = 0; i < 5; i++)
				Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.CrystalPulse, velocity.X, velocity.Y).noGravity = true;

			if (Projectile.owner == Main.myPlayer)
				Projectile.NewProjectileDirect(Projectile.GetSource_Death(), Projectile.Center, velocity, ModContent.ProjectileType<AdornedFlash>(), Projectile.damage, Projectile.knockBack * 0.25f, Projectile.owner);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			Projectile.Bounce(Projectile.velocity, 0.8f);
			Projectile.timeLeft = Math.Min(Projectile.timeLeft, 10); //Detonate after 10 ticks
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			Projectile.Bounce(oldVelocity, 0.8f);
			Projectile.timeLeft = Math.Min(Projectile.timeLeft, 10); //Detonate after 10 ticks

			return !(Projectile.penetrate-- > 0);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, new Vector2(texture.Width / 2, Projectile.height / 2), Projectile.scale, 0);

			return false;
		}

		void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch)
		{
			foreach (VertexTrail trail in _trails)
				trail?.Draw(TrailSystem.TrailShaders, Main.spriteBatch.GraphicsDevice, Matrix.Identity);
		}
	}

	public override void SetDefaults()
    {
		Item.ammo = ModContent.ItemType<Bolt>();
		Item.maxStack = Item.CommonMaxStack;
		Item.consumable = true;
		Item.damage = 10;
		Item.knockBack = 1f;
		Item.rare = ItemRarityID.Blue;
		Item.shoot = ModContent.ProjectileType<PrismBoltProjectile>();
	}
}