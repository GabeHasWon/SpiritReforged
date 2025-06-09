using SpiritReforged.Common.Visuals.Glowmasks;

namespace SpiritReforged.Content.Underground.Moss.MossFlasks;

[AutoloadGlowmask("255,255,255")]
public class FlaskLava : MossFlask
{
	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.LavaMoss, 3).AddIngredient(ItemID.Bottle).Register();
}

public class FlaskLavaProjectile : MossFlaskProjectile
{
	public override MossConversion Conversion => new(TileID.LavaMoss, TileID.LavaMossBrick);
	public override void CreateDust(int type) => base.CreateDust(DustID.LavaMoss);
}