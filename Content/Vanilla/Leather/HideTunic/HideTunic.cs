namespace SpiritReforged.Content.Vanilla.Leather.HideTunic;

[AutoloadEquip(EquipType.Body)]
public class HideTunic : ModItem
{
	public override void Load()
	{
		if (Main.netMode == NetmodeID.Server)
			return;

		EquipLoader.AddEquipTexture(Mod, $"{Texture}_{EquipType.Legs}", EquipType.Legs, this, "HideTunic_Legs");
	}

	public override void SetDefaults()
	{
		Item.width = 30;
		Item.height = 20;
		Item.value = Item.sellPrice(0, 0, 0, 20);
		Item.rare = ItemRarityID.Blue;
		Item.defense = 4;
	}

	public override void SetMatch(bool male, ref int equipSlot, ref bool robes)
	{
		robes = true;
		equipSlot = EquipLoader.GetEquipSlot(Mod, "HideTunic_Legs", EquipType.Legs);
	}

	public override void UpdateEquip(Player player) => player.GetModPlayer<HideTunicPlayer>().active = true;

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Leather, 11).AddTile(TileID.Loom).Register();
}
