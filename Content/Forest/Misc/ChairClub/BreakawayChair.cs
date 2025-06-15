using SpiritReforged.Common.ItemCommon.Abstract;

namespace SpiritReforged.Content.Forest.Misc.ChairClub;

public class BreakawayChair : ClubItem
{
	public override void SafeSetDefaults()
	{
		Item.damage = 5;
		Item.knockBack = 5;
		ChargeTime = 20;
		SwingTime = 24;
		Item.width = 60;
		Item.height = 60;
		Item.crit = 4;
		Item.value = Item.sellPrice(0, 0, 0, 76);
		Item.rare = ItemRarityID.White;
		Item.shoot = ModContent.ProjectileType<BreakawayChairProj>();
		Item.maxStack = Item.CommonMaxStack;
	}

	public override void AddRecipes() => CreateRecipe().AddRecipeGroup(RecipeGroupID.Wood, 4).AddTile(TileID.WorkBenches).Register();
}