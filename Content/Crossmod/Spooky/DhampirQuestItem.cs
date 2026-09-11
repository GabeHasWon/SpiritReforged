namespace SpiritReforged.Content.Crossmod.Spooky;

internal class DhampirQuestItem : ModItem
{
	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.Star);
		Item.rare = ItemRarityID.Quest;
	}
}
