namespace SpiritReforged.Content.Crossmod.Spooky;

internal class MummyQuestItem : ModItem
{
	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.Star);
		Item.rare = ItemRarityID.Quest;
	}
}
