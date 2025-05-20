using SpiritReforged.Common.Visuals.Glowmasks;

namespace SpiritReforged.Content.Underground.Moss.MossFlasks;

[AutoloadGlowmask("255,255,255")]
public class FlaskArgon : MossFlask
{
	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.ArgonMoss, 3).AddIngredient(ItemID.Bottle).Register();
}

public class FlaskArgonProjectile : MossFlaskProjectile
{
	public override MossConversion Conversion => new(TileID.ArgonMoss, TileID.ArgonMossBrick);
	public override void CreateDust(int type) => base.CreateDust(DustID.ArgonMoss);
}