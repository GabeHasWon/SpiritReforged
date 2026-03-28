namespace SpiritReforged.Common.Misc;

public class MapAnimator : ModSystem
{
	/// <summary> Controls map animation. </summary>
	private static readonly List<AnimationSequence> Orchestrators = [];

	/// <summary> Registers animation(s) to the fullscreen map which run simultaneously. </summary>
	public static void Register(params AnimationSequence[] values) => Orchestrators.AddRange(values);

	public override void PostUpdateEverything()
	{
		if (Orchestrators.Count == 0)
			return;

		if (Main.mapFullscreen)
		{
			for (int c = Orchestrators.Count - 1; c >= 0; c--)
			{
				AnimationSequence orchestrator = Orchestrators[c];
				orchestrator.Update(out bool inactive);

				if (inactive)
				{
					Orchestrators.Remove(orchestrator);
				}
				else
				{
					if (orchestrator.position != default)
						Main.mapFullscreenPos = orchestrator.position;

					if (orchestrator.scale != default)
						Main.mapFullscreenScale = orchestrator.scale;
				}
			}
		}
		else
		{
			Orchestrators.Clear();
		}
	}
}