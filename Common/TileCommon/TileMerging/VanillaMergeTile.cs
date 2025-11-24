namespace SpiritReforged.Common.TileCommon.TileMerging;

/// <summary> Handles merge logic for vanilla tiles. </summary>
internal sealed class VanillaMergeTile : GlobalTile
{
	public override void SetStaticDefaults() => TileExtensions.Merge(TileID.ClayBlock, TileID.Sand);

	public override void PostDraw(int i, int j, int type, SpriteBatch spriteBatch)
	{
		if (type == TileID.ClayBlock)
			TileMerger.DrawMerge(spriteBatch, i, j, TileID.Sand);
	}
}