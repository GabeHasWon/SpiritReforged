using Terraria.Utilities;

namespace SpiritReforged.Common.WorldGeneration.GenConfiguration;

#nullable enable

internal interface IGenRange
{
	IGenRange Default { get; }

	string DisplayString();
}

/// <summary>
/// Defines a range for a <see cref="IGenerationPage"/> config (int).
/// </summary>
internal class GenRange(int min, int range, bool upperBoundInclusive = true) : IGenRange
{
	public static readonly GenRange Empty = new(0, 0);

	IGenRange IGenRange.Default => new GenRange(DefaultMin, DefaultRange);

	public readonly int DefaultMin = min;
	public readonly int DefaultRange = range;
	public readonly bool UpperBoundInclusive = upperBoundInclusive;

	public int Minimum = min;
	public int Range = range;

	public GenRange() : this(0, 0)
	{
	}

	public int RollRange(UnifiedRandom? random = null) => Minimum + (random ?? WorldGen.genRand).Next(Range + (UpperBoundInclusive ? 1 : 0));

	string IGenRange.DisplayString() => $"{Minimum} - {Minimum + Range}";
	public override string ToString() => ((IGenRange)this).DisplayString();

	public override bool Equals(object? obj)
	{
		if (obj is not GenRange other)
			return false;

		return Minimum == other.Minimum && Range == other.Range;
	}

	public override int GetHashCode() => Minimum.GetHashCode() ^ Range.GetHashCode();

	public static bool operator ==(GenRange operand, GenRange other) => operand.Minimum == other.Minimum && operand.Range == other.Range;
	public static bool operator !=(GenRange operand, GenRange other) => operand.Minimum != other.Minimum || operand.Range != other.Range;
}

/// <summary>
/// Defines a range for a <see cref="IGenerationPage"/> config (float).
/// </summary>
internal class GenRangeF(float min, float range) : IGenRange
{
	public static readonly GenRangeF Empty = new(0, 0);

	IGenRange IGenRange.Default => new GenRangeF(DefaultMin, DefaultRange);

	public readonly float DefaultMin = min;
	public readonly float DefaultRange = range;

	public float Minimum = min;
	public float Range = range;

	public GenRangeF() : this(0, 0)
	{
	}

	public float RollRange(UnifiedRandom? random = null) => Minimum + (random ?? WorldGen.genRand).NextFloat(Range);

	string IGenRange.DisplayString() => $"{Minimum} - {Minimum + Range}";
	public override string ToString() => ((IGenRange)this).DisplayString();

	public override bool Equals(object? obj)
	{
		if (obj is not GenRangeF other)
			return false;

		return Minimum == other.Minimum && Range == other.Range;
	}

	public override int GetHashCode() => Minimum.GetHashCode() ^ Range.GetHashCode();

	public static bool operator ==(GenRangeF operand, GenRangeF other) => operand.Minimum == other.Minimum && operand.Range == other.Range;
	public static bool operator !=(GenRangeF operand, GenRangeF other) => operand.Minimum != other.Minimum || operand.Range != other.Range;
}
