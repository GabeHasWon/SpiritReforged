using SpiritReforged.Content.Underground.Tiles;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underground.Pottery;

public class BiomePotsEcho : BiomePots
{
	public override string Texture => (typeof(BiomePots).Namespace + "." + typeof(BiomePots).Name).Replace('.', '/');

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		Main.tileCut[Type] = false;
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY) { }
	public override void NearbyEffects(int i, int j, bool closer) { }
	public override bool KillSound(int i, int j, bool fail) => true;
	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch) => true;
}

public class CommonPotsEcho : ModTile
{
	public override string Texture => StackablePots.PotTexture;

	public override void SetStaticDefaults()
	{
		const int row = 3;

		Main.tileSolid[Type] = false;
		Main.tileBlockLight[Type] = false;
		Main.tileFrameImportant[Type] = true;
		Main.tileSpelunker[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = new(0, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.StyleWrapLimit = row;
		TileObjectData.newTile.RandomStyleRange = row * 3;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.UsesCustomCanPlace = true;
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(100, 90, 35), Language.GetText(StackablePots.NameKey));
		DustType = -1;
	}
}