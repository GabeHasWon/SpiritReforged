namespace SpiritReforged.Common.Subclasses.Wrenches;

internal class ScrapItem : ModItem
{
	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.Silk);
		Item.Size = new(22);
	}
}
