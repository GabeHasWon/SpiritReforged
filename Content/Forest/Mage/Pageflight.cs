using SpiritReforged.Common;

namespace SpiritReforged.Content.Forest.Mage;

public class Pageflight : ModItem
{
	public override void SetStaticDefaults() => SpiritSets.MagicBook[Type] = true;

	public override void SetDefaults()
	{
		Item.width = Item.height = 24;
		Item.damage = 9;
		Item.knockBack = 2;
		Item.DamageType = DamageClass.Magic;
		Item.noMelee = true;
		Item.noUseGraphic = true;
		Item.autoReuse = true;
		Item.channel = true;
		Item.useTime = Item.useAnimation = 20;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.value = Item.sellPrice(0, 0, 50, 0);
		Item.rare = ItemRarityID.Blue;
		Item.UseSound = SoundID.Item20;
		Item.mana = 4;
		Item.shootSpeed = 8;
		Item.shoot = ProjectileID.PaperAirplaneA;
	}
}