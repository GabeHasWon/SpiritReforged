using SpiritReforged.Common.ItemCommon;

namespace SpiritReforged.Content.Ocean.Items.Reefhunter;

public class IridescentScale : ModItem
{
	public override bool IsLoadingEnabled(Mod mod) => false;

	public override void SetStaticDefaults() => VariantGlobalItem.AddVariants(Type, [new Point(26, 20), new Point(26, 32), new Point(26, 28)]);
	public override void SetDefaults()
	{
		Item.value = 100;
		Item.maxStack = Item.CommonMaxStack;
		Item.rare = ItemRarityID.Blue;
		Item.width = 26;
		Item.height = 28;
	}
}
