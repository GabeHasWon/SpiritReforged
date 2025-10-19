using SpiritReforged.Common.TileCommon;
using Terraria.Audio;

namespace SpiritReforged.Content.SaltFlats.Tiles.Salt;

public abstract class SaltBlock : ModTile, IAutoloadTileItem
{
	public static readonly SoundStyle Break = new("SpiritReforged/Assets/SFX/Tile/SaltMine", 3)
	{
		Volume = 0.5f,
		PitchVariance = 0.3f
	};

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = false;

		TileID.Sets.ChecksForMerge[Type] = true;
		this.Merge(TileID.IceBlock, TileID.SnowBlock, TileID.Sand, TileID.Dirt);

		DustType = DustID.Pearlsand;
		MineResist = 0.5f;
		HitSound = Break;
	}

	public override void ModifyFrameMerge(int i, int j, ref int up, ref int down, ref int left, ref int right, ref int upLeft, ref int upRight, ref int downLeft, ref int downRight)
		=> WorldGen.TileMergeAttempt(-2, TileID.IceBlock, ref up, ref down, ref left, ref right, ref upLeft, ref upRight, ref downLeft, ref downRight);
}