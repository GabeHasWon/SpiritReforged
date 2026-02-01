namespace SpiritReforged.Common.PlayerCommon;

/// <summary> Handles double tap abilities in any cardinal direction. </summary>
public class DoubleTapPlayer : ModPlayer
{
	public enum Direction { Up, Right, Down, Left }

	public const int TapThreshold = 17;

	public delegate void DoubleTapDelegate(Player player, Direction direction);
	public static event DoubleTapDelegate OnDoubleTap;

	public Dictionary<Direction, int> Counters = [];
	public Direction lastDirection;

	public static Vector2 ConvertDirection(Direction value) => (-Vector2.UnitY).RotatedBy(MathHelper.PiOver2 * (int)value);

	public override void ResetEffects()
	{
		if (Counters.Count == 0)
		{
			Counters.Add(Direction.Up, TapThreshold);
			Counters.Add(Direction.Right, TapThreshold);
			Counters.Add(Direction.Down, TapThreshold);
			Counters.Add(Direction.Left, TapThreshold);
		}

		foreach (Direction direction in Counters.Keys)
			Counters[direction] = Math.Max(Counters[direction] - 1, 0);
	}

	public override void SetControls()
	{
		if (Player.controlUp && Player.releaseUp)
			ReadInput(Direction.Up);

		if (Player.controlRight && Player.releaseRight)
			ReadInput(Direction.Right);

		if (Player.controlDown && Player.releaseDown)
			ReadInput(Direction.Down);

		if (Player.controlLeft && Player.releaseLeft)
			ReadInput(Direction.Left);

		void ReadInput(Direction direction)
		{
			if (lastDirection == direction && Counters[direction] > 0)
			{
				DoubleTap(direction);
				Counters[direction] = 0;

				return;
			}

			Counters[direction] = TapThreshold;
			lastDirection = direction;
		}
	}

	public void DoubleTap(Direction direction)
	{
		if (Main.ReversedUpDownArmorSetBonuses)
		{
			if (direction == Direction.Up)
				direction = Direction.Down;
			else if (direction == Direction.Down)
				direction = Direction.Up;
		}

		OnDoubleTap?.Invoke(Player, direction);
	}
}