using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.Tiles.Amber;

public partial class PolishedAmber : ModTile, IAutoloadTileItem
{
	public const int FullFrameHeight = 90;

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileMergeDirt[Type] = true;
		Main.tileBlockLight[Type] = false;
		Main.tileLighted[Type] = true;
		Main.tileBrick[Type] = true;

		AddMapEntry(Color.Orange);
		DustType = DustID.GemAmber;
		MineResist = .5f;

		this.AutoItem().ResearchUnlockCount = 100;
	}

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak) => TileFraming.Gemspark(i, j, resetFrame);
	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) => (r, g, b) = (0.1f, 0.06f, 0.01f);

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		CustomDraw(i, j, spriteBatch);
		ReflectionPoints.Add(new Point16(i, j));

		return false;
	}

	public static void CustomDraw(int i, int j, SpriteBatch spriteBatch, bool intoRenderTarget = false)
	{
		if (!TileExtensions.GetVisualInfo(i, j, out Color color, out var texture))
			return;

		var tile = Main.tile[i, j];
		color = intoRenderTarget ? Color.White : Color.Lerp(color, Color.White, 0.2f).Additive(240) * 0.8f;

		if (tile.Slope != SlopeType.Solid || tile.IsHalfBlock)
		{
			Vector2 offset = intoRenderTarget ? -TileExtensions.TileOffset : Vector2.Zero;
			TileExtensions.DrawSloped(i, j, texture, color, offset);

			return;
		}

		var source = new Rectangle(tile.TileFrameX, tile.TileFrameY % FullFrameHeight, 16, 16);
		spriteBatch.Draw(texture, new Vector2(i, j) * 16 - Main.screenPosition + (intoRenderTarget ? Vector2.Zero : TileExtensions.TileOffset), source, color, 0, Vector2.Zero, 1, default, 0);
	}
}