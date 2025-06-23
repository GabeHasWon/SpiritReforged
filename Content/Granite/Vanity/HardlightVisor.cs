using SpiritReforged.Common.Visuals.Glowmasks;

namespace SpiritReforged.Content.Granite.Vanity;

[AutoloadEquip(EquipType.Head)]
[AutoloadGlowmask("100,100,100,100")]
public class HardlightVisor : ModItem
{
	public override void SetStaticDefaults() => ArmorIDs.Head.Sets.DrawFullHair[Item.headSlot] = true;

	public override void SetDefaults()
	{
		Item.width = 30;
		Item.height = 12;
		Item.value = Item.buyPrice(0, 0, 75, 0);
		Item.rare = ItemRarityID.White;
		Item.vanity = true;
	}
}