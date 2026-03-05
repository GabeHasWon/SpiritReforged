namespace SpiritReforged.Content.Underground.Moss.MossFlasks;

public class FlaskBlue : MossFlask
{
	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.BlueMoss, 3).AddIngredient(ItemID.Bottle).Register();
}

public class FlaskBlueProjectile : MossFlaskProjectile
{
	public override MossConversion Conversion => new(TileID.BlueMoss, TileID.BlueMossBrick);
	public override void CreateDust(int type) => base.CreateDust(DustID.BlueMoss);
}