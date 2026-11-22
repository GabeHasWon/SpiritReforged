using SpiritReforged.Common.Easing;

namespace SpiritReforged.Common.Misc;

public class AnimationSequence
{
	#region tasks
	public interface IAnimationTask
	{
		int Duration { get; }
		public void Update(AnimationSequence orchestrator);
	}

	/// <summary> Eases from location to location. </summary>
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
			_end = end;
			_ease = ease;
		}

		public int Duration { get; }

		private Vector2 _start;
		private readonly Vector2 _end;
		private readonly EaseFunction _ease;

		public void Update(AnimationSequence orchestrator)
		{
			if (_start == default)
				_start = orchestrator.position;

			orchestrator.position = Vector2.Lerp(_start, _end, _ease.Ease(orchestrator.animationTime / (float)Duration));
		}
	}

	/// <summary> Follows an entities position /// </summary>
	public class FollowSegment : IAnimationTask
	{
		public FollowSegment(int duration, Entity Parent)
		{
			Duration = duration;
			parent = Parent;
			position = parent.Center;
		}
		public int Duration { get; }

		private Vector2 position;
		private Entity parent;
		public void Update(AnimationSequence orchestrator)
		{
			if (position == default)
				position = orchestrator.position;
			else
				position = parent.Center;

			orchestrator.position = Vector2.Lerp(orchestrator.position, position - Main.ScreenSize.ToVector2() / 2, 0.05f);
		}
	}

	/// <summary> Zooms from value to value. </summary>
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
			_end = end;
			_ease = ease;
		}

		public int Duration { get; }

		private float _start;
		private readonly float _end;
		private readonly EaseFunction _ease;

		public void Update(AnimationSequence orchestrator)
		{
			if (_start == default)
				_start = orchestrator.scale;

			orchestrator.scale = MathHelper.Lerp(_start, _end, _ease.Ease(orchestrator.animationTime / (float)Duration));
		}
	}

	public class WaitSegment(int duration) : IAnimationTask
	{
		public int Duration { get; } = duration;
		public void Update(AnimationSequence orchestrator) { }
	}
	#endregion

	public IAnimationTask Task => _tasks[_taskIndex];

	private readonly List<IAnimationTask> _tasks = [];

	public int animationTime;

	public Vector2 position; //RENAME
	public float rotation;
	public float scale;

	private int _taskIndex;

	public void Update(out bool canBeRemoved)
	{
		canBeRemoved = false;
		IAnimationTask task = _tasks[_taskIndex];

		task.Update(this);

		if (++animationTime > task.Duration)
		{
			if (++_taskIndex >= _tasks.Count)
			{
				canBeRemoved = true;
				_taskIndex--; //Move the cursor back into bounds
			}

			animationTime = 0;
		}
	}

	public AnimationSequence Add(IAnimationTask task)
	{
		_tasks.Add(task);
		return this;
	}
}