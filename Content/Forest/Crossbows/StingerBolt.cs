using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Content.Forest.Crossbows;

public class StingerBolt : ModItem
{
	public class StingerBoltProjectile : ModProjectile, IDrawPixelated
	{
		public override LocalizedText DisplayName => ModContent.GetInstance<StingerBolt>().DisplayName;

		public override string Texture => ModContent.GetInstance<StingerBolt>().Texture;

		private VertexTrail _trail;

		public override void SetDefaults()
		{
			Projectile.CloneDefaults(ProjectileID.WoodenArrowFriendly);
			Projectile.extraUpdates = 1;
			Projectile.penetrate = 2;
			Projectile.aiStyle = -1;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

			if (!Main.dedServ)
			{
				_trail ??= new VertexTrail(new LightColorTrail(Color.RosyBrown * 0.5f, Color.Transparent), new RoundCap(), new EntityTrailPosition(Projectile), new DefaultShader(), 6 * Projectile.scale, 75);
				_trail.Update();
			}

			if (++Projectile.ai[0] > 60)
				Projectile.velocity.Y += 0.1f;
		}

		public override void OnKill(int timeLeft) => base.OnKill(timeLeft);

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
		Item.ammo = ModContent.ItemType<Bolt>();
		Item.maxStack = Item.CommonMaxStack;
		Item.consumable = true;
		Item.damage = 7;
		Item.knockBack = 1f;
		Item.rare = ItemRarityID.Blue;
		Item.shoot = ModContent.ProjectileType<StingerBoltProjectile>();
	}

	//public override void AddRecipes() => CreateRecipe(20).AddIngredient(ModContent.ItemType<Bolt>()).AddIngredient(ItemID.Stinger).AddTile(TileID.WorkBenches).Register();
}