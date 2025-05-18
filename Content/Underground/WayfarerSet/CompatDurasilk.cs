using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.ModCompat.Classic;

namespace SpiritReforged.Content.Underground.WayfarerSet;

[FromClassic("DurasilkSheaf")]
internal class CompatDurasilk : ModItem
{
	public override bool IsLoadingEnabled(Mod mod) => CrossMod.Classic.Enabled;

	public override void SetDefaults()
	{
		Item.autoReuse = false;
		Item.useTurn = true;
		Item.width = Item.height = 16;
		Item.rare = ItemRarityID.Blue;
		Item.maxStack = Item.CommonMaxStack;
		Item.value = Item.buyPrice(0, 0, 3, 0);
	}

	public override void AddRecipes()
	{
		Recipe.Create(ModContent.ItemType<WayfarerBody>(), 1)
			.AddIngredient(Type, 1)
			.AddRecipeGroup(RecipeGroupID.IronBar, 2)
			.AddTile(TileID.Anvils)
			.Register();

		Recipe.Create(ModContent.ItemType<WayfarerHead>(), 1)
			.AddIngredient(Type, 1)
			.AddRecipeGroup(RecipeGroupID.IronBar, 1)
			.AddTile(TileID.Anvils)
			.Register();

		Recipe.Create(ModContent.ItemType<WayfarerLegs>(), 1)
			.AddIngredient(Type, 1)
			.AddRecipeGroup(RecipeGroupID.IronBar, 1)
			.AddTile(TileID.Anvils)
			.Register();
	}
}
