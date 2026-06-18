using Terraria.Utilities;

namespace SpiritReforged.Common.WorldGeneration.GenConfiguration;

#nullable enable

internal interface IGenRange
{
	string DisplayString();
}

/// <summary>
/// Defines a range for a <see cref="IGenerationPage"/> config (int).
/// </summary>
internal class GenRange(int min, int range) : IGenRange
{
	public static readonly GenRange Empty = new(0, 0);

	public readonly int DefaultMin = min;
	public readonly int DefaultRange = range;

	public int Minimum = min;
	public int Range = range;

	public GenRange() : this(0, 0)
	{
	}

	public int RollRange(bool upperBoundInclusive, UnifiedRandom? random = null) => Minimum + (random ?? WorldGen.genRand).Next(Range + (upperBoundInclusive ? 1 : 0));

	string IGenRange.DisplayString() => $"{Minimum} - {Range}";
}

/// <summary>
/// Defines a range for a <see cref="IGenerationPage"/> config (float).
/// </summary>
internal class GenRangeF(float min, float range) : IGenRange
{
	public static readonly GenRangeF Empty = new(0, 0);

	public readonly float DefaultMin = min;
	public readonly float DefaultRange = range;

	public float Minimum = min;
	public float Range = range;

	public GenRangeF() : this(0, 0)
	{
	}

	public float RollRange(UnifiedRandom? random = null) => Minimum + (random ?? WorldGen.genRand).NextFloat(Range);

	string IGenRange.DisplayString() => $"{Minimum} - {Range}";
}
