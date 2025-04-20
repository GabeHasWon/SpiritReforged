using RubbleAutoloader;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.Underground.Pottery;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underground.Tiles;

public class UpsideDownPot : PotTile
{
	public override Dictionary<string, int[]> TileStyles => new() { { string.Empty, [0] } };

	public override void AddRecord(int type, StyleDatabase.StyleGroup group)
	{
		var record = new TileRecord(group.name, type, group.styles);
		RecordHandler.Records.Add(record.AddRating(5).AddDescription(Language.GetText(TileRecord.DescKey + ".UpsideDown")));
	}

	public override void AddObjectData()
	{
		const int row = 1;

		Main.tileOreFinderPriority[Type] = 575;
		Main.tileCut[Type] = false;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = new(0, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.StyleWrapLimit = row;
		TileObjectData.newTile.RandomStyleRange = row;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.UsesCustomCanPlace = true;
		TileObjectData.newTile.DrawXOffset = 1;
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		DustType = -1;
	}

	public override void AddMapData() => AddMapEntry(new Color(146, 76, 77), Language.GetText("Mods.SpiritReforged.Items.UpsideDownPotItem.DisplayName"));

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		if (WorldGen.generatingWorld || Autoloader.IsRubble(Type))
			return;

		var source = new EntitySource_TileBreak(i, j);
		if (Main.netMode != NetmodeID.MultiplayerClient)
		{
			Item.NewItem(source, new Rectangle(i * 16, j * 16, 32, 32), ModContent.ItemType<PotHead>());
		}
	}
}

[AutoloadEquip(EquipType.Head)]
public class PotHead : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 24;
		Item.height = 24;
		Item.value = Item.sellPrice(gold: 3);
		Item.rare = ItemRarityID.Blue;
		Item.vanity = true;
	}
}