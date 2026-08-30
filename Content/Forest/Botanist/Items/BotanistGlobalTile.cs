using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using TileHelper.Common;

namespace SpiritReforged.Content.Forest.Botanist.Items;

internal class BotanistGlobalTile : GlobalTile
{
	public override bool PreDraw(int i, int j, int type, SpriteBatch spriteBatch)
	{
		if (HerbSet.IsHerb[type] && BotanistHat.SetActive(Main.LocalPlayer))
		{
			Tile tile = Main.tile[i, j];
			float darkness = (1.2f - Lighting.Brightness(i, j)) / 1.2f;
			Texture2D tex = TextureAssets.Tile[type].Value;

			if (type == TileID.MatureHerbs && WorldGen.IsHarvestableHerbWithSeed(type, Main.tile[i, j].TileFrameX / 18))
				tex = TextureAssets.Tile[TileID.BloomingHerbs].Value;

			Rectangle src = new(tile.TileFrameX, tile.TileFrameY, 16, 20);
			var origin = new Vector2(src.Width / 2f, src.Height);
			Vector2 position = Helpers.GetTilePosition(i, j) + origin - new Vector2(0, 2);
			SpriteEffects effects = i % 2 == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
			float rotation = Main.instance.TilesRenderer.GetWindCycle(i, j, Main.instance.TilesRenderer._grassWindCounter) * 0.2f;
			spriteBatch.Draw(tex, position, src, Color.Lerp(Lighting.GetColor(i, j), Color.Green, darkness), rotation, origin, 1f, effects, 0f);

			return false;
		}

		return true;
	}
}