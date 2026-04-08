using SpiritReforged.Common.ModCompat;

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
		Item.damage = 17;
		Item.knockBack = 3;
		Item.useTime = Item.useAnimation = 22;
		Item.DamageType = DamageClass.Melee;
		Item.width = Item.height = 46;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.value = Item.sellPrice(gold: 1);
		Item.rare = ItemRarityID.Blue;
		Item.UseSound = SoundID.Item1;
		Item.shoot = ModContent.ProjectileType<TungstenSabreSwing>();
		Item.shootSpeed = 1f;
		Item.autoReuse = true;
		Item.useTurn = true;
		Item.noUseGraphic = true;
		Item.noMelee = true;
		MoRHelper.SetSlashBonus(Item);
	}

	public override void AddRecipes() => CreateRecipe().AddRecipeGroup(RecipeGroupID.Wood, 4).AddIngredient(ItemID.TungstenBar, 6).AddTile(TileID.Anvils).Register();
}