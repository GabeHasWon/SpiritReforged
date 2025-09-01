namespace SpiritReforged.Content.Desert.Silk;

[AutoloadEquip(EquipType.Head, EquipType.Front)]
public class SunEarrings : ModItem
{
	public override void SetStaticDefaults() => ArmorIDs.Head.Sets.DrawFullHair[Item.headSlot] = true;
	public override void SetDefaults()
	{
		Item.width = 22;
		Item.height = 22;
		Item.value = 7500;
		Item.rare = ItemRarityID.Blue;
		Item.defense = 1;
	}

	public override bool IsArmorSet(Item head, Item body, Item legs) => head.type == Type && body.type == ModContent.ItemType<SilkTop>() && legs.type == ModContent.ItemType<SilkSirwal>();
	public override void UpdateArmorSet(Player player)
	{
		player.GetModPlayer<AfterimagePlayer>().setActive = true;
		player.setBonus = Language.GetTextValue("Mods.SpiritReforged.SetBonuses.Sundancer");
	}

	public override void UpdateEquip(Player player)
	{
		player.statManaMax2 += 20;
		player.GetDamage(DamageClass.Magic) += 0.07f;
	}

	public override void EquipFrameEffects(Player player, EquipType type)
	{
		if (player.head == Item.headSlot && player.front <= 0)
			player.front = Item.frontSlot;
	}

	public override void AddRecipes() => CreateRecipe()
		.AddRecipeGroup("GoldBars", 2)
		.AddTile(TileID.Anvils)
		.Register();
}