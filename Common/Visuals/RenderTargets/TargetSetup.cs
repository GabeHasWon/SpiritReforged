namespace SpiritReforged.Common.Visuals.RenderTargets;

internal class TargetSetup : ILoadable
{
	/// <summary> Called with <see cref="On_Main.CheckMonoliths"/>. </summary>
	public static event Action DrawIntoRendertargets;

	public void Load(Mod mod)
	{
		On_Main.CheckMonoliths += static (orig) =>
		{
			orig();

			if (!Main.gameMenu && !Main.dedServ)
				DrawIntoRendertargets?.Invoke();
		};

		DrawIntoRendertargets += DrawIntoTargets;
	}
	public void Unload() { }

	private static void DrawIntoTargets()
	{
		foreach (var target in ModTarget2D.Targets)
		{
			if (target.Active)
				target.Prepare(Main.spriteBatch);
		}
	}
}