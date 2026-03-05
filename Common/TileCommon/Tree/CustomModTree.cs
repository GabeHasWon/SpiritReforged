namespace SpiritReforged.Common.TileCommon.Tree;

/// <summary> Dummy <see cref="ModTree"/> used by <see cref="CustomTree"/>s so associated saplings can function normally.<para/>
/// Normally instantiated by <see cref="PresetTiles.SaplingTile.Autoload"/>.</summary>
/// <param name="saplingType"> The sapling type this tree grows from. </param>
/// <param name="anchorTypes"> The tile types this tree can grow on. </param>
public sealed class CustomModTree(int saplingType, params int[] anchorTypes) : ModTree
{
	public override int SaplingGrowthType(ref int style)
	{
		style = 0;
		return saplingType;
	}

	public override void SetStaticDefaults()
	{
		if (anchorTypes == null)
			return;

		GrowsOnTileId = anchorTypes;
	}

	public override TreePaintingSettings TreeShaderSettings => new()
	{
		UseSpecialGroups = true,
		SpecialGroupMinimalHueValue = 11f / 72f,
		SpecialGroupMaximumHueValue = 0.25f,
		SpecialGroupMinimumSaturationValue = 0.88f,
		SpecialGroupMaximumSaturationValue = 1f
	};

	public override int DropWood() => ItemID.Wood;
	public override Asset<Texture2D> GetBranchTextures() => TextureAssets.TreeBranch[0];
	public override Asset<Texture2D> GetTexture() => TextureAssets.Tile[TileID.Trees];
	public override Asset<Texture2D> GetTopTextures() => TextureAssets.TreeTop[0];
	public override void SetTreeFoliageSettings(Tile tile, ref int xoffset, ref int treeFrame, ref int floorY, ref int topTextureFrameWidth, ref int topTextureFrameHeight) { }
}