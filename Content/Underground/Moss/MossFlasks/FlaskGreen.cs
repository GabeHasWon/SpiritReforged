namespace SpiritReforged.Content.Underground.Moss.MossFlasks;

public class FlaskGreen : MossFlask
{
	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.GreenMoss, 3).AddIngredient(ItemID.Bottle).Register();
}

public class FlaskGreenProjectile : MossFlaskProjectile
{
	public override MossConversion Conversion => new(TileID.GreenMoss, TileID.GreenMossBrick);
	public override void CreateDust(int type) => base.CreateDust(DustID.GreenMoss);
}