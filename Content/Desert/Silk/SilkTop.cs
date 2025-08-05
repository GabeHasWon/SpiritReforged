using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Content.Desert.Silk;

[AutoloadEquip(EquipType.Body)]
public class SilkTop : ModItem
{
	public static int AltEquipSlot { get; private set; }
	public override void Load()
	{
		if (!Main.dedServ)
		{
			const string name = "SilkTopAlt_Body";
			AltEquipSlot = EquipLoader.AddEquipTexture(Mod, DrawHelpers.RequestLocal(GetType(), name), EquipType.Body, null, name);
		}
	}

	public override void SetDefaults()
	{
		Item.width = 32;
		Item.height = 30;
		Item.value = 12500;
		Item.rare = ItemRarityID.Blue;
		Item.defense = 4;
	}

	public override void UpdateEquip(Player player) => player.GetDamage(DamageClass.Magic) += 0.07f;
	public override void EquipFrameEffects(Player player, EquipType type)
	{
		if (!player.Male && player.body == Item.bodySlot)
			player.body = AltEquipSlot;
	}

	public override void AddRecipes() => CreateRecipe()
		.AddIngredient(ItemID.Silk, 10)
		.AddRecipeGroup("GoldBars", 3)
		.AddTile(TileID.Anvils)
		.Register();
}