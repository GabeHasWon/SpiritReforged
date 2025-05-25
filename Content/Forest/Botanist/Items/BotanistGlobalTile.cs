using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;

namespace SpiritReforged.Content.Forest.Botanist.Items;

internal class BotanistGlobalTile : GlobalTile
{
	public override bool PreDraw(int i, int j, int type, SpriteBatch spriteBatch)
	{
		if (HerbTile.HerbTypes.Contains(type) && BotanistHat.SetActive(Main.LocalPlayer))
		{
			Tile tile = Main.tile[i, j];
			float darkness = (1.2f - Lighting.Brightness(i, j)) / 1.2f;

			spriteBatch.Draw(TextureAssets.Tile[type].Value, new Vector2(i, j) * 16 - Main.screenPosition + TileExtensions.TileOffset, 
				new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, 20), Color.Lerp(Lighting.GetColor(i, j), Color.DeepPink, darkness));

			return false;
		}

		return true;
	}
}