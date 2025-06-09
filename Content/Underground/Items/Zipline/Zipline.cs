using SpiritReforged.Common.PlayerCommon;

namespace SpiritReforged.Content.Underground.Items.Zipline;

internal class Zipline(int owner)
{
	public const float MaxAngle = MathHelper.PiOver4;

	public readonly List<Vector2> points = [];
	private readonly int owner = owner;

	public Player Owner => Main.player[owner];
	public bool DrawingLine => points.Count == 2;

	/// <returns> Returns the angle of the zipline in radians. </returns>
	public float Angle()
	{
		if (!DrawingLine)
			return 0;

		GetRange(out var start, out var end);
		return start.AngleTo(end);
	}

	/// <returns> Returns the angle of the zipline to <paramref name="to"/> in radians. </returns>
	public float Angle(Vector2 to)
	{
		if (points.Count == 0)
			return 0;

		Vector2 start, end;
		var last = points[^1];

		if (to.X < last.X)
		{
			start = to;
			end = last;
		}
		else
		{
			start = last;
			end = to;
		}

		return start.AngleTo(end);
	}

	public bool Contains(Point point, out Vector2 contained)
	{
		const int size = 16;

		foreach (var p in points)
		{
			if (new Rectangle((int)(p.X - size / 2), (int)(p.Y - size / 2), size, size).Contains(point))
			{
				contained = p;
				return true;
			}
		}

		contained = Vector2.Zero;
		return false;
	}

	/// <summary> Removes <paramref name="point"/> from this zipline and destroys it if no points remain. </summary>
	/// <param name="point"> The point to remove. </param>
	public void RemovePoint(Vector2 point)
	{
		points.Remove(point);

		if (points.Count == 0)
			ZiplineHandler.Ziplines.Remove(this);
	}

	public void Draw(SpriteBatch spriteBatch)
	{
		var texture = ZiplineHandler.HookTexture.Value;

		for (int i = 0; i < points.Count; i++)
		{
			var p = points[i];

			if (i + 1 < points.Count)
				DrawLine(spriteBatch, p, points[i + 1]);

			var lightColor = Lighting.GetColor(p.ToTileCoordinates());
			spriteBatch.Draw(texture, p - Main.screenPosition, null, lightColor, 0, texture.Size() / 2, 1, default, 0);
		}

		static void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end)
		{
			var texture = ZiplineHandler.WireTexture.Value;
			int length = (int)(start.Distance(end) / texture.Width) + 2;

			for (int i = 0; i < length; i++)
			{
				var position = Vector2.Lerp(start, end, i / (length - 1f));
				float rotation = start.AngleTo(end);
				var lightColor = Lighting.GetColor(position.ToTileCoordinates());

				spriteBatch.Draw(texture, position - Main.screenPosition, null, lightColor, rotation, texture.Size() / 2, 1, default, 0);
			}
		}
	}

	/// <summary> Checks whether <paramref name="player"/> is colliding with this zipline, and if so, calls <see cref="UpdatePlayer"/>. </summary>
	public bool OnZipline(Player player)
	{
		const int width = 6; //Collision line width

		if (!DrawingLine || player.mount.Active)
			return false;

		GetRange(out var start, out var end);

		float collisionPoint = 0;
		var lowRect = new Rectangle((int)player.position.X, (int)player.position.Y + player.height / 2, player.width, player.height / 2);

		if (player.velocity.Y >= 0 && Collision.CheckAABBvLineCollision(lowRect.TopLeft(), lowRect.Size(), start, end, width, ref collisionPoint) && !player.FallThrough())
		{
			float angle = start.AngleTo(end);
			var delta = GetDelta(collisionPoint);

			if (Math.Abs(angle) > MaxAngle)
				return false;

			UpdatePlayer(player, delta, angle);
			player.GetModPlayer<ZiplinePlayer>().DoEffects(delta, GetDelta(collisionPoint - player.velocity.X * 5f));

			return true;
		}

		return false;

		Vector2 GetDelta(float progress) => Vector2.Lerp(start, end, progress / start.Distance(end));
	}

	/// <summary> Controls <paramref name="player"/> velocity and position to imitate a solid surface. </summary>
	/// <param name="player"> The player. </param>
	/// <param name="delta"> The coordinates of the rail in contact. </param>
	/// <param name="rotation"> The rotation of the player. </param>
	private static void UpdatePlayer(Player player, Vector2 delta, float rotation)
	{
		float velocityOffY = player.velocity.X * (rotation / MathHelper.PiOver4);

		player.position = new Vector2(player.position.X, delta.Y - player.height + velocityOffY);
		player.velocity.Y = 0;

		player.gfxOffY = 0;
		player.Rotate(rotation, new Vector2(0, player.height));
	}

	/// <summary> Orders two <see cref="points"/> by X (ascending, <paramref name="start"/> to <paramref name="end"/>). </summary>
	private void GetRange(out Vector2 start, out Vector2 end)
	{
		if (points[0].X < points[1].X)
		{
			start = points[0];
			end = points[1];
		}
		else
		{
			start = points[1];
			end = points[0];
		}
	}
}
