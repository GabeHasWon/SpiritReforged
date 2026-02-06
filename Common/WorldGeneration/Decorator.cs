using SpiritReforged.Common.TileCommon;

namespace SpiritReforged.Common.WorldGeneration;

public readonly struct Decorator(Rectangle bounds, WorldMethods.GenDelegate commonDelegate = null)
{
	public readonly record struct GenInfo(Func<int> Style);
	private readonly record struct ObjectInfo(WorldMethods.GenDelegate Condition, int Count)
	{
		public readonly bool NoCount => Count == 0;
	}

	public readonly Rectangle bounds = bounds;
	private readonly WorldMethods.GenDelegate _commonDelegate = commonDelegate;
	private readonly List<ObjectInfo> _objects = [];

	/// <summary> Queues a task to randomly run within <see cref="bounds"/>. </summary>
	/// <param name="tileType"> The tile type to place. </param>
	/// <param name="count"> The number of items to generate. </param>
	public readonly Decorator Enqueue(int tileType, int count, GenInfo info = default)
	{
		_objects.Add(new((i, j) => Placer.Check(i, j, tileType, info.Style?.Invoke() ?? -1).IsClear().Place().success, count));
		return this;
	}

	/// <summary> Queues a task to run for each coordinate within <see cref="bounds"/>. </summary>
	/// <param name="tileType"> The tile type to place. </param>
	/// <param name="chance"> The chance for <paramref name="tileType"/> to place. </param>
	public readonly Decorator Enqueue(int tileType, float chance, GenInfo info = default)
	{
		_objects.Add(new((i, j) => WorldGen.genRand.NextFloat() <= chance && Placer.Check(i, j, tileType, info.Style?.Invoke() ?? -1).IsClear().Place().success, 0));
		return this;
	}

	public readonly Decorator Enqueue(WorldMethods.GenDelegate action, int count)
	{
		_objects.Add(new(action, count));
		return this;
	}

	public readonly void Run()
	{
		List<ObjectInfo> objects = _objects;
		WorldMethods.GenDelegate del = _commonDelegate;

		WorldMethods.GenerateSquared((i, j) =>
		{
			foreach (ObjectInfo item in objects)
			{
				if (item.NoCount && del?.Invoke(i, j) != false && item.Condition.Invoke(i, j))
					return true;
			}

			return false;
		}, out _, bounds);

		foreach (ObjectInfo item in objects)
		{
			if (!item.NoCount)
				WorldMethods.Generate((i, j) => del?.Invoke(i, j) != false && item.Condition.Invoke(i, j), item.Count, out _, bounds);
		}
	}
}