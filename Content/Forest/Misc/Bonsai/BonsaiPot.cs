using SpiritReforged.Common.NPCCommon;

namespace SpiritReforged.Content.Forest.Misc.Bonsai;

public class BonsaiPot : ModItem
{
	public override void SetStaticDefaults() => NPCShopHelper.AddEntry(NPCShopHelper.ConditionalEntry.FromNPC(NPCID.Dryad, new NPCShop.Entry(Type)));
	public override void SetDefaults()
	{
		Item.width = Item.height = 14;
		Item.maxStack = Item.CommonMaxStack;
		Item.value = Item.buyPrice(silver: 25);
	}
}