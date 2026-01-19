using RubbleAutoloader;
using SpiritReforged.Common.ItemCommon;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Ziggurat.Tiles;

public abstract class ZigguratPile : ModTile, IAutoloadRubble
{
	public abstract IAutoloadRubble.RubbleData Data { get; }

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;
		Main.tileNoFail[Type] = true;

		TileID.Sets.BreakableWhenPlacing[Type] = true;

		AddMapEntry(new Color(191, 138, 67));
		DustType = DustID.Gold;
	}

	public override IEnumerable<Item> GetItemDrops(int i, int j)
	{
		if (Autoloader.IsRubble(Type))
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

public class ZigguratPiles2x1 : ZigguratPile, IAutoloadRubble
{
	public override IAutoloadRubble.RubbleData Data => new(ModContent.GetInstance<RedSandstoneBrick>().AutoItemType(), IAutoloadRubble.RubbleSize.Small);

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x1);
		TileObjectData.newTile.CoordinateHeights = [16, 18];
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.addTile(Type);
	}
}

public class ZigguratPiles2x2 : ZigguratPile
{
	public override IAutoloadRubble.RubbleData Data => new(ModContent.GetInstance<RedSandstoneBrick>().AutoItemType(), IAutoloadRubble.RubbleSize.Medium);

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = new Point16(0, 1);
		TileObjectData.newTile.CoordinateHeights = [16, 18];
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.addTile(Type);
	}
}