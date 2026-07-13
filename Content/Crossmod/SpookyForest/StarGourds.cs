using SpiritReforged.Common.ModCompat;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Crossmod.SpookyForest;

internal class StarGourdGreen : StarGourd
{
	protected override IGourdInfo Info => new GourdInfo<StarGourdGreen>("GourdBlockGreenItem");
}

internal class StarGourdLime : StarGourd
{
	protected override IGourdInfo Info => new GourdInfo<StarGourdLime>("GourdBlockLimeItem");

	protected override bool ModifyObjectData(ModTile tile, TileObjectData newTile)
	{
		newTile.Width = 3;
		newTile.Height = 3;
		newTile.CoordinateHeights = [16, 16, 16];
		newTile.Origin = new Point16(1, 2);
		newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		newTile.CoordinateWidth = 16;
		newTile.CoordinatePadding = 2;
		newTile.DrawYOffset = 2;

		tile.AddMapEntry(new Color(194, 218, 9));
		return false;
	}
}

internal class StarGourdOrangeLime : StarGourd
{
	protected override IGourdInfo Info => new GourdInfo<StarGourdOrangeLime>("GourdBlockLimeOrangeItem", false);

	protected override bool ModifyObjectData(ModTile tile, TileObjectData newTile)
	{
		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x4);
		TileObjectData.newTile.Origin = new Point16(1, 3);
		TileObjectData.newTile.DrawYOffset = 2;

		tile.AddMapEntry(new Color(107, 171, 15));
		return false;
	}
}

internal class StarGourdOrange : StarGourd
{
	protected override IGourdInfo Info => new GourdInfo<StarGourdOrange>("GourdBlockOrangeItem");

	protected override bool ModifyObjectData(ModTile tile, TileObjectData newTile)
	{
		tile.AddMapEntry(new Color(195, 96, 27));
		return false;
	}
}

internal class StarGourdRed : StarGourd
{
	protected override IGourdInfo Info => new GourdInfo<StarGourdRed>("GourdBlockRedItem");

	protected override bool ModifyObjectData(ModTile tile, TileObjectData newTile)
	{
		newTile.Width = 3;
		newTile.Height = 3;
		newTile.CoordinateHeights = [16, 16, 16];
		newTile.Origin = new Point16(1, 2);
		newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		newTile.CoordinateWidth = 16;
		newTile.CoordinatePadding = 2;
		newTile.DrawYOffset = 2;

		tile.AddMapEntry(new Color(184, 47, 26));
		return false;
	}
}

internal class StarGourdRotten : StarGourd
{
	protected override IGourdInfo Info => new GourdInfo<StarGourdRotten>("RottenSeed", false);

	protected override bool ModifyObjectData(ModTile tile, TileObjectData newTile)
	{
		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x4);
		TileObjectData.newTile.Origin = new Point16(1, 3);
		TileObjectData.newTile.DrawYOffset = 2;

		tile.AddMapEntry(new Color(120, 96, 62));
		return false;
	}

	public override void KillMultiTile(int i, int j, int fX, int fY)
	{
		if (Main.rand.NextBool() && CrossMod.Spooky.TryFind("RottenChunk", out ModItem chunk))// && Flags.downedRotGourd) TODO
		{
			int randomAmount = Main.rand.Next(2, 6);

			for (int numItems = 0; numItems <= randomAmount; numItems++)
				Item.NewItem(new EntitySource_TileBreak(i, j), i * 16, j * 16, 32, 16, chunk.Type);
		}

		base.KillMultiTile(i, j, fX, fY);
	}
}

internal class StarGourdWhite : StarGourd
{
	protected override IGourdInfo Info => new GourdInfo<StarGourdWhite>("GourdBlockWhiteItem");

	protected override bool ModifyObjectData(ModTile tile, TileObjectData newTile)
	{
		newTile.Width = 3;
		newTile.Height = 3;
		newTile.CoordinateHeights = [16, 16, 16];
		newTile.Origin = new Point16(1, 2);
		newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		newTile.CoordinateWidth = 16;
		newTile.CoordinatePadding = 2;
		newTile.DrawYOffset = 2;

		tile.AddMapEntry(new Color(165, 173, 177));
		return false;
	}
}

internal class StarGourdYellow : StarGourd
{
	protected override IGourdInfo Info => new GourdInfo<StarGourdYellow>("GourdBlockYellowItem");

	protected override bool ModifyObjectData(ModTile tile, TileObjectData newTile)
	{
		TileObjectData.newTile.CopyFrom(TileObjectData.Style2xX);
		TileObjectData.newTile.Height = 2;
		TileObjectData.newTile.CoordinateHeights = [16, 16];
		TileObjectData.newTile.Origin = new Point16(0, 1);
		TileObjectData.newTile.DrawYOffset = 2;

		tile.AddMapEntry(new Color(195, 146, 27));
		return false;
	}
}

internal class StarGourdYellowGreen : StarGourd
{
	protected override IGourdInfo Info => new GourdInfo<StarGourdYellowGreen>("GourdBlockYellowGreenItem");

	protected override bool ModifyObjectData(ModTile tile, TileObjectData newTile)
	{
		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x4);
		TileObjectData.newTile.Width = 4;
		TileObjectData.newTile.Height = 3;
		TileObjectData.newTile.Origin = new Point16(1, 2);
		TileObjectData.newTile.CoordinateHeights = [16, 16, 16];
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.CoordinateWidth = 16;
		TileObjectData.newTile.CoordinatePadding = 2;
		TileObjectData.newTile.DrawYOffset = 2;

		tile.AddMapEntry(new Color(162, 171, 15));
		return false;
	}
}