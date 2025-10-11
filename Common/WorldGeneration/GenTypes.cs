using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration;

public static class GenTypes
{
	public class ReplaceType(ushort type) : GenAction
	{
		private readonly ushort _type = type;

		public override bool Apply(Point origin, int x, int y, params object[] args)
		{
			_tiles[x, y].TileType = _type;
			return UnitApply(origin, x, y, args);
		}
	}

	public class Send() : GenAction
	{
		public override bool Apply(Point origin, int x, int y, params object[] args)
		{
			if (Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendTileSquare(-1, x, y);

			return UnitApply(origin, x, y, args);
		}
	}

	/// <summary> Modification of <see cref="Modifiers.IsTouchingAir"/> that ignores non-solids, but not slopes. </summary>
	public class SolidIsTouchingAir(bool useDiagonals = false) : GenAction
	{
		private static readonly int[] DIRECTIONS = [0, -1, 1, 0, -1, 0, 0, 1, -1, -1, 1, -1, -1, 1, 1, 1];
		private readonly bool _useDiagonals = useDiagonals;

		public override bool Apply(Point origin, int x, int y, params object[] args)
		{
			int num = _useDiagonals ? 16 : 8;
			for (int i = 0; i < num; i += 2)
			{
				if (!WorldGen.SolidOrSlopedTile(x + DIRECTIONS[i], y + DIRECTIONS[i + 1]))
					return UnitApply(origin, x, y, args);
			}

			return Fail();
		}
	}

	public class Curve(int width, int height) : GenShape
	{
		private readonly int _width = width;
		private readonly int _height = height;

		public override bool Perform(Point origin, GenAction action)
		{
			for (int i = origin.X; i < origin.X + _width; i++)
			{
				float progress = (float)(i - origin.X) / (_width - 1);
				int curveShape = (int)(Math.Sin(progress * MathHelper.Pi) * _height);

				for (int j = origin.Y - curveShape; j <= origin.Y; j++)
				{
					if (!UnitApply(action, origin, i, j) && _quitOnFail)
						return false;
				}
			}

			return true;
		}
	}

	public class Splatter(int radius, int samples, int distance) : GenShape
	{
		private readonly int _radius = radius;
		private readonly int _samples = samples;
		private readonly int _distance = distance;

		public override bool Perform(Point origin, GenAction action)
		{
			for (int a = 0; a < _samples; a++)
			{
				float strength = _random.NextFloat();
				int finalRadius = (int)(_radius * strength);

				if (finalRadius < 2)
					continue;

				var location = origin + (_random.NextVector2Unit() * (_distance * (1f - strength))).ToPoint();

				if (!GenCircle(location, new(finalRadius, finalRadius), action))
					return false;
			}

			return true;
		}

		private bool GenCircle(Point origin, Point radius, GenAction action)
		{
			int xRadius = radius.X;
			int yRadius = radius.Y;
			int num = (xRadius + 1) * (xRadius + 1);

			for (int i = origin.Y - yRadius; i <= origin.Y + yRadius; i++)
			{
				double num2 = xRadius / (double)yRadius * (i - origin.Y);
				int num3 = Math.Min(xRadius, (int)Math.Sqrt(num - num2 * num2));

				for (int j = origin.X - num3; j <= origin.X + num3; j++)
				{
					if (!UnitApply(action, origin, j, i) && _quitOnFail)
						return false;
				}
			}

			return true;
		}
	}
}