namespace SpiritReforged.Content.Forest.Glyphs.Rot;

public class RotGlyph : GlyphItem
{
	public override void SetDefaults()
	{
		Item.width = Item.height = 28;
		Item.rare = ItemRarityID.Green;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(new(142, 186, 231));
	}
}