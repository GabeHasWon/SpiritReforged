﻿using Terraria.DataStructures;
using static Terraria.GameContent.Drawing.TileDrawing;

namespace SpiritReforged.Common.TileCommon.TileSway;

internal class SwayGlobalTile : GlobalTile
{
	public override bool PreDraw(int i, int j, int type, SpriteBatch spriteBatch)
	{
		if (Main.LightingEveryFrame && TileSwaySystem.DoesSway(type, out int counter))
		{
			var cType = (TileCounterType)counter;

			if (cType is TileCounterType.Vine)
				Main.instance.TilesRenderer.CrawlToTopOfVineAndAddSpecialPoint(j, i);
			else if (cType is TileCounterType.ReverseVine)
				Main.instance.TilesRenderer.CrawlToBottomOfReverseVineAndAddSpecialPoint(j, i);
			else if (TileObjectData.IsTopLeft(i, j)) //IsTopLeft also prevents invalid object data
			{
				if (counter == -1)
					DrawCustomSway(i, j, spriteBatch);
				else
				{
					if (cType is TileCounterType.MultiTileGrass)
					{
						var origin = TileObjectData.GetTileData(Main.tile[i, j]).Origin;
						Main.instance.TilesRenderer.AddSpecialPoint(i + origin.X, j + origin.Y, cType);
					}
					else if (cType is TileCounterType.MultiTileVine)
						Main.instance.TilesRenderer.AddSpecialPoint(i, j, cType);
				}
			}

			return false;
		}

		return true;
	}

	private static void DrawCustomSway(int i, int j, SpriteBatch spriteBatch)
	{
		var tile = Main.tile[i, j];
		if (TileLoader.GetTile(tile.TileType) is not ISwayTile sway)
			return;

		var data = TileObjectData.GetTileData(tile);
		float physics = sway.Physics(new Point16(i, j));

		for (int x = 0; x < data.Width; x++)
		{
			for (int y = 0; y < data.Height; y++)
			{
				int tileOriginY = (data.Origin.Y == 0 && data.Height > 1) ? data.Origin.Y : data.Origin.Y + 1;

				float rotation;
				if (tileOriginY == 0)
				{
					float swing = 1f - (1f - (float)(y + 1) / data.Height) + .5f;
					rotation = physics * swing * -.1f;
				}
				else
				{
					float swing = 1f - (float)(y + 1) / tileOriginY + .5f;
					rotation = physics * swing * .1f;
				}

				var rotationOffset = new Vector2(0, Math.Abs(rotation) * 20f) * ((tileOriginY == 0) ? -1 : 1);

				var drawOrigin = new Vector2(-(x * 16) + (data.Origin.X + 1) * 16, -(y * 16) + tileOriginY * 16);
				if (data.Width % 2 != 0)
					drawOrigin.X -= 8; //Center drawOrigin for multitiles with odd width

				sway.DrawSway(i + x, j + y, spriteBatch, drawOrigin + rotationOffset, rotation, drawOrigin);
			}
		}
	}
}
