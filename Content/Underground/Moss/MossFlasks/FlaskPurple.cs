namespace SpiritReforged.Content.Underground.Moss.MossFlasks;

public class FlaskPurple : MossFlask
{
	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.PurpleMoss, 3).AddIngredient(ItemID.Bottle).Register();
}

public class FlaskPurpleProjectile : MossFlaskProjectile
{
	public override MossConversion Conversion => new(TileID.PurpleMoss, TileID.PurpleMossBrick);
	public override void CreateDust(int type) => base.CreateDust(DustID.PurpleMoss);
}