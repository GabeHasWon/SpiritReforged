namespace SpiritReforged.Content.Forest.Botanist.Items;

[AutoloadEquip(EquipType.Head)]
public class BotanistHat : ModItem
{
	/// <returns> Whether the set bonus related to this item is active on <paramref name="player"/>. </returns>
	public static bool SetActive(Player player) => player.active
		&& player.armor[0].type == ModContent.ItemType<BotanistHat>() 
		&& player.armor[1].type == ModContent.ItemType<BotanistBody>() 
		&& player.armor[2].type == ModContent.ItemType<BotanistLegs>();

	public override void SetStaticDefaults() => ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = true;
	public override void SetDefaults()
	{
		Item.width = 28;
		Item.height = 24;
		Item.value = Item.sellPrice(0, 0, 10, 0);
		Item.rare = ItemRarityID.White;
		Item.defense = 2;
	}

	public override void UpdateEquip(Player player) => player.AddBuff(BuffID.Sunflower, 2);
	public override bool IsArmorSet(Item head, Item body, Item legs) => body.type == ModContent.ItemType<BotanistBody>() && legs.type == ModContent.ItemType<BotanistLegs>();
	public override void UpdateArmorSet(Player player) => player.setBonus = Language.GetTextValue("Mods.SpiritReforged.SetBonuses.Botanist");

	public override void AddRecipes()
	{
		CreateRecipe().AddIngredient(ItemID.Sunflower, 1).AddIngredient(ItemID.Hay, 10).AddTile(TileID.Loom).Register();
		Recipe.Create(ModContent.ItemType<BotanistBody>()).AddIngredient(ItemID.Silk, 10).AddTile(TileID.Loom).Register();
		Recipe.Create(ModContent.ItemType<BotanistLegs>()).AddIngredient(ItemID.Silk, 6).AddTile(TileID.Loom).Register();
	}
}
