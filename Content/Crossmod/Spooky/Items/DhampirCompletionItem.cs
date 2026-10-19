namespace SpiritReforged.Content.Crossmod.Spooky.Items;

internal class DhampirCompletionItem : ModItem
{
	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.Star);
		Item.rare = ItemRarityID.Quest;
	}
}
