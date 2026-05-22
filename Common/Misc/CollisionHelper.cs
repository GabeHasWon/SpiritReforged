namespace SpiritReforged.Common.Misc;

internal static class CollisionHelper 
{
	/// <summary>
	/// Behaves the exact same as <see cref="Collision.SolidCollision(Vector2, int, int)"/> with the exception that non-wooden platforms are properly recognized as surfaces
	/// </summary>
	/// <param name="Position"></param>
	/// <param name="Width"></param>
	/// <param name="Height"></param>
	/// <param name="acceptTopSurfaces"></param>
	/// <returns></returns>
	public static bool SolidCollision(Vector2 Position, int Width, int Height, bool acceptTopSurfaces)
	{
		int value = (int)(Position.X / 16f) - 1;
		int value2 = (int)((Position.X + (float)Width) / 16f) + 2;
		int value3 = (int)(Position.Y / 16f) - 1;
		int value4 = (int)((Position.Y + (float)Height) / 16f) + 2;
		int num = Utils.Clamp(value, 0, Main.maxTilesX - 1);
		value2 = Utils.Clamp(value2, 0, Main.maxTilesX - 1);
		value3 = Utils.Clamp(value3, 0, Main.maxTilesY - 1);
		value4 = Utils.Clamp(value4, 0, Main.maxTilesY - 1);
		Vector2 vector = default(Vector2);
		for (int i = num; i < value2; i++)
		{
			for (int j = value3; j < value4; j++)
			{
				Tile tile = Main.tile[i, j];
				if (tile == null || !tile.HasUnactuatedTile)
					continue;

				bool flag = Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
				if (acceptTopSurfaces)
					flag |= (Main.tileSolidTop[tile.TileType] && tile.TileFrameY == 0) || TileID.Sets.Platforms[tile.TileType];

				if (flag)
				{
					vector.X = i * 16;
					vector.Y = j * 16;
					int num2 = 16;
					if (tile.IsHalfBlock)
					{
						vector.Y += 8f;
						num2 -= 8;
					}

					if (Position.X + (float)Width > vector.X && Position.X < vector.X + 16f && Position.Y + (float)Height > vector.Y && Position.Y < vector.Y + (float)num2)
						return true;
				}
			}
		}

		return false;
	}
}
