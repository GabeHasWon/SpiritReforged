using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat.Classic;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.Jungle.Bamboo.Tiles;
using System.Linq;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Forest.Cloud.Items;

[FromClassic("CloudstalkItem")]
public class Cloudstalk : ModItem
{
	public override void SetStaticDefaults()
	{
		ItemLootDatabase.ModifyItemRule(ItemID.HerbBag, AddTypesToList);
		Item.ResearchUnlockCount = 25;
	}

	/// <summary> Adds Cloudstalk and Cloudstalk Seeds to the Herb Bag drop pool. </summary>
	private static void AddTypesToList(ref ItemLoot loot)
	{
		foreach (var rule in loot.Get())
		{
			if (rule is HerbBagDropsItemDropRule herbRule)
			{
				var drops = herbRule.dropIds.ToList();
				drops.AddRange([ModContent.ItemType<Cloudstalk>(), ModContent.ItemType<CloudstalkSeed>()]);

				herbRule.dropIds = [.. drops];
				return;
			}
		}
	}

	public override void SetDefaults()
	{
		Item.autoReuse = false;
		Item.useTurn = true;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.useAnimation = Item.useTime = 15;
		Item.maxStack = Item.CommonMaxStack;
		Item.width = 22;
		Item.height = 18;
	}

	public override void AddRecipes()
	{
		Recipe.Create(ItemID.FeatherfallPotion, 1).AddIngredient(ItemID.Blinkroot)
			.AddIngredient(ItemID.Daybloom).AddIngredient(Type).AddIngredient(ItemID.BottledWater).AddTile(TileID.Bottles).Register();

		Recipe.Create(ItemID.GravitationPotion).AddIngredient(ItemID.Blinkroot).AddIngredient(ItemID.Fireblossom)
			.AddIngredient(ItemID.Deathweed).AddIngredient(Type).AddIngredient(ItemID.BottledWater).AddTile(TileID.Bottles).Register();
	}
}

public class CloudstalkTile : HerbTile
{
	private const float BloomWindSpeed = 20; //Constant for bloom wind speed, in mph

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		TileObjectData.newTile.CopyFrom(TileObjectData.StyleAlch);
		TileObjectData.newTile.AnchorValidTiles = [TileID.Grass, TileID.HallowedGrass, TileID.JungleGrass, ModContent.TileType<Savanna.Tiles.SavannaGrass>(), 
			ModContent.TileType<Savanna.Tiles.SavannaGrassHallow>(), TileID.Cloud, TileID.RainCloud, TileID.SnowCloud];
		TileObjectData.newTile.AnchorAlternateTiles = [TileID.ClayPot, TileID.PlanterBox, ModContent.TileType<Tiles.CloudstalkBox>(), ModContent.TileType<BambooPot>()];
		TileObjectData.addTile(Type);

		LocalizedText name = CreateMapEntryName();
		AddMapEntry(new Color(178, 234, 234), name);

		DustType = DustID.Cloud;
		HerbType = ModContent.ItemType<Cloudstalk>();
		SeedType = ModContent.ItemType<CloudstalkSeed>();
	}

	public override void NearbyEffects(int i, int j, bool closer)
	{
		float windSpeed = Math.Abs(Main.windSpeedCurrent) * 50;

		if (windSpeed > BloomWindSpeed && GetStage(i, j) == PlantStage.Growing)
			SetStage(i, j, PlantStage.Grown);
		else if (windSpeed <= BloomWindSpeed && GetStage(i, j) == PlantStage.Grown)
			SetStage(i, j, PlantStage.Growing);
	}
}