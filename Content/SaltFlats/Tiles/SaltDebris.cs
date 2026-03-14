using RubbleAutoloader;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using Terraria.DataStructures;

namespace SpiritReforged.Content.SaltFlats.Tiles;

public abstract class SaltDebris : ModTile, IAutoloadRubble
{
	public readonly record struct Info(int DustType);

	public abstract IAutoloadRubble.RubbleData Data { get; }

	private readonly Dictionary<int, Info> _infoByStyle = [];

	public void AddInfo(Info value, int style) => _infoByStyle.Add(style, value);
	public void AddInfo(Info value, params int[] styles)
	{
		foreach (int style in styles)
			_infoByStyle.Add(style, value);
	}

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileMergeDirt[Type] = false;
		Main.tileBlockLight[Type] = false;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoFail[Type] = true;

		TileID.Sets.BreakableWhenPlacing[Type] = true;

		AddMapEntry(new Color(190, 150, 150));
		DustType = -1;
	}

	public override bool CreateDust(int i, int j, ref int type)
	{
		if (TileObjectData.GetTileData(Type, 0) is TileObjectData objectData && _infoByStyle.TryGetValue(Main.tile[i, j].TileFrameX / objectData.CoordinateFullWidth, out Info info))
			type = info.DustType;

		return true;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;
}

public class SaltDebrisTiny : SaltDebris
{
	public override IAutoloadRubble.RubbleData Data => new(AutoContent.ItemType<SaltBlockDull>(), IAutoloadRubble.RubbleSize.Small);

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = 10;
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		AddInfo(new(DustID.Stone), 0, 1, 2, 3, 4, 5, 6);
		AddInfo(new(DustID.Pearlsand), 7, 8, 9);
	}
}

public class SaltDebrisSmall : SaltDebris, IAutoloadRubble
{
	public override IAutoloadRubble.RubbleData Data => new(AutoContent.ItemType<SaltBlockDull>(), IAutoloadRubble.RubbleSize.Small);

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = 7;
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		AddInfo(new(DustID.Stone), 0, 1, 2);
		AddInfo(new(DustID.Pearlsand), 3, 4, 5);
		AddInfo(new(DustID.Pearlwood), 6);
	}
}

public class SaltDebrisMedium : SaltDebris, IAutoloadRubble
{
	public override IAutoloadRubble.RubbleData Data => new(AutoContent.ItemType<SaltBlockDull>(), IAutoloadRubble.RubbleSize.Medium);

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = new(1, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = 5;
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		AddInfo(new(DustID.Stone), 0, 1, 2, 3);
		AddInfo(new(DustID.Pearlwood), 4);
	}
}

public class SaltDebrisLarge : SaltDebris, IAutoloadRubble
{
	public override IAutoloadRubble.RubbleData Data => new(AutoContent.ItemType<SaltBlockDull>(), IAutoloadRubble.RubbleSize.Large);

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
		TileObjectData.newTile.Origin = new(2, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = 3;
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		AddInfo(new(DustID.Bone), 0, 1);
		AddInfo(new(DustID.Pearlwood), 2);
	}
}