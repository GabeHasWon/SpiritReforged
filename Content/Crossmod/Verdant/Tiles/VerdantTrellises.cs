using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Content.Forest.Walls;

namespace SpiritReforged.Content.Crossmod.Verdant.Tiles;

public class LushWoodTrellis : Trellis
{
	public override bool IsLoadingEnabled(Mod mod) => CrossMod.Verdant.Enabled;

	public override void AddItemRecipes(ModItem item)
	{
		if (!CrossMod.Verdant.TryFind("VerdantWoodBlock", out ModItem sp))
			return;

		item.CreateRecipe(4).AddIngredient(sp.Type).AddTile(TileID.Sawmill).Register();
		item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<LushWoodTrellisTwo>(), 4).AddTile(TileID.Sawmill).Register();
	}

	public override void AddEntry()
	{
		AddMapEntry(new Color(114, 75, 40));
		DustType = DustID.t_BorealWood;
	}
}

public class LushWoodTrellisTwo : LushWoodTrellis
{
	public override void AddItemRecipes(ModItem item)
	{
		if (!CrossMod.Verdant.TryFind("VerdantWoodBlock", out ModItem sp))
			return;

		item.CreateRecipe(4).AddIngredient(sp.Type).AddTile(TileID.Sawmill).Register();
		item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<LushWoodTrellis>(), 4).AddTile(TileID.Sawmill).Register();
	}
}
