using SpiritReforged.Content.Ocean.Items.Reefhunter.Buffs;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.PlayerCommon;

namespace SpiritReforged.Content.Ocean.Items.Reefhunter;

public class PendantOfTheOcean : AccessoryItem, ITimerItem
{
	public override void Load() => DoubleTapPlayer.OnDoubleTap += DoubleTapUp;

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		string down = Main.ReversedUpDownArmorSetBonuses ? "UP" : "DOWN";

		foreach (TooltipLine line in tooltips)
			if (line.Mod == "Terraria" && line.Name == "Tooltip0")
				line.Text = line.Text.Replace("{0}", down);
	}

	public override void SetDefaults()
	{
		Item.width = 26;
		Item.height = 48;
		Item.rare = ItemRarityID.Green;
		Item.value = Item.buyPrice(0, 0, 80, 0);
		Item.DamageType = DamageClass.Melee;
		Item.accessory = true;
		Item.knockBack = 5f;
	}

	private void DoubleTapUp(Player player, int keyDir)
	{
		if (keyDir == 0 && player.HasAccessory<PendantOfTheOcean>() && player.ItemTimer<PendantOfTheOcean>() <= 0)
		{
			player.AddBuff(ModContent.BuffType<EmpoweredSwim>(), 60 * 10);
			player.SetItemTimer<PendantOfTheOcean>(60 * 45);
		}
	}

	public int TimerCount() => 1;

	public override void AddRecipes()
	{
		var recipe = CreateRecipe();
		recipe.AddIngredient(ModContent.ItemType<IridescentScale>(), 4);
		recipe.AddIngredient(ModContent.ItemType<SulfurDeposit>(), 10);
		recipe.AddRecipeGroup(RecipeGroupID.IronBar, 5);
		recipe.AddTile(TileID.Anvils);
		recipe.Register();
	}
}
