using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Content.Forest.Walls;

namespace SpiritReforged.Content.Crossmod.Spooky.SpookyForest;

public class TrellisSpookyWood : Trellis
{
	public override bool IsLoadingEnabled(Mod mod) => CrossMod.Spooky.Enabled;

	public override void AddItemRecipes(ModItem item)
	{
		if (!CrossMod.Spooky.TryFind("SpookyWoodItem", out ModItem sp))
			return;

		item.CreateRecipe(4).AddIngredient(sp.Type).AddTile(TileID.Sawmill).Register();
		item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<TrellisSpookyWoodTwo>(), 4).AddTile(TileID.Sawmill).Register();
	}

	public override void AddEntry()
	{
		AddMapEntry(new Color(56, 36, 22));
		DustType = DustID.WoodFurniture;
	}
}

public class TrellisSpookyWoodTwo : TrellisSpookyWood
{
	public override void AddItemRecipes(ModItem item)
	{
		if (!CrossMod.Spooky.TryFind("SpookyWoodItem", out ModItem sp))
			return;

		item.CreateRecipe(4).AddIngredient(sp.Type).AddTile(TileID.Sawmill).Register();
		item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<TrellisSpookyWood>(), 4).AddTile(TileID.Sawmill).Register();
	}
}

public class TrellisOldBirch : Trellis
{
	public override bool IsLoadingEnabled(Mod mod) => CrossMod.Spooky.Enabled;

	public override void AddItemRecipes(ModItem item)
	{
		if (!CrossMod.Spooky.TryFind("BirchWoodItem", out ModItem sp))
			return;

		item.CreateRecipe(4).AddIngredient(sp.Type).AddTile(TileID.Sawmill).Register();
		item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<TrellisOldBirchTwo>(), 4).AddTile(TileID.Sawmill).Register();
	}

	public override void AddEntry()
	{
		AddMapEntry(new Color(139, 129, 121));
		DustType = DustID.Web;
	}
}

public class TrellisOldBirchTwo : TrellisOldBirch
{
	public override void AddItemRecipes(ModItem item)
	{
		if (!CrossMod.Spooky.TryFind("BirchWoodItem", out ModItem sp))
			return;

		item.CreateRecipe(4).AddIngredient(sp.Type).AddTile(TileID.Sawmill).Register();
		item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<TrellisOldBirch>(), 4).AddTile(TileID.Sawmill).Register();
	}
}