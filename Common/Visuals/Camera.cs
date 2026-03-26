using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using Terraria.Graphics.CameraModifiers;
using static SpiritReforged.Common.Misc.AnimationSequence;

namespace SpiritReforged.Common.Visuals;

public class SequenceCameraModifier(AnimationSequence animation) : ICameraModifier
{
	public class ReturnSegment(int duration, EaseFunction ease) : IAnimationTask
	{
		public int Duration { get; } = duration;
		public readonly EaseFunction ease = ease;

		public void Update(AnimationSequence orchestrator) { }
	}

	private readonly AnimationSequence _orchestrator = animation;

	public string UniqueIdentity => "Spirit Reforged Sequence Camera";

	public bool Finished { get; private set; }

	public void Update(ref CameraInfo cameraInfo)
	{
		_orchestrator.Update(out bool inactive);

		if (inactive)
		{
			Finished = true;
		}
		else
		{
			if (_orchestrator.Task is ReturnSegment returnSegment) //Special interaction
				cameraInfo.CameraPosition = Vector2.Lerp(_orchestrator.position, cameraInfo.OriginalCameraPosition, returnSegment.ease.Ease(_orchestrator.animationTime / (float)returnSegment.Duration));
			else
				cameraInfo.CameraPosition = _orchestrator.position;
		}
	}
}