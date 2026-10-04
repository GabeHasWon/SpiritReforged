namespace SpiritReforged.Content.Crossmod.Spooky.Items;

internal class MummyBandageScrap : ModItem
{
	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.Star);
		Item.rare = ItemRarityID.Quest;
	}
}
