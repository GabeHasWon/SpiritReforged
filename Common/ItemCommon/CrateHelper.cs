using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Common.ItemCommon;

/// <summary>
/// Creates shortcuts to replicate vanilla crate loot database functionality.<br/>
/// Both <see cref="BiomeCrate(ItemLoot, IItemDropRule, IItemDropRule[])"/> and <see cref="HardmodeBiomeCrate(ItemLoot, IItemDropRule, IItemDropRule[])"/> 
/// take in the "main" item drop and all sub drops and automatically put them in how vanilla crates work.
/// </summary>
internal static class CrateHelper
{
	/// <summary>
	/// Populates an item's loot with a "primary" drop and standard drops for a pre-hardmode crate. Use <see cref="HardmodeBiomeCrate(ItemLoot, IItemDropRule, IItemDropRule[])"/>
	/// for hardmode crates.
	/// </summary>
	public static void BiomeCrate(ItemLoot loot, IItemDropRule mainDrop, params IItemDropRule[] subDrops)
	{
		var ores = new IItemDropRule[6]
		{
			ItemDropRule.NotScalingWithLuck(ItemID.CopperOre, 1, 12, 21),
			ItemDropRule.NotScalingWithLuck(ItemID.TinOre, 1, 12, 21),
			ItemDropRule.NotScalingWithLuck(ItemID.IronOre, 1, 12, 21),
			ItemDropRule.NotScalingWithLuck(ItemID.LeadOre, 1, 12, 21),
			ItemDropRule.NotScalingWithLuck(ItemID.SilverOre, 1, 12, 21),
			ItemDropRule.NotScalingWithLuck(ItemID.TungstenOre, 1, 12, 21)
		};

		var bars = new IItemDropRule[6]
		{
			ItemDropRule.NotScalingWithLuck(ItemID.CopperBar, 1, 4, 7),
			ItemDropRule.NotScalingWithLuck(ItemID.TinBar, 1, 4, 7),
			ItemDropRule.NotScalingWithLuck(ItemID.IronBar, 1, 4, 7),
			ItemDropRule.NotScalingWithLuck(ItemID.LeadBar, 1, 4, 7),
			ItemDropRule.NotScalingWithLuck(ItemID.SilverBar, 1, 4, 7),
			ItemDropRule.NotScalingWithLuck(ItemID.TungstenBar, 1, 4, 7)
		};
		
		var potions = new IItemDropRule[8]
		{
			ItemDropRule.NotScalingWithLuck(ItemID.ObsidianSkinPotion, 1, 2, 4),
			ItemDropRule.NotScalingWithLuck(ItemID.SpelunkerPotion, 1, 2, 4),
			ItemDropRule.NotScalingWithLuck(ItemID.HunterPotion, 1, 2, 4),
			ItemDropRule.NotScalingWithLuck(ItemID.GravitationPotion, 1, 2, 4),
			ItemDropRule.NotScalingWithLuck(ItemID.MiningPotion, 1, 2, 4),
			ItemDropRule.NotScalingWithLuck(ItemID.HeartreachPotion, 1, 2, 4),
			ItemDropRule.NotScalingWithLuck(ItemID.CalmingPotion, 1, 2, 4),
			ItemDropRule.NotScalingWithLuck(ItemID.FlipperPotion, 1, 2, 4)
		};
		
		var extraPotions = new IItemDropRule[2]
		{
			ItemDropRule.NotScalingWithLuck(188, 1, 5, 15),
			ItemDropRule.NotScalingWithLuck(189, 1, 5, 15)
		};
		
		var extraBait = new IItemDropRule[2]
		{
			ItemDropRule.NotScalingWithLuck(ItemID.MasterBait, 3, 2, 4),
			ItemDropRule.NotScalingWithLuck(ItemID.JourneymanBait, 1, 2, 4)
		};

		IItemDropRule coinRule = ItemDropRule.NotScalingWithLuck(73, 4, 5, 12);

		List<IItemDropRule> final =
		[
			coinRule,
			new OneFromRulesRule(4, extraPotions),
			new OneFromRulesRule(6, extraBait),
			new OneFromRulesRule(7, ores),
			new OneFromRulesRule(4, bars),
			new OneFromRulesRule(3, potions),
			.. subDrops
		];

		if (mainDrop is not null)
			final.Add(ItemDropRule.SequentialRulesNotScalingWithLuck(1, mainDrop));

		loot.Add(ItemDropRule.AlwaysAtleastOneSuccess([.. final]));
	}

	/// <summary>
	/// Populates an item's loot with a "primary" drop and standard drops for a hardmode crate. Use <see cref="BiomeCrate(ItemLoot, IItemDropRule, IItemDropRule[])"/>
	/// for pre-hardmode crates.
	/// </summary>
	public static void HardmodeBiomeCrate(ItemLoot loot, IItemDropRule mainDrop = null, params IItemDropRule[] subDrops)
	{
		var ores = new IItemDropRule[6]
		{
			ItemDropRule.NotScalingWithLuck(ItemID.CopperOre, 1, 12, 21),
			ItemDropRule.NotScalingWithLuck(ItemID.TinOre, 1, 12, 21),
			ItemDropRule.NotScalingWithLuck(ItemID.IronOre, 1, 12, 21),
			ItemDropRule.NotScalingWithLuck(ItemID.LeadOre, 1, 12, 21),
			ItemDropRule.NotScalingWithLuck(ItemID.SilverOre, 1, 12, 21),
			ItemDropRule.NotScalingWithLuck(ItemID.TungstenOre, 1, 12, 21)
		};

		var hardmodeOres = new IItemDropRule[4]
		{
			ItemDropRule.NotScalingWithLuck(ItemID.CobaltOre, 1, 12, 21),
			ItemDropRule.NotScalingWithLuck(ItemID.PalladiumOre, 1, 12, 21),
			ItemDropRule.NotScalingWithLuck(ItemID.MythrilOre, 1, 12, 21),
			ItemDropRule.NotScalingWithLuck(ItemID.OrichalcumOre, 1, 12, 21)
		};

		var bars = new IItemDropRule[6]
		{
			ItemDropRule.NotScalingWithLuck(ItemID.CopperBar, 1, 4, 7),
			ItemDropRule.NotScalingWithLuck(ItemID.TinBar, 1, 4, 7),
			ItemDropRule.NotScalingWithLuck(ItemID.IronBar, 1, 4, 7),
			ItemDropRule.NotScalingWithLuck(ItemID.LeadBar, 1, 4, 7),
			ItemDropRule.NotScalingWithLuck(ItemID.SilverBar, 1, 4, 7),
			ItemDropRule.NotScalingWithLuck(ItemID.TungstenBar, 1, 4, 7)
		};

		var hardmodeBars = new IItemDropRule[4]
		{
			ItemDropRule.NotScalingWithLuck(ItemID.CobaltBar, 1, 3, 7),
			ItemDropRule.NotScalingWithLuck(ItemID.PalladiumBar, 1, 3, 7),
			ItemDropRule.NotScalingWithLuck(ItemID.MythrilBar, 1, 3, 7),
			ItemDropRule.NotScalingWithLuck(ItemID.OrichalcumBar, 1, 3, 7)
		};

		var potions = new IItemDropRule[8]
		{
			ItemDropRule.NotScalingWithLuck(ItemID.ObsidianSkinPotion, 1, 2, 4),
			ItemDropRule.NotScalingWithLuck(ItemID.SpelunkerPotion, 1, 2, 4),
			ItemDropRule.NotScalingWithLuck(ItemID.HunterPotion, 1, 2, 4),
			ItemDropRule.NotScalingWithLuck(ItemID.GravitationPotion, 1, 2, 4),
			ItemDropRule.NotScalingWithLuck(ItemID.MiningPotion, 1, 2, 4),
			ItemDropRule.NotScalingWithLuck(ItemID.HeartreachPotion, 1, 2, 4),
			ItemDropRule.NotScalingWithLuck(ItemID.CalmingPotion, 1, 2, 4),
			ItemDropRule.NotScalingWithLuck(ItemID.FlipperPotion, 1, 2, 4)
		};

		IItemDropRule coinRule = ItemDropRule.NotScalingWithLuck(73, 4, 5, 12);
		IItemDropRule hardmodeOresRule = ItemDropRule.SequentialRulesNotScalingWithLuck(7, new OneFromRulesRule(2, hardmodeOres), new OneFromRulesRule(1, ores));
		IItemDropRule hardmodeBarsRule = ItemDropRule.SequentialRulesNotScalingWithLuck(4, new OneFromRulesRule(3, 2, hardmodeBars), new OneFromRulesRule(1, bars));

		List<IItemDropRule> final =
		[
			coinRule,
			hardmodeOresRule,
			hardmodeBarsRule,
			new OneFromRulesRule(3, potions),
			.. subDrops,
		];

		if (mainDrop is not null)
			final.Add(ItemDropRule.SequentialRulesNotScalingWithLuck(1, mainDrop));

		loot.Add(ItemDropRule.AlwaysAtleastOneSuccess([.. final]));
	}
}