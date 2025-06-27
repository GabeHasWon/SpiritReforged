namespace SpiritReforged.Content.Underground.Moss.MossFlasks;

public class FlaskBrown : MossFlask
{
	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.BrownMoss, 3).AddIngredient(ItemID.Bottle).Register();
}

public class FlaskBrownProjectile : MossFlaskProjectile
{
	public override MossConversion Conversion => new(TileID.BrownMoss, TileID.BrownMossBrick);
	public override void CreateDust(int type) => base.CreateDust(DustID.BrownMoss);
}