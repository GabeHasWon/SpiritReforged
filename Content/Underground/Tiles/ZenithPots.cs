using RubbleAutoloader;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.Underground.Pottery;
using static SpiritReforged.Common.TileCommon.StyleDatabase;

namespace SpiritReforged.Content.Underground.Tiles;

public class ZenithPots : PotTile, ILootTile
{
	public override Dictionary<string, int[]> TileStyles => new()
	{
		{ string.Empty, [0, 1, 2] },
		{ "Pale", [3, 4, 5] }
	};

	public override void AddRecord(int type, StyleGroup group)
	{
		var record = new TileRecord(group.name, type, group.styles);
		RecordHandler.Records.Add(record.AddRating(2).AddDescription(Language.GetText(TileRecord.DescKey + ".Zenith")).Hide());
	}

	public override void AddItemRecipes(ModItem modItem, StyleGroup group)
	{
		LocalizedText dicovered = AutoloadedPotItem.Discovered;
		var function = RecordedOrProgressed;

		modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddTile(ModContent.TileType<PotteryWheel>()).AddCondition(dicovered, function).Register();
		bool RecordedOrProgressed() => Main.LocalPlayer.GetModPlayer<RecordPlayer>().IsValidated(group.name) || NPC.downedMoonlord;
	}

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		DustType = Autoloader.IsRubble(Type) ? -1 : DustID.TreasureSparkle;
	}

	public void AddLoot(int objectStyle, ILoot loot) => ModContent.GetInstance<Pots>().AddLoot(objectStyle, loot);
}