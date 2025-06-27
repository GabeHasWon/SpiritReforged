using SpiritReforged.Common.Visuals.Glowmasks;

namespace SpiritReforged.Content.Underground.Moss.MossFlasks;

[AutoloadGlowmask("Method:Content.Underground.Moss.MossFlasks.FlaskHelium Glow")]
public class FlaskHelium : MossFlask
{
	public static Color Glow(object obj) => Main.DiscoColor;

	public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
	{
		var glow = GlowmaskItem.ItemIdToGlowmask[Type].Glowmask.Value;
		spriteBatch.Draw(glow, position, frame, Item.GetAlpha(Glow(null)), 0, origin, scale, default, 0);
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.RainbowMoss, 3).AddIngredient(ItemID.Bottle).Register();
}

public class FlaskHeliumProjectile : MossFlaskProjectile
{
	public override MossConversion Conversion => new(TileID.RainbowMoss, TileID.RainbowMossBrick);
	public override void CreateDust(int type)
	{
		for (int i = 0; i < 8; i++)
			Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.RainbowRod, newColor: Main.DiscoColor).noGravity = true;
	}
}