using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.Tiles.Amber;

public partial class PolishedAmber : ModTile, IAutoloadTileItem
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileMergeDirt[Type] = true;
		Main.tileBlockLight[Type] = false;
		Main.tileLighted[Type] = true;

		AddMapEntry(Color.Orange);
		DustType = DustID.GemAmber;
		MineResist = .5f;

		this.AutoItem().ResearchUnlockCount = 100;
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) => (r, g, b) = (0.1f, 0.06f, 0.01f);
	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		var tile = Main.tile[i, j];
		var texture = TextureAssets.Tile[Type].Value;
		var source = new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, 16);
		var color = Color.Lerp(Lighting.GetColor(i, j), Color.White, 0.2f).Additive(240) * 0.8f;

		spriteBatch.Draw(texture, new Vector2(i, j) * 16 - Main.screenPosition + TileExtensions.TileOffset, source, color, 0, Vector2.Zero, 1, default, 0);

		ReflectionPoints.Add(new Point16(i, j));
		return false;
	}
}