namespace SpiritReforged.Common.Misc;

internal class TimeUtils : ModSystem
{
	public static event Action JustTurnedDay;

	private bool _wasDayTime;

	public override void PostUpdateEverything()
	{
		if (Main.dayTime && !_wasDayTime)
			JustTurnedDay?.Invoke();

		_wasDayTime = Main.dayTime;
	}
}
