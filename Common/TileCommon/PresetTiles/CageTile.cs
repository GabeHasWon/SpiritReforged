using SpiritReforged.Common.ItemCommon;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Common.TileCommon.PresetTiles;

public abstract class CageTile : ModTile, IAutoloadTileItem
{
	private static readonly int[] cageFrames = new int[Main.cageFrames];
	private static readonly int[] cageFrameCounters = new int[Main.cageFrames];

	public abstract int NumFrames { get; }

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;
		Main.tileSolidTop[Type] = true;
		Main.tileTable[Type] = true;

		TileID.Sets.CritterCageLidStyle[Type] = 0;

		AddObjectData();

		DustType = DustID.Glass;
		AnimationFrameHeight = 54;
		AdjTiles = [TileID.BirdCage];

		RegisterItemDrop(this.AutoItemType());
	}

	public virtual void AddObjectData()
	{
		TileObjectData.newTile.CopyFrom(TileObjectData.GetTileData(TileID.BirdCage, 0));
		TileObjectData.addTile(Type);
	}

	public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
	{
		offsetY = 2;
		Main.critterCage = true;
	}

	public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
	{
		var tile = Main.tile[i, j];
		int fullWidth = TileObjectData.GetTileData(type, 0)?.CoordinateFullWidth ?? 108;
		int tileCageFrameIndex = TileDrawing.GetBigAnimalCageFrame(i, j, tile.TileFrameX, tile.TileFrameY);

		frameYOffset = cageFrames[tileCageFrameIndex] % NumFrames * AnimationFrameHeight;
		frameXOffset = cageFrames[tileCageFrameIndex] / NumFrames * fullWidth;
	}

	public sealed override void AnimateTile(ref int frame, ref int frameCounter)
	{
		if (!Main.critterCage)
			return;

		for (int i = 0; i < Main.cageFrames; i++)
			AnimateCage(ref cageFrames[i], ref cageFrameCounters[i]);
	}

	public virtual void AnimateCage(ref int frame, ref int frameCounter) { }
	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
}