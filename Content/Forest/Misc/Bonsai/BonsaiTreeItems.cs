using SpiritReforged.Common.NPCCommon;

namespace SpiritReforged.Content.Forest.Misc.Bonsai;

public class SakuraBonsaiItem : ModItem
{
	public virtual int Style => 0;

	public override void SetStaticDefaults() => NPCShopHelper.AddEntry(new NPCShopHelper.ConditionalEntry((shop) 
		=> shop.NpcType == NPCID.Dryad, new NPCShop.Entry(Type, Condition.MoonPhasesEven)));
	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<BonsaiTrees>(), Style);
		Item.value = Item.buyPrice(silver: 50);
	}
}

public class WillowBonsaiItem : SakuraBonsaiItem
{
	public override int Style => 1;
	public override void SetStaticDefaults() => NPCShopHelper.AddEntry(new NPCShopHelper.ConditionalEntry((shop) 
		=> shop.NpcType == NPCID.Dryad, new NPCShop.Entry(Type, Condition.MoonPhasesOdd)));
}

public class PurityBonsaiItem : SakuraBonsaiItem
{
	public override int Style => 2;
	public override void SetStaticDefaults() => NPCShopHelper.AddEntry(new NPCShopHelper.ConditionalEntry((shop) 
		=> shop.NpcType == NPCID.Dryad, new NPCShop.Entry(Type)));
}

public class RubyBonsaiItem : SakuraBonsaiItem
{
	public override int Style => 3;
	public override void SetStaticDefaults() => NPCShopHelper.AddEntry(new NPCShopHelper.ConditionalEntry((shop) 
		=> shop.NpcType == NPCID.Dryad, new NPCShop.Entry(Type, Condition.InBelowSurface, Condition.PlayerCarriesItem(ItemID.Ruby))));
}

public class DiamondBonsaiItem : SakuraBonsaiItem
{
	public override int Style => 4;
	public override void SetStaticDefaults() => NPCShopHelper.AddEntry(new NPCShopHelper.ConditionalEntry((shop) 
		=> shop.NpcType == NPCID.Dryad, new NPCShop.Entry(Type, Condition.InBelowSurface, Condition.PlayerCarriesItem(ItemID.Diamond))));
}

public class EmeraldBonsaiItem : SakuraBonsaiItem
{
	public override int Style => 5;
	public override void SetStaticDefaults() => NPCShopHelper.AddEntry(new NPCShopHelper.ConditionalEntry((shop) 
		=> shop.NpcType == NPCID.Dryad, new NPCShop.Entry(Type, Condition.InBelowSurface, Condition.PlayerCarriesItem(ItemID.Emerald))));
}

public class SapphireBonsaiItem : SakuraBonsaiItem
{
	public override int Style => 6;
	public override void SetStaticDefaults() => NPCShopHelper.AddEntry(new NPCShopHelper.ConditionalEntry((shop) 
		=> shop.NpcType == NPCID.Dryad, new NPCShop.Entry(Type, Condition.InBelowSurface, Condition.PlayerCarriesItem(ItemID.Sapphire))));
}

public class TopazBonsaiItem : SakuraBonsaiItem
{
	public override int Style => 7;
	public override void SetStaticDefaults() => NPCShopHelper.AddEntry(new NPCShopHelper.ConditionalEntry((shop) 
		=> shop.NpcType == NPCID.Dryad, new NPCShop.Entry(Type, Condition.InBelowSurface, Condition.PlayerCarriesItem(ItemID.Topaz))));
}

public class AmethystBonsaiItem : SakuraBonsaiItem
{
	public override int Style => 8;
	public override void SetStaticDefaults() => NPCShopHelper.AddEntry(new NPCShopHelper.ConditionalEntry((shop) 
		=> shop.NpcType == NPCID.Dryad, new NPCShop.Entry(Type, Condition.InBelowSurface, Condition.PlayerCarriesItem(ItemID.Amethyst))));
}