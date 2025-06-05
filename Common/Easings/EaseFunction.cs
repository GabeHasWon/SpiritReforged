using System;
using System.Collections.Generic;

namespace SpiritReforged.Common.Easing;

public abstract class EaseFunction
{
	public static readonly EaseFunction Linear = new PolynomialEase((float x) => x);

	public static readonly EaseFunction EaseQuadIn = new PolynomialEase((float x) => x * x);
	public static readonly EaseFunction EaseQuadOut = new PolynomialEase((float x) => 1f - EaseQuadIn.Ease(1f - x));
	public static readonly EaseFunction EaseQuadInOut = new PolynomialEase((float x) => (x < 0.5f) ? 2f * x * x : -2f * x * x + 4f * x - 1f);

	public static readonly EaseFunction EaseCubicIn = new PolynomialEase((float x) => x * x * x);
	public static readonly EaseFunction EaseCubicOut = new PolynomialEase((float x) => 1f - EaseCubicIn.Ease(1f - x));
	public static readonly EaseFunction EaseCubicInOut = new PolynomialEase((float x) => (x < 0.5f) ? 4f * x * x * x : 4f * x * x * x - 12f * x * x + 12f * x - 3f);

	public static readonly EaseFunction EaseQuarticIn = new PolynomialEase((float x) => x * x * x * x);
	public static readonly EaseFunction EaseQuarticOut = new PolynomialEase((float x) => 1f - EaseQuarticIn.Ease(1f - x));
	public static readonly EaseFunction EaseQuarticInOut = new PolynomialEase((float x) => (x < 0.5f) ? 8f * x * x * x * x : -8f * x * x * x * x + 32f * x * x * x - 48f * x * x + 32f * x - 7f);

	public static readonly EaseFunction EaseQuinticIn = new PolynomialEase((float x) => x * x * x * x * x);
	public static readonly EaseFunction EaseQuinticOut = new PolynomialEase((float x) => 1f - EaseQuinticIn.Ease(1f - x));
	public static readonly EaseFunction EaseQuinticInOut = new PolynomialEase((float x) => (x < 0.5f) ? 16f * x * x * x * x * x : 16f * x * x * x * x * x - 80f * x * x * x * x + 160f * x * x * x - 160f * x * x + 80f * x - 15f);

	public static readonly EaseFunction EaseCircularIn = new PolynomialEase((float x) => 1f - (float)Math.Sqrt(1.0 - Math.Pow(x, 2)));
	public static readonly EaseFunction EaseCircularOut = new PolynomialEase((float x) => (float)Math.Sqrt(1.0 - Math.Pow(x - 1.0, 2)));
	public static readonly EaseFunction EaseCircularInOut = new PolynomialEase((float x) => (x < 0.5f) ? (1f - (float)Math.Sqrt(1.0 - Math.Pow(x * 2, 2))) * 0.5f : (float)((Math.Sqrt(1.0 - Math.Pow(-2 * x + 2, 2)) + 1) * 0.5));

	public static readonly EaseFunction EaseSine = new PolynomialEase((float x) => (float)Math.Sin(x * MathHelper.Pi));

	public static EaseFunction EaseOutBack(double maxValue = 1.70158)
	{
		double c1 = maxValue;
		double c3 = c1 + 1;

		return new PolynomialEase((float x) => (float)(1 + c3 * Math.Pow(x - 1, 3) + c1 * Math.Pow(x - 1, 2)));
	}

	public static EaseFunction EaseOutElastic(double maxValue = 2.09439) => new PolynomialEase((float x) => x switch
	{
		0 => 0,
		1 => 1,
		_ => (float)(Math.Pow(2, -10 * x) * Math.Sin((x * 10 - 0.75) * maxValue) + 1)
	});

	/// <summary>
	/// Returns the result of multiple ease functions applied to one input, as its own singular function.
	/// </summary>
	/// <param name="inputs"></param>
	/// <returns></returns>
	public static EaseFunction CompoundEase(EaseFunction[] inputs)
	{
		float func(float x)
		{
			for (int i = 0; i < inputs.Length; i++)
				x = inputs[i].Ease(x);

			return x;
		}

		return new PolynomialEase(func);
	}

	/// <summary>
	/// Returns the result of two different ease functions being stitched together, as its own singular function. <br />
	/// "cutOff" refers to the point at which the function switches from the first to the second function, as a percentage (ie 0.5f refers to 50% through)
	/// </summary>
	/// <param name="easeStart"></param>
	/// <param name="easeEnd"></param>
	/// <param name="cutOff"></param>
	/// <returns></returns>
	public static EaseFunction MultistepEase(EaseFunction easeStart, EaseFunction easeEnd, float cutOff = 0.5f)
	{
		float func(float x)
		{
			float inverseCutoff = 1 - cutOff;
			float easeStartRate = 1 / cutOff;
			float easeEndRate = 1 / inverseCutoff;
			if (x < cutOff)
				return easeStart.Ease(easeStartRate * x) * cutOff;
			else
				return cutOff + easeEnd.Ease(easeEndRate * (x - cutOff)) * inverseCutoff;
		}

		return new PolynomialEase(func);
	}

	/// <summary>
	/// Returns the result of two different ease functions being smoothly combined, as its own singular function. <br />
	/// Interpolates from the first function to the second as the eased value increases, with an optional parameter to affect how this function's interpolation is eased.
	/// </summary>
	/// <param name="easeStart"></param>
	/// <param name="easeEnd"></param>
	/// <param name="interpolationEase"></param>
	/// <returns></returns>
	public static EaseFunction MultistepEaseLerp(EaseFunction easeStart, EaseFunction easeEnd, EaseFunction interpolationEase = null)
	{
		interpolationEase ??= Linear;
		float func(float x) => MathHelper.Lerp(easeStart.Ease(x), easeEnd.Ease(x), interpolationEase.Ease(x));

		return new PolynomialEase(func);
	}

	public abstract float Ease(float time);
}

public class PolynomialEase(Func<float, float> func) : EaseFunction
{
	private readonly Func<float, float> _function = func;

	public override float Ease(float time) => _function(time);
}

public class EaseBuilder : EaseFunction
{
	private readonly List<EasePoint> _points;

	public EaseBuilder() => _points = [];

	public void AddPoint(float x, float y, EaseFunction function) => AddPoint(new Vector2(x, y), function);

	public void AddPoint(Vector2 vector, EaseFunction function)
	{
		if (vector.X < 0f) 
			throw new ArgumentException("X value of point is not in valid range!");

		var newPoint = new EasePoint(vector, function);

		if (_points.Count == 0) 
		{
			_points.Add(newPoint);
			return;
		}

		EasePoint last = _points[^1];

		if (last.Point.X > vector.X) 
			throw new ArgumentException("New point has an x value less than the previous point when it should be greater or equal");

		_points.Add(newPoint);
	}

	public override float Ease(float time)
	{
		Vector2 prevPoint = Vector2.Zero;
		EasePoint usePoint = _points[0];

		for (int i = 0; i < _points.Count; i++)
		{
			usePoint = _points[i];

			if (time <= usePoint.Point.X)
				break;

			prevPoint = usePoint.Point;
		}

		float dist = usePoint.Point.X - prevPoint.X;
		float progress = (time - prevPoint.X) / dist;

		if (progress > 1f) 
			progress = 1f;
		
		return MathHelper.Lerp(prevPoint.Y, usePoint.Point.Y, usePoint.Function.Ease(progress));
	}

	private struct EasePoint(Vector2 p, EaseFunction func)
	{
		public Vector2 Point = p;
		public EaseFunction Function = func;
	}
}
