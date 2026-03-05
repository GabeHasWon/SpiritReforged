using SpiritReforged.Common.ModCompat.Classic;

namespace SpiritReforged.Content.Ocean.Items.Lifesaver;

[FromClassic("Mantaray_Hunting_Harpoon")]
public class Lifesaver : ModItem
{
	public override void SetDefaults()
	{
		Item.Size = new Vector2(30);
		Item.useTime = Item.useAnimation = 20;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.rare = ItemRarityID.Yellow;
		Item.UseSound = SoundID.Item3;
		Item.noMelee = true;
		Item.mountType = ModContent.MountType<MantarayMount>();
		Item.value = Item.sellPrice(gold: 5);
	}
}