namespace SpiritReforged.Content.Forest.Crossbows;

public class Bolt : ModItem
{
	public override void SetDefaults()
    {
		Item.ammo = Type;
		Item.maxStack = Item.CommonMaxStack;
		Item.consumable = true;
		Item.damage = 10;
		Item.knockBack = 1f;
		Item.rare = ItemRarityID.White;
	}
}