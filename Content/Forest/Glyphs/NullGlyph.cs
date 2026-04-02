using Terraria.Audio;

namespace SpiritReforged.Content.Forest.Glyphs;

public sealed class NullGlyph : GlyphItem
{
	public override bool CanApplyGlyph(Item item) => item.TryGetGlobalItem(out GlyphGlobalItem glyphItem) && glyphItem.glyph != default;

	public override void ApplyGlyph(Item item, ApplicationContext context)
	{
		item.GetGlobalItem<GlyphGlobalItem>().glyph = default;

		if (context == ApplicationContext.Apply)
			SoundEngine.PlaySound(EnchantSound);

		item.prefix = 0;
		item.Refresh(false);
		item.ClearNameOverride();
	}

	public override void SetDefaults()
	{
		Item.width = 28;
		Item.height = 28;
		Item.value = Item.buyPrice(0, 5, 0, 0);
		Item.rare = ItemRarityID.Blue;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(new() { PackedValue = 0x9f9593 });
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		if (tooltips.FindIndex(static x => x.Name == "Tooltip0") is int index && index < 0)
			return;

		tooltips.Insert(index, new(Mod, "GlyphHint", (Item.shopCustomPrice.HasValue ? Target : this.GetLocalization("RightClick")).Value)
		{
			OverrideColor = new Color(120, 190, 120)
		});
	}
}