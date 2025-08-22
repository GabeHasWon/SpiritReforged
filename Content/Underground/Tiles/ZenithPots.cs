using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.Underground.Pottery;

namespace SpiritReforged.Content.Underground.Tiles;

public class ZenithPots : PotTile, ILootable
{
	public override Dictionary<string, int[]> TileStyles => new()
	{
		{ string.Empty, [0, 1, 2] },
		{ "Pale", [3, 4, 5] }
	};

	public override void AddRecord(int type, StyleDatabase.StyleGroup group)
	{
		var record = new TileRecord(group.name, type, group.styles);
		RecordHandler.Records.Add(record.AddRating(2).AddDescription(Language.GetText(TileRecord.DescKey + ".Zenith")).Hide());
	}

	public override void AddItemRecipes(ModItem modItem, StyleDatabase.StyleGroup group, Condition condition)
	{
		modItem.CreateRecipe().AddRecipeGroup("ClayAndMud", 3).AddTile(ModContent.TileType<PotteryWheel>()).AddCondition(condition.Description, RecordedOrProgressed).Register();
		bool RecordedOrProgressed() => Main.LocalPlayer.GetModPlayer<RecordPlayer>().IsValidated(group.name) || NPC.downedMoonlord;
	}

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		DustType = IsRubble ? -1 : DustID.TreasureSparkle;
	}

	public void AddLoot(ILoot loot)
	{
		if (TileLootHandler.TryGetLootPool(ModContent.TileType<Pots>(), out var dele))
			dele.Invoke(loot);
	}
}