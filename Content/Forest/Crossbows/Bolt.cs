using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Content.Forest.Crossbows;

public class Bolt : ModItem
{
	public class BoltProjectile : ModProjectile, IDrawPixelated
	{
		public override LocalizedText DisplayName => ModContent.GetInstance<Bolt>().DisplayName;

		public override string Texture => ModContent.GetInstance<Bolt>().Texture;

		private VertexTrail _trail;

		public override void SetDefaults()
		{
			Projectile.CloneDefaults(ProjectileID.WoodenArrowFriendly);
			Projectile.extraUpdates = 1;
			Projectile.aiStyle = -1;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

			if (!Main.dedServ)
			{
				_trail ??= new VertexTrail(new LightColorTrail(Color.White * 0.5f, Color.Transparent), new TriangleCap(), new EntityTrailPosition(Projectile), new DefaultShader(), 6 * Projectile.scale, 75);
				_trail.Update();
			}

			if (++Projectile.ai[0] > 60)
				Projectile.velocity.Y += 0.1f;
		}

		public override void OnKill(int timeLeft)
		{
			Vector2 velocity = Projectile.velocity * 0.2f;

			for (int i = 0; i < 5; i++)
				Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.WoodFurniture, velocity.X, velocity.Y);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, new Vector2(texture.Width / 2, Projectile.height / 2), Projectile.scale, 0);

			return false;
		}

		void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch) => _trail?.Draw(TrailSystem.TrailShaders, Main.spriteBatch.GraphicsDevice, Matrix.Identity);
	}

	public override void SetDefaults()
    {
		Item.ammo = Type;
		Item.maxStack = Item.CommonMaxStack;
		Item.consumable = true;
		Item.damage = 10;
		Item.knockBack = 1f;
		Item.rare = ItemRarityID.White;
		Item.shoot = ModContent.ProjectileType<BoltProjectile>();
	}
}