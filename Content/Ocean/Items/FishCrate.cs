using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ItemCommon.FloatingItem;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Ocean.Items;

public class FishCrate : FloatingItem
{
	public override float SpawnWeight => 0.4f;
	public override float Weight => base.Weight * 0.9f;
	public override float Bouyancy => base.Bouyancy * 1.05f;

	public override void SetStaticDefaults() => Item.ResearchUnlockCount = 10;

	public override void SetDefaults()
	{
		Item.width = 20;
		Item.height = 20;
		Item.rare = ItemRarityID.Orange;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.createTile = ModContent.TileType<FishCrateTile>();
		Item.maxStack = Item.CommonMaxStack;
		Item.autoReuse = true;
		Item.useAnimation = 15;
		Item.useTime = 10;
		Item.consumable = true;
	}

	public override bool CanRightClick() => true;

	public override void ModifyItemLoot(ItemLoot itemLoot)
	{
		itemLoot.AddCommon<Vanilla.Items.Food.RawFish>(2);
		itemLoot.Add(DropRules.LootPoolDrop.SameStack(3, 4, 1, 1, 1, ItemID.Shrimp, ItemID.Salmon, ItemID.Bass, ItemID.RedSnapper, ItemID.Trout));
		itemLoot.Add(DropRules.LootPoolDrop.SameStack(1, 2, 1, 4, 1, ItemID.Damselfish, ItemID.DoubleCod, ItemID.ArmoredCavefish, ItemID.FrostMinnow));
		itemLoot.AddOneFromOptions(27, ItemID.ReaverShark, ItemID.Swordfish, ItemID.SawtoothShark);
		itemLoot.AddOneFromOptions<Vanity.DiverSet.DiverLegs, Vanity.DiverSet.DiverBody, Vanity.DiverSet.DiverHead>(14);
		itemLoot.Add(DropRules.LootPoolDrop.SameStack(9, 12, 1, 3, 1, ItemID.FrostDaggerfish, ItemID.BombFish));

		LeadingConditionRule isHardmode = new LeadingConditionRule(new Conditions.IsHardmode());
		isHardmode.OnSuccess(DropRules.LootPoolDrop.SameStack(1, 3, 1, 10, 1, ItemID.FlarefinKoi, ItemID.Obsidifish, ItemID.Prismite, ItemID.PrincessFish));
		itemLoot.Add(isHardmode);

		itemLoot.AddCommon(ItemID.SilverCoin, 3, 40, 91);
		itemLoot.AddCommon(ItemID.GoldCoin, 7, 2, 5);
	}
}

public class FishCrateTile : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileTable[Type] = true;
		Main.tileSolidTop[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;
		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.CoordinateHeights = [16, 18];
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(150, 150, 150));
	}
}
