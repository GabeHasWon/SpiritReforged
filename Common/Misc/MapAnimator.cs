using SpiritReforged.Common.Easing;

namespace SpiritReforged.Common.Misc;

public class MapAnimator : ModSystem
{
	#region tasks
	public interface IAnimationTask
	{
		int Duration { get; }
		public void Update(Animation orchestrator);
	}

	public class EaseSegment : IAnimationTask
	{
		public EaseSegment(int duration, Vector2 start, Vector2 end, EaseFunction ease)
		{
			Duration = duration;
			_start = start;
			_end = end;
			_ease = ease;
		}

		public EaseSegment(int duration, Vector2 end, EaseFunction ease)
		{
			Duration = duration;
			_start = Main.mapFullscreenPos;
			_end = end;
			_ease = ease;
		}

		public int Duration { get; }

		private readonly Vector2 _start;
		private readonly Vector2 _end;
		private readonly EaseFunction _ease;

		public void Update(Animation orchestrator) => Main.mapFullscreenPos = Vector2.Lerp(_start, _end, _ease.Ease(orchestrator.animationTime / (float)Duration));
	}

	public class ZoomSegment : IAnimationTask
	{
		public ZoomSegment(int duration, float start, float end, EaseFunction ease)
		{
			Duration = duration;
			_start = start;
			_end = end;
			_ease = ease;
		}

		public ZoomSegment(int duration, float end, EaseFunction ease)
		{
			Duration = duration;
			_start = Main.mapFullscreenScale;
			_end = end;
			_ease = ease;
		}

		public int Duration { get; }

		private readonly float _start;
		private readonly float _end;
		private readonly EaseFunction _ease;

		public void Update(Animation orchestrator) => Main.mapFullscreenScale = MathHelper.Lerp(_start, _end, _ease.Ease(orchestrator.animationTime / (float)Duration));
	}
	#endregion

	public class Animation
	{
		private readonly List<IAnimationTask> tasks = [];

		private int _taskIndex;
		public int animationTime;

		public void Update(out bool canBeRemoved)
		{
			canBeRemoved = false;
			IAnimationTask task = tasks[_taskIndex];

			task.Update(this);

			if (++animationTime > task.Duration)
			{
				if (++_taskIndex >= tasks.Count)
					canBeRemoved = true;

				animationTime = 0;
			}
		}

		public Animation Add(IAnimationTask task)
		{
			tasks.Add(task);
			return this;
		}
	}

	/// <summary> Controls map animation. </summary>
	private static readonly List<Animation> Orchestrators = [];

	/// <summary> Registers animation(s) to the fullscreen map which run simultaneously. </summary>
	public static void Register(params Animation[] values) => Orchestrators.AddRange(values);

	public override void PostUpdateEverything()
	{
		if (Orchestrators.Count == 0)
			return;

		if (Main.mapFullscreen)
		{
			for (int c = Orchestrators.Count - 1; c >= 0; c--)
			{
				Animation orchestrator = Orchestrators[c];
				orchestrator.Update(out bool inactive);

				if (inactive)
					Orchestrators.Remove(orchestrator);
			}
		}
		else
		{
			Orchestrators.Clear();
		}
	}
}