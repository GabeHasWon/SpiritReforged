using Terraria.Utilities;

namespace SpiritReforged.Common.Misc;

internal static class MiscExtensions
{
	public static Vector2 NextVec2CircularEven(this UnifiedRandom rand, float halfWidth, float halfHeight)
	{
		double x = rand.NextDouble();
		double y = rand.NextDouble();

		if (x + y > 1)
		{
			x = 1 - x;
			y = 1 - y;
		}

		double s = 1 / (x + y);

		if (double.IsNaN(s))
			return Vector2.Zero;

		s *= s;
		s = Math.Sqrt(x * x * s + y * y * s);
		s = 1 / s;
		x *= s;
		y *= s;

		double angle = rand.NextDouble() * (2 * Math.PI);
		double cos = Math.Cos(angle);
		double sin = Math.Sin(angle);

		return new Vector2((float)(x * cos - y * sin) * halfWidth, (float)(x * sin + y * cos) * halfHeight);
	}

	public static Vector2 TurnRight(this Vector2 vec) => new(-vec.Y, vec.X);

	public static Vector2 TurnLeft(this Vector2 vec) => new(vec.Y, -vec.X);

	/// <summary>
	/// Ends & restarts the spriteBatch with default vanilla parameters.
	/// </summary>
	public static void RestartToDefault(this SpriteBatch batch)
	{
		batch.End();
		batch.BeginDefault();
	}

	/// <summary>
	/// Begins the spriteBatch with default vanilla parameters.
	/// </summary>
	/// <param name="batch"></param>
	public static void BeginDefault(this SpriteBatch batch) => batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

	public static Color Additive(this Color color, byte newAlpha = 0)
	{
		var temp = color;
		temp.A = (byte)(temp.A * newAlpha / byte.MaxValue);
		return temp;
	}

	/// <summary>
	/// Tries to move an entity to a given spot, using linear acceleration, and multiplicative inverse deceleration.
	/// </summary>
	/// <param name="ent"></param>
	/// <param name="desiredPosition"></param>
	/// <param name="accelSpeed"></param>
	/// <param name="deccelSpeed"></param>
	/// <param name="maxSpeed"></param>
	public static void AccelFlyingMovement(this Entity ent, Vector2 desiredPosition, Vector2 accelSpeed, Vector2 deccelSpeed, Vector2? maxSpeed = null)
	{
		if (ent.Center.X != desiredPosition.X)
			ent.velocity.X = (ent.Center.X < desiredPosition.X)
				? ((ent.velocity.X < 0) ? ent.velocity.X / (deccelSpeed.X + 1) : ent.velocity.X) + accelSpeed.X
				: ((ent.velocity.X > 0) ? ent.velocity.X / (deccelSpeed.X + 1) : ent.velocity.X) - accelSpeed.X;

		if (ent.Center.Y != desiredPosition.Y)
			ent.velocity.Y = (ent.Center.Y < desiredPosition.Y)
				? ((ent.velocity.Y < 0) ? ent.velocity.Y / (deccelSpeed.Y + 1) : ent.velocity.Y) + accelSpeed.Y
				: ((ent.velocity.Y > 0) ? ent.velocity.Y / (deccelSpeed.Y + 1) : ent.velocity.Y) - accelSpeed.Y;

		if (maxSpeed != null)
			ent.velocity = new Vector2(MathHelper.Clamp(ent.velocity.X, -maxSpeed.Value.X, maxSpeed.Value.X), MathHelper.Clamp(ent.velocity.Y, -maxSpeed.Value.Y, maxSpeed.Value.Y));
	}

	public static void AccelFlyingMovement(this Entity ent, Vector2 desiredPosition, float accelSpeed, float deccelSpeed, float maxSpeed = -1) => AccelFlyingMovement(ent, desiredPosition, new Vector2(accelSpeed), new Vector2(deccelSpeed), (maxSpeed >= 0) ? new Vector2(maxSpeed) : null);

	/// <summary> Adds <paramref name="options"/> and distributes <paramref name="weight"/> evenly. </summary>
	public static void AddRange<T>(this WeightedRandom<T> r, double weight = 1, params T[] options)
	{
		double finalWeight = weight / options.Length;

		foreach (var type in options)
			r.Add(type, finalWeight);
	}

	/// <summary> Whether common NPC spawn conditions related to invasions and events are satisfied. </summary>
	public static bool Common(this NPCSpawnInfo info) => !info.Invasion && (!Main.bloodMoon && !Main.pumpkinMoon && !Main.snowMoon || Main.dayTime) && (!Main.eclipse || !Main.dayTime) && !info.Player.ZoneOldOneArmy;
}