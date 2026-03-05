using SpiritReforged.Common.Misc;
using SpiritReforged.Content.Underground.Moss.Oganesson;
using SpiritReforged.Content.Underground.Moss.Radon;

namespace SpiritReforged.Content.Underground.Moss;

/// <summary> Adds ecto-mist glowing moss recipes. </summary>
internal class MossExchange : ModSystem
{
	public const string Group = "GlowingMoss";

	/// <summary> All glowing moss types except for <see cref="ItemID.RainbowMoss"/>. </summary>
	public static readonly HashSet<int> GlowingMossTypes = [ItemID.ArgonMoss, ItemID.KryptonMoss, ItemID.LavaMoss, ItemID.VioletMoss, ItemID.XenonMoss, ModContent.ItemType<OganessonMossItem>(), ModContent.ItemType<RadonMossItem>()];

	public override void AddRecipeGroups() => RecipeGroup.RegisterGroup(Group, Recipes.BaseGroup(ItemID.ArgonMoss, [.. GlowingMossTypes]));
	public override void AddRecipes()
	{
		var c = Condition.InGraveyard;

		foreach (int item in GlowingMossTypes)
			Recipe.Create(item).AddRecipeGroup(Group).AddTile(TileID.WorkBenches).AddCondition(c).Register();
	}
}