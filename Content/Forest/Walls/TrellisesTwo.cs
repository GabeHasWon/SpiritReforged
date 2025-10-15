using SpiritReforged.Common.ItemCommon;

namespace SpiritReforged.Content.Forest.Walls;

public class TrellisTwo : Trellis
{
	public override void AddItemRecipes(ModItem item) => item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<Trellis>(), 4).AddTile(TileID.Sawmill).Register();
}

public class TrellisTwoBoreal : TrellisBoreal
{
	public override void AddItemRecipes(ModItem item) => item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<TrellisBoreal>(), 4).AddTile(TileID.Sawmill).Register();
}

public class TrellisTwoEbonwood : TrellisEbonwood
{
	public override void AddItemRecipes(ModItem item) => item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<TrellisEbonwood>(), 4).AddTile(TileID.Sawmill).Register();
}

public class TrellisTwoMahogany : TrellisMahogany
{
	public override void AddItemRecipes(ModItem item) => item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<TrellisMahogany>(), 4).AddTile(TileID.Sawmill).Register();
}

public class TrellisTwoPalm : TrellisPalm
{
	public override void AddItemRecipes(ModItem item) => item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<TrellisPalm>(), 4).AddTile(TileID.Sawmill).Register();
}

public class TrellisTwoPearlwood : TrellisPearlwood
{
	public override void AddItemRecipes(ModItem item) => item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<TrellisPearlwood>(), 4).AddTile(TileID.Sawmill).Register();
}

public class TrellisTwoShadewood : TrellisShadewood
{
	public override void AddItemRecipes(ModItem item) => item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<TrellisShadewood>(), 4).AddTile(TileID.Sawmill).Register();
}