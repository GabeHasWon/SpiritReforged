using SpiritReforged.Common.TileCommon;

namespace SpiritReforged.Common.WorldGeneration;

public readonly struct Decorator(Rectangle bounds)
{
	private readonly record struct ObjectInfo(WorldMethods.GenDelegate Condition, int Count)
	{
		public readonly bool NoCount => Count == 0;
	}

	public readonly Rectangle bounds = bounds;
	private readonly HashSet<ObjectInfo> _objects = [];

	/// <summary> Queues a task to randomly run within <see cref="bounds"/>. </summary>
	/// <param name="tileType"> The tile type to place. </param>
	/// <param name="count"> The number of items to generate. </param>
	public readonly Decorator Enqueue(int tileType, int count, int style = -1)
	{
		_objects.Add(new((i, j) => Placer.Check(i, j, tileType, style).IsClear().Place().success, count));
		return this;
	}

	/// <summary> Queues a task to run for each coordinate within <see cref="bounds"/>. </summary>
	/// <param name="tileType"> The tile type to place. </param>
	/// <param name="chance"> The chance for <paramref name="tileType"/> to place. </param>
	public readonly Decorator Enqueue(int tileType, float chance, int style = -1)
	{
		_objects.Add(new((i, j) => WorldGen.genRand.NextFloat() <= chance && Placer.Check(i, j, tileType, style).IsClear().Place().success, 0));
		return this;
	}

	public readonly Decorator Enqueue(WorldMethods.GenDelegate action, int count)
	{
		_objects.Add(new(action, count));
		return this;
	}

	public readonly void Run()
	{
		HashSet<ObjectInfo> objects = _objects;
		WorldMethods.GenerateSquared((i, j) =>
		{
			foreach (ObjectInfo item in objects)
			{
				if (item.NoCount && item.Condition.Invoke(i, j))
					return true;
			}

			return false;
		}, out _, bounds);

		foreach (ObjectInfo item in objects)
		{
			if (!item.NoCount)
				WorldMethods.Generate(item.Condition.Invoke, item.Count, out _, bounds);
		}
	}
}