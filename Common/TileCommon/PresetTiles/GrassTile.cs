using System.Linq;

namespace SpiritReforged.Common.TileCommon.PresetTiles;

public abstract class GrassTile : ModTile
{
	protected virtual int DirtType => TileID.Dirt;

	protected void AllowAnchor(params int[] types)
	{
		foreach (int type in types)
		{
			var data = TileObjectData.GetTileData(type, 0);
			if (data != null)
				data.AnchorValidTiles = data.AnchorValidTiles.Concat([Type]).ToArray();
		}
	}

	/// <summary><inheritdoc/>
	/// <para/>Also automatically controls common grass tile settings. </summary>
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;
		Main.tileBlendAll[Type] = true;

		this.Merge(DirtType, TileID.Grass);
		AllowAnchor(TileID.Sunflower);
		TileID.Sets.Grass[Type] = true;
		TileID.Sets.CanBeDugByShovel[Type] = true;
		TileID.Sets.NeedsGrassFramingDirt[Type] = DirtType;
		TileID.Sets.NeedsGrassFraming[Type] = true;
	}

	public override bool CanExplode(int i, int j)
	{
		WorldGen.KillTile(i, j, false, false, true); //Makes the tile completely go away instead of reverting to dirt
		return true;
	}

	public override void RandomUpdate(int i, int j)
	{
		SpreadHelper.Spread(i, j, Type, 4, DirtType);
		GrowPlants(i, j);
	}

	public virtual void GrowPlants(int i, int j) { }

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;
	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (!effectOnly) //Change self into dirt
		{
			fail = true;
			WorldGen.KillTile_MakeTileDust(i, j, Main.tile[i, j]);
			Framing.GetTileSafely(i, j).TileType = (ushort)DirtType;
		}
	}
}