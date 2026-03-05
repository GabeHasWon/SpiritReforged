using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Content.Ocean.Tiles;

namespace SpiritReforged.Content.Ocean.Items.DriftwoodSet.DriftwoodArmor;

[AutoloadEquip(EquipType.Head)]
public class DriftwoodHelmet : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 28;
		Item.height = 24;
		Item.value = 0;
		Item.rare = ItemRarityID.White;
		Item.defense = 2;
	}

	public override bool IsArmorSet(Item head, Item body, Item legs)
		=> (head.type, body.type, legs.type) == (Type, ModContent.ItemType<DriftwoodChestplate>(), ModContent.ItemType<DriftwoodLeggings>());

	public override void UpdateArmorSet(Player player)
	{
		player.setBonus = Language.GetTextValue("Mods.SpiritReforged.SetBonuses.Driftwood");
		player.fishingSkill += 5;

		if (player.wet)
			player.velocity.Y = MathHelper.Clamp(player.velocity.Y -= 0.35f, -4, 100000);
	}

	public override void AddRecipes()
	{
		int driftwood = AutoContent.ItemType<Driftwood>();

		CreateRecipe().AddIngredient(driftwood, 15).AddTile(TileID.WorkBenches).Register();
		Recipe.Create(ModContent.ItemType<DriftwoodChestplate>()).AddIngredient(driftwood, 20).AddTile(TileID.WorkBenches).Register();
		Recipe.Create(ModContent.ItemType<DriftwoodLeggings>()).AddIngredient(driftwood, 12).AddTile(TileID.WorkBenches).Register();
	}
}
