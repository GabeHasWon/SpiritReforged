using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.TileMerging;
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
	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) => (r, g, b) = (0.1f, 0.06f, 0.01f);

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
		color = intoRenderTarget ? Color.White : Color.Lerp(color, Color.White, 0.2f).Additive(240) * 0.8f;

		if (tile.Slope != SlopeType.Solid || tile.IsHalfBlock)
		{
			TileExtensions.DrawSloped(i, j, texture, color, offset);
			TileMerger.DrawMerge(spriteBatch, i, j, intoRenderTarget ? Color.Black : Lighting.GetColor(i, j), offset, TileID.Sand);

			return;
		}

		var source = new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, 16);
		spriteBatch.Draw(texture, new Vector2(i, j) * 16 - Main.screenPosition + (intoRenderTarget ? Vector2.Zero : TileExtensions.TileOffset), source, color, 0, Vector2.Zero, 1, default, 0);

		TileMerger.DrawMerge(spriteBatch, i, j, intoRenderTarget ? Color.Black : Lighting.GetColor(i, j), offset, TileID.Sand);
	}
}