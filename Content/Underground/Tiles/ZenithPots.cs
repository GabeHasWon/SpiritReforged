using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.UI.PotCatalogue;
using System.Linq;

namespace SpiritReforged.Content.Underground.Tiles;

public class ZenithPots : PotTile, ILootable
{
	public override Dictionary<string, int[]> TileStyles => new()
	{
		{ string.Empty, [0, 1, 2] },
		{ "Pale", [3, 4, 5] }
	};

	public override TileRecord AddRecord(int type, NamedStyles.StyleGroup group)
	{
		var record = new TileRecord(group.name, type, group.styles);
		return record.AddRating(2).AddDescription(Language.GetText(TileRecord.DescKey + ".Zenith")).SetCondition(FoundAll).Hide();

		bool FoundAll()
		{
			var global = Main.LocalPlayer.GetModPlayer<RecordPlayer>();

			if (global.IsValidated(group.name))
				return true;

			return RecordHandler.Records.All(static x => x.type == ModContent.TileType<ZenithPots>() || x.Condition.IsMet());
		}
	}

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		DustType = IsRubble ? -1 : DustID.TreasureSparkle;
	}

	public void AddLoot(ILoot loot)
	{
		if (TileLootSystem.TryGetLootPool(ModContent.TileType<Pots>(), out var dele))
			dele.Invoke(loot);
	}
}