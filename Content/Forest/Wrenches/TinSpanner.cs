using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Content.Forest.Wrenches;

public class TinSpanner : CopperSpanner
{
	public class TinSpannerSwing : CopperSpannerSwing, IDrawPixelated
	{
		public override LocalizedText DisplayName => ModContent.GetInstance<TinSpanner>().DisplayName;
		public override string Texture => ModContent.GetInstance<TinSpanner>().Texture;

		void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch) => DrawPixelatedSmear(spriteBatch, new Color(187, 165, 124));
	}

	public override void SetDefaults()
	{
		base.SetDefaults();

		Item.damage = 10;
		Item.useTime = Item.useAnimation = 18;
		Item.shoot = ModContent.ProjectileType<TinSpannerSwing>();
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.TinBar, 12).AddTile(TileID.Anvils).Register();
}