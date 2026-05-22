using SpiritReforged.Common.ItemCommon;

namespace SpiritReforged.Content.Forest.Trophies;

/// <summary> Contains all Spirit boss trophies in different styles. </summary>
public class Trophies : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileLavaDeath[Type] = true;
		TileID.Sets.FramesOnKillWall[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.StyleWrapLimit = 21;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(120, 85, 60), Language.GetText("MapObject.Trophy"));
		DustType = DustID.WoodFurniture;
	}
}

public class ScarabTrophy : ModItem
{
	public override void SetDefaults() => Item.DefaultToTrophy(0);
}