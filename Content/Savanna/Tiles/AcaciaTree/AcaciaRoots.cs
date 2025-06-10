using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.Corruption;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Savanna.Tiles.AcaciaTree;

public class AcaciaRootsLarge : ModTile, IConvertibleTile
{
	public virtual Point FrameOffset => Point.Zero;

	public override string Texture => base.Texture.Replace("Large", string.Empty);

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;
		Main.tileNoFail[Type] = true;

		TileID.Sets.BreakableWhenPlacing[Type] = true;

		SetObjectData();

		DustType = DustID.WoodFurniture;
		RegisterItemDrop(AutoContent.ItemType<Drywood>());
		AddMapEntry(new Color(87, 61, 51));
	}

	public virtual void SetObjectData()
	{
		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
		TileObjectData.newTile.Width = 3;
		TileObjectData.newTile.Height = 1;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.Origin = new Point16(1, 0);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SavannaDirt>(), ModContent.TileType<SavannaGrass>(), 
			ModContent.TileType<SavannaGrassCorrupt>(), ModContent.TileType<SavannaGrassCrimson>(), ModContent.TileType<SavannaGrassHallow>()];
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.newTile.RandomStyleRange = 4;
		TileObjectData.addTile(Type);
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (!TileExtensions.GetVisualInfo(i, j, out var color, out var texture))
			return false;

		var t = Main.tile[i, j];
		var frame = new Point(t.TileFrameX + 18 * FrameOffset.X, t.TileFrameY + 18 * FrameOffset.Y);
		var source = new Rectangle(frame.X, frame.Y, 16, 16);
		var position = new Vector2(i, j) * 16 - Main.screenPosition + TileExtensions.TileOffset + new Vector2(0, 2);

		spriteBatch.Draw(texture, position, source, color, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
		return false;
	}

	public virtual bool Convert(IEntitySource source, ConversionType type, int i, int j)
	{
		TileExtensions.GetTopLeft(ref i, ref j);

		for (int w = 0; w < 3; w++)
		{
			var tile = Main.tile[i + w, j];

			tile.TileType = (ushort)(type switch
			{
				ConversionType.Hallow => ModContent.TileType<AcaciaRootsLargeHallow>(),
				ConversionType.Crimson => ModContent.TileType<AcaciaRootsLargeCrimson>(),
				ConversionType.Corrupt => ModContent.TileType<AcaciaRootsLargeCorrupt>(),
				_ => ModContent.TileType<AcaciaRootsLarge>(),
			});
		}

		return true;
	}
}

public class AcaciaRootsSmall : AcaciaRootsLarge
{
	public override Point FrameOffset => new(12, 0);

	public override string Texture => base.Texture.Replace("Small", string.Empty);

	public override void SetObjectData()
	{
		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
		TileObjectData.newTile.Width = 2;
		TileObjectData.newTile.Height = 1;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.Origin = new Point16(1, 0);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SavannaDirt>(), ModContent.TileType<SavannaGrass>()];
		TileObjectData.newTile.RandomStyleRange = 2;
		TileObjectData.addTile(Type);
	}

	public override bool Convert(IEntitySource source, ConversionType type, int i, int j)
	{
		TileExtensions.GetTopLeft(ref i, ref j);

		for (int w = 0; w < 2; w++)
		{
			var tile = Main.tile[i + w, j];

			tile.TileType = (ushort)(type switch
			{
				ConversionType.Hallow => ModContent.TileType<AcaciaRootsSmallHallow>(),
				ConversionType.Crimson => ModContent.TileType<AcaciaRootsSmallCrimson>(),
				ConversionType.Corrupt => ModContent.TileType<AcaciaRootsSmallCorrupt>(),
				_ => ModContent.TileType<AcaciaRootsSmall>(),
			});
		}

		return true;
	}
}

public class AcaciaRootsLargeCorrupt : AcaciaRootsLarge
{
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		TileID.Sets.Corrupt[Type] = true;
	}
}

public class AcaciaRootsLargeCrimson : AcaciaRootsLarge
{
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		TileID.Sets.Crimson[Type] = true;
	}
}

public class AcaciaRootsLargeHallow : AcaciaRootsLarge
{
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		TileID.Sets.Hallow[Type] = true;
	}
}

public class AcaciaRootsSmallCorrupt : AcaciaRootsSmall
{
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		TileID.Sets.Corrupt[Type] = true;
	}
}

public class AcaciaRootsSmallCrimson : AcaciaRootsSmall
{
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		TileID.Sets.Crimson[Type] = true;
	}
}

public class AcaciaRootsSmallHallow : AcaciaRootsSmall
{
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		TileID.Sets.Hallow[Type] = true;
	}
}