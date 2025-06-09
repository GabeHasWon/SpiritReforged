namespace SpiritReforged.Content.Underground.Moss.MossFlasks;

public class FlaskRed : MossFlask
{
	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.RedMoss, 3).AddIngredient(ItemID.Bottle).Register();
}

public class FlaskRedProjectile : MossFlaskProjectile
{
	public override MossConversion Conversion => new(TileID.RedMoss, TileID.RedMossBrick);
	public override void CreateDust(int type) => base.CreateDust(DustID.RedMoss);
}