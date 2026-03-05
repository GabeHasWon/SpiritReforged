using RubbleAutoloader;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals.Glowmasks;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Ocean.Tiles;

[AutoloadGlowmask("255,255,255")]
public class OceanDecor1x2 : ModTile, IAutoloadRubble
{
	public virtual IAutoloadRubble.RubbleData Data => new(ItemID.Coral, IAutoloadRubble.RubbleSize.Small);

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileBlockLight[Type] = false;
		Main.tileFrameImportant[Type] = true;
		Main.tileLighted[Type] = true;
		
		AddTileObjectData();

		SolidBottomTile.TileTypes.Add(Type);
		AddMapEntry(new Color(121, 92, 19));
		DustType = DustID.Coralstone;
		HitSound = SoundID.Dig;
	}

	public override bool CreateDust(int i, int j, ref int type)
	{
		Tile tile = Main.tile[i, j];
		var data = TileObjectData.GetTileData(tile);

		if (tile.TileFrameX >= data.CoordinateFullWidth)
			type = DustID.Coralstone;
		else
			type = DustID.Grass;

		return true;
	}

	protected virtual Vector3 GlowColor(int x, int y)
	{
		var tile = Main.tile[x, y];

		return ((tile.TileFrameX / 18) switch
		{
			0 => Color.Yellow,
			2 => Color.Cyan,
			_ => Color.HotPink * .25f
		}).ToVector3();
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		float randomLerp = (float)(1 + Math.Sin(i / 10f) / 2f) * .2f;
		float seedLerp = .4f + (float)(1 + Math.Sin(Main.ActiveWorldFileData.Seed) / 2f) * .2f;

		Vector3 color = Vector3.Lerp(GlowColor(i, j), Color.White.ToVector3(), seedLerp + randomLerp) / 4.5f;
		(r, g, b) = (color.X, color.Y, color.Z);
	}

	protected virtual void AddTileObjectData()
	{
		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2);
		TileObjectData.newTile.CoordinateHeights = [16, 18];
		//TileObjectData.newTile.DrawYOffset = -2;
		TileObjectData.newTile.RandomStyleRange = 3;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.Origin = new(0, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, 1, 0);
		TileObjectData.newTile.AnchorValidTiles = [TileID.Sand, TileID.Crimsand, TileID.Pearlsand, TileID.Ebonsand];
		TileObjectData.addTile(Type);
	}

	public override IEnumerable<Item> GetItemDrops(int i, int j)
	{
		yield return new Item(ItemID.Coral, 1);
	}
}

public class OceanDecor2x2 : OceanDecor1x2
{
	public override IAutoloadRubble.RubbleData Data => new(ItemID.Coral, IAutoloadRubble.RubbleSize.Medium);

	protected override void AddTileObjectData()
	{
		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.CoordinateHeights = [16, 18];
		//TileObjectData.newTile.DrawYOffset = -2;
		TileObjectData.newTile.RandomStyleRange = 4;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.Origin = new(0, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, 2, 0);
		TileObjectData.newTile.AnchorValidTiles = [TileID.Sand, TileID.Crimsand, TileID.Pearlsand, TileID.Ebonsand];
		TileObjectData.addTile(Type);
	}

	protected override Vector3 GlowColor(int x, int y)
	{
		var tile = Main.tile[x, y];

		return ((tile.TileFrameX / 32) switch
		{
			0 => Color.Yellow,
			1 => Color.Cyan,
			3 => Color.Green,
			_ => Color.Red * .25f
		}).ToVector3();
	}

	public override IEnumerable<Item> GetItemDrops(int i, int j)
	{
		yield return new Item(ItemID.Coral, Main.rand.Next(2, 4));
	}
}

public class OceanDecor2x3 : OceanDecor1x2
{
	public override IAutoloadRubble.RubbleData Data => new(ItemID.Coral, IAutoloadRubble.RubbleSize.Large);

	protected override void AddTileObjectData()
	{
		TileObjectData.newTile.CopyFrom(TileObjectData.Style2xX);
		TileObjectData.newTile.Height = 3;
		TileObjectData.newTile.CoordinateHeights = [16, 16, 18];
		//TileObjectData.newTile.DrawYOffset = -2;
		TileObjectData.newTile.RandomStyleRange = 2;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.Origin = new Point16(1, 2);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, 2, 0);
		TileObjectData.newTile.AnchorValidTiles = [TileID.Sand, TileID.Crimsand, TileID.Pearlsand, TileID.Ebonsand];
		TileObjectData.addTile(Type);
	}

	protected override Vector3 GlowColor(int x, int y) => Color.Yellow.ToVector3();

	public override IEnumerable<Item> GetItemDrops(int i, int j)
	{
		yield return new Item(ItemID.Coral, Main.rand.Next(3, 6));
	}
}