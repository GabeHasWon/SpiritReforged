using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.TileMerging;
using SpiritReforged.Content.Particles;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.Tiles.Amber;

public abstract class ShiningAmber : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileMergeDirt[Type] = true;
		Main.tileBlockLight[Type] = false;
		Main.tileLighted[Type] = true;
		TileID.Sets.CanBeClearedDuringOreRunner[Type] = false;

		AddMapEntry(Color.Orange);
		this.Merge(ModContent.TileType<PolishedAmber>(), ModContent.TileType<AmberFossil>(), ModContent.TileType<AmberFossilSafe>(), TileID.Sand);

		DustType = DustID.GemAmber;
		MineResist = 0.5f;
	}

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak) => TileFraming.Gemspark(i, j, resetFrame);
	public override void NearbyEffects(int i, int j, bool closer)
	{
		if (closer && !Main.gamePaused && Main.rand.NextBool(3000))
		{
			Vector2 coords = new Vector2(i, j) * 16;
			Vector2 position = Main.rand.NextVector2FromRectangle(new((int)coords.X, (int)coords.Y, 16, 16));

			float scale = Main.rand.NextFloat(0.2f, 0.5f);
			ParticleHandler.SpawnParticle(new GlowParticle(position, Vector2.UnitY * -0.3f, Color.Goldenrod * 0.5f, scale, 200));
		}
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (!TileExtensions.GetVisualInfo(i, j, out Color color, out _))
			return false;

		ShiningAmberVisuals.ReflectionPoints.Add(new Point16(i, j));

		TileExtensions.DrawSingleTile(i, j, true, TileExtensions.TileOffset);
		TileMerger.DrawMerge(spriteBatch, i, j, color, TileExtensions.TileOffset, TileID.Sand);
		return false;
	}
}