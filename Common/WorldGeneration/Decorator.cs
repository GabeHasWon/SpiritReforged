using SpiritReforged.Common.TileCommon;

namespace SpiritReforged.Common.WorldGeneration;

public readonly struct Decorator(Rectangle bounds, WorldMethods.GenDelegate commonDelegate = null)
{
	public readonly record struct GenInfo(Func<int> Style);
	private readonly record struct ObjectInfo(WorldMethods.GenDelegate Condition, int Count);

	public readonly int LastIndex => _objects.Count - 1;

	public readonly Rectangle bounds = bounds;
	private readonly WorldMethods.GenDelegate _commonDelegate = commonDelegate;
	private readonly List<ObjectInfo> _objects = [];

	/// <summary> Queues a task to randomly run within <see cref="bounds"/>. </summary>
	/// <param name="tileType"> The tile type to place. </param>
	/// <param name="count"> The number of items to generate. </param>
	public readonly Decorator Enqueue(int tileType, int count, GenInfo info = default)
	{
		_objects.Add(new(DefaultCondition, count));
		return this;

		bool DefaultCondition(int x, int y) => Placer.Check(x, y, tileType, info.Style?.Invoke() ?? -1).IsClear().Place().success;
	}

	/// <summary> Queues a task to run for each coordinate within <see cref="bounds"/>. </summary>
	/// <param name="tileType"> The tile type to place. </param>
	/// <param name="chance"> The chance for <paramref name="tileType"/> to place. </param>
	public readonly Decorator Enqueue(int tileType, float chance, GenInfo info = default)
	{
		_objects.Add(new(DefaultCondition, 0));
		return this;

		bool DefaultCondition(int x, int y) => WorldGen.genRand.NextFloat() <= chance && Placer.Check(x, y, tileType, info.Style?.Invoke() ?? -1).IsClear().Place().success;
	}

	public readonly Decorator Enqueue(WorldMethods.GenDelegate action, int count)
	{
		_objects.Add(new(action, count));
		return this;
	}

	public readonly void Run(out int[] objectCounts)
	{
		WorldMethods.GenDelegate del = _commonDelegate;
		objectCounts = new int[_objects.Count];

		for (int index = 0; index < _objects.Count; index++)
		{
			ObjectInfo item = _objects[index];
			if (item.Count == 0)
				WorldMethods.GenerateSquared((i, j) => del?.Invoke(i, j) != false && item.Condition.Invoke(i, j), out objectCounts[index], bounds);
			else
				WorldMethods.Generate((i, j) => del?.Invoke(i, j) != false && item.Condition.Invoke(i, j), item.Count, out objectCounts[index], bounds);
		}
	}
}