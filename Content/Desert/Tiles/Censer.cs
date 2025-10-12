using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Particles;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.Tiles;

public class Censer : ModTile, IAutoloadTileItem
{
	public override void SetStaticDefaults()
	{
		Main.tileTable[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.CoordinateHeights = [16, 16];
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(140, 140, 140));
		DustType = -1;
	}

	public override void EmitParticles(int i, int j, Tile tile, short tileFrameX, short tileFrameY, Color tileLight, bool visible)
	{
		if (visible && TileObjectData.IsTopLeft(i, j))
			EmitSmoke(new Rectangle(i * 16, j * 16, 32, 32));
	}

	public static void EmitSmoke(Rectangle hitbox)
	{
		if (Main.rand.NextBool(3))
		{
			var spawn = Main.rand.NextVector2FromRectangle(hitbox);
			float scale = Main.rand.NextFloat(0.5f, 1.5f);
			var velocity = (Vector2.UnitY * -1f).RotatedBy(Math.Sin(Main.timeForVisualEffects / 20f) / 3);

			ParticleHandler.SpawnParticle(new SteamParticle(spawn, velocity, scale, 60, ParticleLayer.AbovePlayer) { Color = Color.White * 0.2f });
		}
	}
}