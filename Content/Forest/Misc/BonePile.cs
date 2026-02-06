namespace SpiritReforged.Content.Forest.Misc;

public class BonePile : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileBlockLight[Type] = true;

		TileID.Sets.GeneralPlacementTiles[Type] = false;
		TileID.Sets.CanBeClearedDuringOreRunner[Type] = false;

		AddMapEntry(new Color(150, 140, 110));
		DustType = DustID.Bone;
	}
}