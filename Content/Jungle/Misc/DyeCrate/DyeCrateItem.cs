using SpiritReforged.Common.ItemCommon;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Jungle.Misc.DyeCrate;

public class DyeCrateItem : ModItem
{
	public override void SetStaticDefaults()
	{
		Item.ResearchUnlockCount = 10;

		ItemLootDatabase.AddItemRule(ItemID.JungleFishingCrate, ItemDropRule.Common(Type, 3));
		ItemLootDatabase.AddItemRule(ItemID.JungleFishingCrateHard, ItemDropRule.Common(Type, 2));
	}

	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.HerbBag);
		Item.Size = new Vector2(32, 36);
	}

	public override bool CanRightClick() => true;

	public override void ModifyItemLoot(ItemLoot itemLoot)
	{
		itemLoot.Add(new DropRules.LootPoolDrop(4, 1, 1, 
			[
			new(ItemID.BlueBerries, 2..5),
			new(ItemID.CyanHusk, 1..3),
			new(ItemID.RedHusk, 1..3),
			new(ItemID.VioletHusk, 1..3),
			new(ItemID.GreenMushroom, 1..3),
			new(ItemID.LimeKelp, 1..3),
			new(ItemID.OrangeBloodroot, 1..3),
			new(ItemID.PinkPricklyPear, 1..3),
			new(ItemID.PurpleMucos, 1..3),
			new(ItemID.SkyBlueFlower, 1..3),
			new(ItemID.TealMushroom, 1 ..3),
			new(ItemID.YellowMarigold, 1 ..3),
			new(ItemID.BlackInk, 1 ..3)
			]
		));

		itemLoot.Add(new DropRules.LootPoolDrop(1, 10, 1,
			[
			new(ItemID.StrangePlant1, 1..3),
			new(ItemID.StrangePlant2, 1..3),
			new(ItemID.StrangePlant3, 1..3),
			new(ItemID.StrangePlant4, 1..3)
			]
		));
	}
}