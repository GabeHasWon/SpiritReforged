using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Content.Forest.Crossbows;

public class StingerBolt : ModItem
{
	public class StingerBoltProjectile : Bolt.BoltProjectile, IDrawPixelated
	{
		public override LocalizedText DisplayName => ModContent.GetInstance<StingerBolt>().DisplayName;

		public override string Texture => ModContent.GetInstance<StingerBolt>().Texture;

		private VertexTrail _trail;

		public override void SetDefaults()
		{
			Projectile.CloneDefaults(ProjectileID.WoodenArrowFriendly);
			Projectile.extraUpdates = 1;
			Projectile.aiStyle = -1;
			Projectile.penetrate = 2;
		}

		public override void PostAI() //Update trails regardless of what PreAI returns
		{
			if (!Main.dedServ)
			{
				if (Stationary)
				{
					_trail.Dissolve();
				}
				else
				{
					_trail ??= new VertexTrail(new LightColorTrail(Color.RosyBrown * 0.5f, Color.Transparent), new RoundCap(), new EntityTrailPosition(Projectile), new DefaultShader(), 6 * Projectile.scale, 120);
				}

				_trail.Update();
			}
		}

		public override void OnKill(int timeLeft)
		{
			Vector2 velocity = Projectile.velocity * 0.2f;

			for (int i = 0; i < 5; i++)
				Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Ash, velocity.X, velocity.Y);
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