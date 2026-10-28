using SpiritReforged.Common.ItemCommon.MagazineSystem;

namespace SpiritReforged.Content.Forest.Misc;

public class LeatherSling : ModItem
{
	// TODO: Texture
	public override string Texture => AssetLoader.EmptyTexture;

	public override void SetDefaults()
	{
		Item.DefaultToAccessory();
		Item.rare = ItemRarityID.Green;
	}

	public override void UpdateEquip(Player player)
	{
		var modPlayer = player.GetModPlayer<MagazinePlayer>();

		modPlayer.inventoryReload = true;
		modPlayer.inventoryReloadTimerMax = 3;
	}
}

class LeatherSlingGlobalItem : GlobalItem
{
	public override void PostDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
	{
		var magazinePlayer = Main.LocalPlayer.GetModPlayer<MagazinePlayer>();

		if (item.TryGetGlobalItem<MagazineGlobalItem>(out var magazineItem) && magazineItem.Active)
		{

			// TODO: drawing logic	
		}
	}
}

