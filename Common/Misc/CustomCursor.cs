namespace SpiritReforged.Common.Misc;

public class CustomCursor : ILoadable
{
	/// <summary> 'bool' is whether the cursor outline (thick cursor) is being drawn. This will be invoked twice so make sure to be explicit. </summary>
	public static event Action<bool> DrawCustomCursor;
	/// <summary> Whether the default cursor should not be drawn. </summary>
	internal static bool HideCursor { get; set; }

	public void Load(Mod mod)
	{
		On_Main.DrawCursor += DrawCursor;
		On_Main.DrawThickCursor += DrawThickCursor;
	}

	public void Unload() => DrawCustomCursor = null;

	private Vector2 DrawThickCursor(On_Main.orig_DrawThickCursor orig, bool smart)
	{
		DrawCustomCursor?.Invoke(true);
		return HideCursor ? Vector2.Zero : orig(smart); //Skips orig
	}

	private void DrawCursor(On_Main.orig_DrawCursor orig, Vector2 bonus, bool smart)
	{
		DrawCustomCursor?.Invoke(false);

		if (!HideCursor)
			orig(bonus, smart); //Skips orig
	}
}
