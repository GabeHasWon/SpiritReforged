using RubbleAutoloader;
using SpiritReforged.Common.ItemCommon;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Ziggurat.Tiles;

public class ZigguratPiles2x2 : ModTile, IAutoloadRubble
{
	public IAutoloadRubble.RubbleData Data => new(ModContent.GetInstance<RedSandstoneBrick>().AutoItemType(), IAutoloadRubble.RubbleSize.Medium);

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;
		Main.tileNoFail[Type] = true;

		TileID.Sets.ReplaceTileBreakUp[Type] = true;
		TileID.Sets.DoesntGetReplacedWithTileReplacement[Type] = false;
		TileID.Sets.BreakableWhenPlacing[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = new Point16(0, 1);
		TileObjectData.newTile.CoordinateHeights = [16, 18];
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(191, 138, 67));
		DustType = DustID.Gold;
	}

	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem) => fail = false;

	public override IEnumerable<Item> GetItemDrops(int i, int j)
	{
		if (Autoloader.IsRubble(Main.tile[i, j].TileType))
			yield break;

		if (Main.rand.NextBool(4))
			yield return new Item(ItemID.Diamond, Main.rand.Next(1, 3));
		else if (Main.rand.NextBool(2))
			yield return new Item(ItemID.Sapphire, Main.rand.Next(1, 3));
		else
			yield return new Item(ItemID.Ruby, Main.rand.Next(1, 3));

		yield return new Item(ItemID.GoldCoin, Main.rand.Next(2, 5));
	}
}