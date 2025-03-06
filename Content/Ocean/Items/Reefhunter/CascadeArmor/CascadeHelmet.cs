namespace SpiritReforged.Content.Ocean.Items.Reefhunter.CascadeArmor;

[AutoloadEquip(EquipType.Head)]
public class CascadeHelmet : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 28;
		Item.height = 24;
		Item.value = 4800;
		Item.rare = ItemRarityID.Blue;
		Item.defense = 3;
	}

	public override bool IsArmorSet(Item head, Item body, Item legs)
		=> (head.type, body.type, legs.type) == (Type, ModContent.ItemType<CascadeChestplate>(), ModContent.ItemType<CascadeLeggings>());
	public override void UpdateArmorSet(Player player)
	{
		player.setBonus = Language.GetTextValue("Mods.SpiritReforged.SetBonuses.Cascade");
		player.GetModPlayer<CascadeArmorPlayer>().setActive = true;
	}

	public override void UpdateEquip(Player player) => player.gills = true;
	public override void AddRecipes()
	{
		CreateRecipe().AddIngredient(ModContent.ItemType<MineralSlag>(), 12).AddTile(TileID.Anvils).Register();
		Recipe.Create(ModContent.ItemType<CascadeChestplate>()).AddIngredient(ModContent.ItemType<MineralSlag>(), 14).AddIngredient(ItemID.SharkFin).AddTile(TileID.Anvils).Register();
		Recipe.Create(ModContent.ItemType<CascadeLeggings>()).AddIngredient(ModContent.ItemType<MineralSlag>(), 10).AddTile(TileID.Anvils).Register();
	}
}
