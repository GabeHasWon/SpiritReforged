using SpiritReforged.Common.Subclasses.Greatshields;

namespace SpiritReforged.Content.Forest.Shields;

public class StaminaRing : ModItem
{
	public const int SHIELD_BONUS = 10;
	public const float SHIELD_REGEN_MULTIPLIER = 0.2f;

	public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(SHIELD_BONUS, (int)(SHIELD_REGEN_MULTIPLIER * 100));

	public override void SetDefaults()
	{
		Item.width = 30;
		Item.height = 34;
		Item.value = Item.sellPrice(gold: 1);
		Item.rare = ItemRarityID.Blue;
		Item.accessory = true;
	}

	public override void UpdateAccessory(Player player, bool hideVisual)
	{
		if (player.TryGetModPlayer(out GreatshieldPlayer shieldPlayer))
		{
			shieldPlayer.ShieldHealthStat.Flat += SHIELD_BONUS;
			shieldPlayer.ShieldRegenStat += SHIELD_REGEN_MULTIPLIER;
		}
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Emerald).AddRecipeGroup("GoldBars", 4).AddIngredient(ItemID.JungleSpores, 3).AddTile(TileID.Anvils).Register();
}