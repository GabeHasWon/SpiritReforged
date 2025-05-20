using SpiritReforged.Common.Visuals.Glowmasks;

namespace SpiritReforged.Content.Underground.Moss.MossFlasks;

[AutoloadGlowmask("255,255,255")]
public class FlaskKrypton : MossFlask
{
	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.KryptonMoss, 3).AddIngredient(ItemID.Bottle).Register();
}

public class FlaskKryptonProjectile : MossFlaskProjectile
{
	public override MossConversion Conversion => new(TileID.KryptonMoss, TileID.KryptonMossBrick);
	public override void CreateDust(int type) => base.CreateDust(DustID.KryptonMoss);
}