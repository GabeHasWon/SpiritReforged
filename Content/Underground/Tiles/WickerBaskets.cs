using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.Loot;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.Jungle.Bamboo.Tiles;
using SpiritReforged.Content.Savanna.Items.Food;
using SpiritReforged.Content.Underground.Pottery;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.WorldBuilding;

namespace SpiritReforged.Content.Underground.Tiles;

public class WickerBaskets : PotTile, ILootTile
{
	/// <summary> The area that <see cref="WickerBaskets"/> start generating within. </summary>
	public static Rectangle PicnicArea
	{
		get
		{
			int picnicY = (int)GenVars.worldSurfaceHigh;
			return new Rectangle(20, picnicY, Main.maxTilesX - 40, (int)Math.Max(Main.worldSurface - picnicY, 10));
		}
	}

	public override Dictionary<string, int[]> TileStyles => new() { { string.Empty, [0, 1, 2] } };
	public override void AddRecord(int type, StyleDatabase.StyleGroup group)
	{
		var desc = Language.GetText(TileRecord.DescKey + ".WickerBasket");
		RecordHandler.Records.Add(new TileRecord(group.name, type, group.styles).AddDescription(desc).AddRating(4));
	}

	public override void AddItemRecipes(ModItem modItem, StyleDatabase.StyleGroup group, Condition condition) => modItem.CreateRecipe()
		.AddRecipeGroup("ClayAndMud", 3)
		.AddIngredient(AutoContent.ItemType<StrippedBamboo>(), 3)
		.AddTile(ModContent.TileType<PotteryWheel>())
		.AddCondition(condition)
		.Register();

	public override void AddObjectData()
	{
		DustType = IsRubble ? -1 : DustID.PalmWood;
		Main.tileOreFinderPriority[Type] = 575;

		base.AddObjectData();
	}

	public override void AddMapData() => AddMapEntry(new Color(190, 146, 95), Language.GetText("Mods.SpiritReforged.Items.WickerBasketsItem.DisplayName"));

	public override void DeathEffects(int i, int j, int frameX, int frameY)
	{
		var source = new EntitySource_TileBreak(i, j);
		int variant = frameX / 36;

		for (int g = 0; g < 3; g++)
		{
			int goreType = Mod.Find<ModGore>("Picnic" + (g + variant * 3 + 1)).Type;
			Gore.NewGore(source, new Vector2(i, j) * 16, Vector2.Zero, goreType);
		}

		for (int d = 0; d < 10; d++)
		{
			var dust = Dust.NewDustDirect(new Vector2(i, j) * 16, 32, 32, DustID.TreasureSparkle);
			dust.velocity = -Vector2.UnitY;
			dust.fadeIn = 1.3f;
		}
	}

	public void AddLoot(ILootTile.Context context, ILoot loot)
	{
		if (CrossMod.Thorium.Enabled && GetThoriumTypes() is int[] types && types.Length > 0)
			loot.AddOneFromOptions(2, types);

		if (CrossMod.Redemption.Enabled && CrossMod.Redemption.TryFind("Soulshake", out ModItem shake))
			loot.AddCommon(shake.Type, 4);

		loot.Add(DropRules.LootPoolDrop.SameStack(1, 1, 3, 1, 1, ItemID.Apple, ItemID.Apricot, ItemID.Grapefruit, 
			ItemID.Lemon, ItemID.Peach, ItemID.Cherry, ItemID.Plum, ItemID.BlackCurrant, ItemID.BloodOrange, ItemID.Mango, 
			ItemID.Pineapple, ItemID.Banana, ItemID.Pomegranate, ItemID.SpicyPepper, ModContent.ItemType<Caryocar>(), ModContent.ItemType<CustardApple>()));

		var rule = ItemDropRule.OneFromOptions(2, ItemID.ApplePie, ItemID.Milkshake, ItemID.GrapeJuice);
		rule.OnFailedRoll(ItemDropRule.OneFromOptions(1, ItemID.AppleJuice, ItemID.BloodyMoscato, ItemID.BananaDaiquiri, ItemID.FruitJuice, ItemID.FruitSalad,
			ItemID.Lemonade, ItemID.PeachSangria, ItemID.PinaColada, ItemID.PrismaticPunch, ItemID.SmoothieofDarkness, ItemID.TropicalSmoothie));

		loot.Add(rule);
	}

	private static readonly string[] ThoriumNames = ["Aril", "Cranberry", "Fig", "Lychee", "Mangosteen", "Persimmon", "Raspberry", "Soursop"];
	private static int[] GetThoriumTypes()
	{
		HashSet<int> types = [];
		for (int i = 0; i < ThoriumNames.Length; i++)
		{
			if (CrossMod.Thorium.TryFind(ThoriumNames[i], out ModItem item))
				types.Add(item.Type);
		}

		return [.. types];
	}
}