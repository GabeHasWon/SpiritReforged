namespace SpiritReforged.Content.Forest.Misc;

public class GiantThorns : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileMerge[Type][TileID.Dirt] = true;
		Main.tileMerge[Type][TileID.Grass] = true;

		TileID.Sets.GeneralPlacementTiles[Type] = false;
		TileID.Sets.CanBeClearedDuringOreRunner[Type] = false;

		AddMapEntry(new Color(125, 160, 50));

		DustType = DustID.JunglePlants;
		HitSound = SoundID.Grass;
	}
}