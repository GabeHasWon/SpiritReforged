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
		CustomDraw(i, j, spriteBatch);
		ShiningAmberVisuals.ReflectionPoints.Add(new Point16(i, j));

		return false;
	}

	public static void CustomDraw(int i, int j, SpriteBatch spriteBatch, bool intoRenderTarget = false)
	{
		if (!TileExtensions.GetVisualInfo(i, j, out Color color, out var texture))
			return;

		Tile tile = Main.tile[i, j];
		Vector2 offset = intoRenderTarget ? Vector2.Zero : TileExtensions.TileOffset;

		if (tile.Slope != SlopeType.Solid || tile.IsHalfBlock)
		{
			TileExtensions.DrawSloped(i, j, texture, color, offset);
			TileMerger.DrawMerge(spriteBatch, i, j, intoRenderTarget ? Color.Black : color, offset, TileID.Sand);

			return;
		}

		var source = new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, 16);
		spriteBatch.Draw(texture, new Vector2(i, j) * 16 - Main.screenPosition + (intoRenderTarget ? Vector2.Zero : TileExtensions.TileOffset), source, color, 0, Vector2.Zero, 1, default, 0);

		TileMerger.DrawMerge(spriteBatch, i, j, intoRenderTarget ? Color.Black : color, offset, TileID.Sand);
	}
}