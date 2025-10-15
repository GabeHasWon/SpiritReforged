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
			Texture2D tex = TextureAssets.Tile[type].Value;

			if (type == TileID.MatureHerbs && WorldGen.IsHarvestableHerbWithSeed(type, Main.tile[i, j].TileFrameX / 18))
				tex = TextureAssets.Tile[TileID.BloomingHerbs].Value;

			Rectangle src = new(tile.TileFrameX, tile.TileFrameY, 16, 20);
			Vector2 position = new Vector2(i, j) * 16 - Main.screenPosition + TileExtensions.TileOffset - new Vector2(0, 2) + src.Bottom();
			SpriteEffects effects = i % 2 == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
			float rotation = Main.instance.TilesRenderer.GetWindCycle(i, j, Main.instance.TilesRenderer._grassWindCounter) * 0.2f;
			spriteBatch.Draw(tex, position, src, Color.Lerp(Lighting.GetColor(i, j), Color.Green, darkness), rotation, src.Bottom(), 1f, effects, 0f);

			return false;
		}

		return true;
	}
}