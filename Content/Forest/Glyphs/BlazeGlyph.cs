namespace SpiritReforged.Content.Forest.Glyphs;

public class BlazeGlyph : GlyphItem
{
	public override void SetDefaults()
	{
		Item.height = Item.width = 28;
		Item.rare = ItemRarityID.Pink;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(new(233, 143, 26));
	}
}