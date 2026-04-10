using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.ProjectileCommon.Abstract;

namespace SpiritReforged.Content.Forest.Rapiers;

public class TungstenSabre : SilverRapier
{
	public class TungstenSabreSwing : SilverRapierSwing
	{
		public override string Texture => ModContent.GetInstance<TungstenSabre>().Texture;
		public override LocalizedText DisplayName => ModContent.GetInstance<TungstenSabre>().DisplayName;
	}

	public override void SetDefaults()
	{
		Item.DefaultToSpear(ModContent.ProjectileType<TungstenSabreSwing>(), 1f, 22);
		Item.SetShopValues(ItemRarityColor.Blue1, Item.sellPrice(silver: 50));
		Item.damage = 17;
		Item.knockBack = 3.5f;
		Item.UseSound = RapierProjectile.DefaultSwing;
		Item.autoReuse = true;
		MoRHelper.SetSlashBonus(Item);
	}

	public override void AddRecipes() => CreateRecipe().AddRecipeGroup(RecipeGroupID.Wood, 4).AddIngredient(ItemID.TungstenBar, 6).AddTile(TileID.Anvils).Register();
}