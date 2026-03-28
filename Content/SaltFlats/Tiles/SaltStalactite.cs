using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using Terraria.DataStructures;

namespace SpiritReforged.Content.SaltFlats.Tiles;

public class SaltStalactite : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLighted[Type] = true;
		Main.tileLavaDeath[Type] = true;
		TileID.Sets.DrawTileInSolidLayer[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2);
		TileObjectData.newTile.Origin = Point16.Zero;
		TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile, 1, 0);
		TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
		TileObjectData.newTile.DrawYOffset = -2;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = 3;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(160, 150, 150));
		DustType = -1;
		HitSound = SaltBlock.Break;
	}

	public override void EmitParticles(int i, int j, Tile tile, short tileFrameX, short tileFrameY, Color tileLight, bool visible)
	{
		if (Main.rand.NextBool(25))
		{
			var dust = Dust.NewDustDirect(new Vector2(i, j) * 16, 16, 16, DustID.Snow, 0, 0, 120, default, 0.1f);
			dust.velocity = Vector2.UnitY * Main.rand.NextFloat(0.2f, 0.5f);
			dust.fadeIn = 1;
		}
	}
}