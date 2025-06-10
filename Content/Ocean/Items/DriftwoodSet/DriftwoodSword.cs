using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Content.Ocean.Tiles;

namespace SpiritReforged.Content.Ocean.Items.DriftwoodSet;

public class DriftwoodSword : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 30;
		Item.height = 30;
		Item.value = Item.sellPrice(0, 0, 0, 20);
		Item.rare = ItemRarityID.White;

		Item.damage = 9;
		Item.knockBack = 5f;

		Item.useStyle = ItemUseStyleID.Swing;
		Item.useTime = 22;
		Item.useAnimation = 22;

		Item.DamageType = DamageClass.Melee;
		Item.autoReuse = false;

		Item.UseSound = SoundID.Item1;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(AutoContent.ItemType<Driftwood>(), 16).AddTile(TileID.WorkBenches).Register();
}
