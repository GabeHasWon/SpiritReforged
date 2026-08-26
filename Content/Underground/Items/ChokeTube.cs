using SpiritReforged.Common.Subclasses.Shotguns;

namespace SpiritReforged.Content.Underground.Items;

public class ChokeTube : ModItem
{
	public override string Texture => AssetLoader.EmptyTexture; // TODO: actual texture
	public override void SetDefaults()
	{
		Item.DefaultToAccessory();

		Item.rare = ItemRarityID.Blue;
		Item.value = Item.sellPrice(silver: 50);
	}

	public override void UpdateEquip(Player player) => player.GetModPlayer<ShotgunPlayer>().shotgunStats._spreadMultiplier -= 0.5f;
}
