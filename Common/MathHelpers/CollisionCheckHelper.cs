namespace SpiritReforged.Common.MathHelpers;

public static class CollisionCheckHelper
{
	public static bool CheckSolidTilesAndPlatforms(Rectangle range)
	{
		int startX = range.X;
		int endX = range.X + range.Width;

		int startY = range.Y;
		int endY = range.Y + range.Height;

		if (Collision.SolidTiles(startX, endX, startY, endY))
			return true;

		for (int x = startX; x <= endX; x++)
		{
			for (int y = startY; y <= endY; y++)
			{
				if (TileID.Sets.Platforms[Framing.GetTileSafely(new Point(x, y)).TileType])
					return true;
			}
		}

		return false;
	}

	public static bool LineOfSightSolidTop(Vector2 start, Vector2 end)
	{
		Vector2 checkPoint = start;
		int numChecks = (int)(start.Distance(end) / 16f);

		for(int i = 0; i < numChecks; i++)
		{
			checkPoint += start.DirectionTo(end) * 16;
			if (Main.tileSolidTop[Framing.GetTileSafely(checkPoint).TileType] || Collision.SolidTiles(checkPoint, 1, 1))
				return true;
		}

		return false;
	}

	/// <summary> Checks whether <paramref name="end"/> can be reached by traversing solid tiles for a gravity-affected entity. </summary>
	/// <param name="start"> The entity trying to reach <paramref name="end"/>. </param>
	/// <param name="end"> The entity to reach. </param>
	/// <param name="allowedHeight"> How many pixels above <paramref name="start"/> can be reached. If the entity jumps, include jump height here. </param>
	/// <param name="fallHeightCutoff"> The maximum number of pixels <paramref name="start"/> can fall before this check is invalidated. </param>
	public static bool CanReachFromGround(Entity start, Entity end, int allowedHeight, int fallHeightCutoff = 200)
	{
		const int moveDist = 16;

		int direction = Math.Sign(end.Center.X - start.Center.X);
		int loops = (int)(Math.Abs(start.Center.X - end.Center.X) / moveDist) + 1;
		Vector2 position = start.position;

		for (int i = 0; i < loops; i++)
		{
			float oldY = position.Y;

			while (!Collision.SolidCollision(position + new Vector2(0, moveDist), start.width, start.height) && !Collision.IsWorldPointSolid(position + new Vector2(start.width / 2, start.height)))
			{
				position.Y += moveDist; //Move down

				if (Math.Abs(oldY - position.Y) > fallHeightCutoff)
					return false;
			}

			while (Collision.SolidCollision(position, start.width, start.height))
			{
				position.Y -= moveDist; //Move up

				if (Math.Abs(oldY - position.Y) > allowedHeight)
					return false;
			}

			position.X += direction * moveDist;

			Rectangle hit = Hitbox();
			if ((hit with { Y = hit.Y - allowedHeight, Height = hit.Height + allowedHeight }).Intersects(end.Hitbox))
				return true;
		}

		return false;

		Rectangle Hitbox() => new((int)position.X, (int)position.Y, start.width, start.height);
	}

	/// <summary> Based on <see cref="Collision.TileCollision"/> but ignores slopes completely. </summary>
	public static Vector2 NoSlopeCollision(Vector2 position, Vector2 velocity, int width, int height, bool fallThrough = false)
	{
		Collision.up = false;
		Collision.down = false;

		Vector2 result = velocity;

		int topLeft = (int)(position.X / 16f) - 1;
		int topRight = (int)((position.X + width) / 16f) + 2;
		int bottomLeft = (int)(position.Y / 16f) - 1;
		int bottomRight = (int)((position.Y + height) / 16f) + 2;

		int num1 = -1;
		int num2 = -1;
		int num3 = -1;
		int num4 = -1;

		topLeft = Utils.Clamp(topLeft, 0, Main.maxTilesX - 1);
		topRight = Utils.Clamp(topRight, 0, Main.maxTilesX - 1);
		bottomLeft = Utils.Clamp(bottomLeft, 0, Main.maxTilesY - 1);
		bottomRight = Utils.Clamp(bottomRight, 0, Main.maxTilesY - 1);

		float num5 = (bottomRight + 3) * 16;

		for (int i = topLeft; i < topRight; i++)
		{
			for (int j = bottomLeft; j < bottomRight; j++)
			{
				var t = Main.tile[i, j];

				if (!WorldGen.SolidTile(i, j) && !Main.tileSolidTop[t.TileType] && !t.IsHalfBlock)
					continue;

				Vector2 world = new Vector2(i, j) * 16;
				int num6 = 16;

				if (t.IsHalfBlock)
				{
					world.Y += 8f;
					num6 -= 8;
				}

				Vector2 displace = position + velocity;

				if (!(displace.X + width > world.X) || !(displace.X < world.X + 16f) || !(displace.Y + height > world.Y) || !(displace.Y < world.Y + num6))
					continue;

				bool flag = false;
				bool flag2 = false;

				if (flag2)
					continue;

				if (position.Y + height <= world.Y) //Move down
				{
					Collision.down = true;
					if ((!(Main.tileSolidTop[t.TileType] && fallThrough) || !(velocity.Y <= 1f)) && num5 > world.Y)
					{
						num3 = i;
						num4 = j;

						if (num6 < 16)
							num4++;

						if (num3 != num1 && !flag)
						{
							result.Y = world.Y - (position.Y + height);
							num5 = world.Y;
						}
					}
				}
				else if (position.X + width <= world.X && !Main.tileSolidTop[t.TileType]) //Move right
				{
					num1 = i;
					num2 = j;

					if (num2 != num4)
						result.X = world.X - (position.X + width);

					if (num3 == num1)
						result.Y = velocity.Y;
				}
				else if (position.X >= world.X + 16f && !Main.tileSolidTop[t.TileType]) //Move left
				{
					num1 = i;
					num2 = j;

					if (num2 != num4)
						result.X = world.X + 16f - position.X;

					if (num3 == num1)
						result.Y = velocity.Y;
				}
				else if (position.Y >= world.Y + num6 && !Main.tileSolidTop[t.TileType]) //Move up
				{
					Collision.up = true;
					num3 = i;
					num4 = j;
					result.Y = world.Y + num6 - position.Y;

					if (num4 == num2)
						result.X = velocity.X;
				}
			}
		}

		return result;
	}
}