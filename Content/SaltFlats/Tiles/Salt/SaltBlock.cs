using SpiritReforged.Common.TileCommon;

namespace SpiritReforged.Content.SaltFlats.Tiles.Salt;

public class SaltBlock : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileMergeDirt[Type] = true;
		Main.tileBlockLight[Type] = false;

		TileID.Sets.ChecksForMerge[Type] = true;

		AddMapEntry(new Color(230, 220, 220));
		this.Merge(TileID.IceBlock, TileID.SnowBlock, TileID.Sand);

		DustType = DustID.Pearlsand;
		MineResist = 0.5f;
	}

	public override void PostSetDefaults() => Main.tileNoSunLight[Type] = false;

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		SaltBlockVisuals.ReflectionPoints.Add(new(i, j));
		return true;
	}

	public override void ModifyFrameMerge(int i, int j, ref int up, ref int down, ref int left, ref int right, ref int upLeft, ref int upRight, ref int downLeft, ref int downRight)
		=> WorldGen.TileMergeAttempt(-2, TileID.IceBlock, ref up, ref down, ref left, ref right, ref upLeft, ref upRight, ref downLeft, ref downRight);
}