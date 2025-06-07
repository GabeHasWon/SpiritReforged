using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.Underground.Pottery;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Underground.Tiles;

/// <summary> A stand-in for vanilla pot tiles, used to contain custom data. </summary>
public class Pots : PotTile, ILootTile
{
	public const string PotTexture = "Terraria/Images/Tiles_28";

	public override string Texture => PotTexture;
	public override Dictionary<string, int[]> TileStyles
	{
		get
		{
			string[] names = ["Cavern", "Ice", "Jungle", "Dungeon", "Hell", "Corruption", "Spider", "Crimson", "Pyramid", "Temple", "Marble", "Desert"];
			Dictionary<string, int[]> groups = [];

			groups.Add(names[0], [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11]);
			const int length = 9;

			for (int i = 1; i < names.Length; i++)
			{
				int skip = i * length + 3;
				int[] styles = [skip, skip + 1, skip + 2, skip + 3, skip + 4, skip + 5, skip + 6, skip + 7, skip + 8];

				groups.Add(names[i], styles);
			}

			return groups;
		}
	}

	public override void AddRecord(int type, StyleDatabase.StyleGroup group)
	{
		if (group.name == "PotsCrimson")
		{
			RecordHandler.Records.Add(new TileRecord(group.name, type, group.styles).Hide(() => !WorldGen.crimson)); //Conditionally hide some entries
		}
		else if (group.name == "PotsCorruption")
		{
			RecordHandler.Records.Add(new TileRecord(group.name, type, group.styles).Hide(() => WorldGen.crimson));
		}
		else
		{
			base.AddRecord(type, group);
		}
	}

	public override void AddItemRecipes(ModItem modItem, StyleDatabase.StyleGroup group)
	{
		int wheel = ModContent.TileType<PotteryWheel>();
		LocalizedText dicovered = AutoloadedPotItem.Discovered;
		var function = (modItem as AutoloadedPotItem).RecordedPot;

		switch (group.name)
		{
			case "PotsCavern":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddTile(wheel).AddCondition(dicovered, function).Register();
				break;

			case "PotsIce":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.IceBlock, 3).AddTile(wheel).AddCondition(dicovered, function).Register();
				break;

			case "PotsJungle":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.RichMahogany, 3).AddTile(wheel).AddCondition(dicovered, function).Register();
				break;

			case "PotsDungeon":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.Bone, 3).AddTile(wheel).AddCondition(dicovered, function).Register();
				break;

			case "PotsHell":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.Obsidian, 2).AddTile(wheel).AddCondition(dicovered, function).Register();
				break;

			case "PotsCorruption":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.RottenChunk).AddTile(wheel).AddCondition(dicovered, function).Register();
				break;

			case "PotsSpider":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.Cobweb, 3).AddTile(wheel).AddCondition(dicovered, function).Register();
				break;

			case "PotsCrimson":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.Vertebrae).AddTile(wheel).AddCondition(dicovered, function).Register();
				break;

			case "PotsPyramid":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddTile(wheel).AddCondition(dicovered, function).Register();
				break;

			case "PotsTemple":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.LihzahrdBrick).AddTile(wheel).AddCondition(dicovered, function).Register();
				break;

			case "PotsMarble":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.Marble, 3).AddTile(wheel).AddCondition(dicovered, function).Register();
				break;

			case "PotsDesert":
				modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.Sandstone, 2).AddTile(wheel).AddCondition(dicovered, function).Register();
				break;
		}
	}

	public override void AddObjectData()
	{
		const int row = 3;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = new(0, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.StyleWrapLimit = row;
		TileObjectData.newTile.RandomStyleRange = 9;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.UsesCustomCanPlace = true;
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);
	}

	public void AddLoot(int objectStyle, ILoot loot)
	{
		string styleName = StyleDatabase.GetName(Type, (byte)objectStyle);

		List<IItemDropRule> branch = []; //Full branch to select ONE option from

		if (styleName == "PotsDungeon")
			branch.Add(ItemDropRule.Common(ItemID.GoldenKey, 35));

		List<int> potions = [ItemID.IronskinPotion, ItemID.ShinePotion, ItemID.NightOwlPotion, ItemID.SwiftnessPotion,
			ItemID.MiningPotion, ItemID.CalmingPotion, ItemID.BuilderPotion, ItemID.RecallPotion, ItemID.ArcheryPotion,
			ItemID.GillsPotion, ItemID.HunterPotion, ItemID.TrapsightPotion, ItemID.FeatherfallPotion, ItemID.WaterWalkingPotion,
			ItemID.GravitationPotion, ItemID.InvisibilityPotion, ItemID.ThornsPotion, ItemID.HeartreachPotion, ItemID.FlipperPotion,
			ItemID.ManaRegenerationPotion, ItemID.ObsidianSkinPotion, ItemID.MagicPowerPotion, ItemID.BattlePotion, ItemID.TitanPotion];

		var pCond0 = ItemDropRule.OneFromOptions(13, [.. potions]);

		if (styleName == "PotsHell")
			pCond0.OnSuccess(ItemDropRule.Common(ItemID.PotionOfReturn, 5));

		branch.Add(pCond0);
		branch.Add(ItemDropRule.ByCondition(new DropConditions.Standard(Condition.Multiplayer), ItemID.WormholePotion, 30));

		var subCondition = new DropConditions.Dynamic(() => false, "Mods.SpiritReforged.Conditions.Submerged");
		branch.Add((styleName == "PotsIce") ? ItemDropRule.Common(ItemID.IceTorch, 1, 3, 12) : ItemDropRule.Common(TorchType(), 1, 3, 12));
		branch.Add((styleName == "PotsIce") ? ItemDropRule.ByCondition(subCondition, ItemID.StickyGlowstick, 1, 3, 12) : ItemDropRule.ByCondition(subCondition, ItemID.Glowstick, 1, 3, 12));

		if (styleName == "PotsHell")
			branch.Add(ItemDropRule.Common(ItemID.HellfireArrow, 1, 10, 20));
		else if (Main.hardMode)
			branch.Add(ItemDropRule.OneFromOptions(1, ItemID.UnholyArrow, ItemID.Grenade, (WorldGen.SavedOreTiers.Silver == TileID.Silver) ? ItemID.SilverBullet : ItemID.TungstenBullet));
		else
			branch.Add(DropRules.LootPoolDrop.SameStack(10, 20, 1, 1, 1, ItemID.WoodenArrow, ItemID.Shuriken));

		branch.Add(ItemDropRule.Common(Main.hardMode ? ItemID.HealingPotion : ItemID.LesserHealingPotion));
		branch.Add(ItemDropRule.Common((styleName == "PotsDesert") ? ItemID.ScarabBomb : ItemID.Bomb, 1, 1, 4));

		if (!Main.hardMode)
			branch.Add(ItemDropRule.Common(ItemID.Rope, 1, 20, 40));

		loot.Add(new OneFromRulesRule(1, [.. branch]));

		int TorchType()
		{
			int result = styleName switch
			{
				"PotCorruption" => ItemID.CorruptTorch,
				"PotCrimson" => ItemID.CrimsonTorch,
				"PotJungle" => ItemID.JungleTorch,
				"PotDesert" => ItemID.DesertTorch,
				_ => ItemID.Torch
			};

			return result;
		}
	}
}