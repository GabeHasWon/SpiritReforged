using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Dusts;
using SpiritReforged.Content.Underground.Moss.Radon;

namespace SpiritReforged.Content.Underground.Moss.MossFlasks;

[AutoloadGlowmask("255,255,255")]
public class FlaskRadon : MossFlask
{
	public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<RadonMossItem>(), 3).AddIngredient(ItemID.Bottle).Register();
}

public class FlaskRadonProjectile : MossFlaskProjectile
{
	public override MossConversion Conversion => new((ushort)ModContent.TileType<RadonMoss>(), (ushort)ModContent.TileType<RadonMossGrayBrick>(), (ushort)ModContent.TileType<RadonPlants>());
	public override void CreateDust(int type) => base.CreateDust(ModContent.DustType<RadonMossDust>());
}