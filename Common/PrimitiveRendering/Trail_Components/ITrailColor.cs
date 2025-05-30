﻿using SpiritReforged.Common.Easing;

namespace SpiritReforged.Common.PrimitiveRendering.Trail_Components;

public interface ITrailColor
{
	Color GetColourAt(float distanceFromStart, float trailLength, List<Vector2> points, Vector2 curPoint);
}

#region Different Trail Color Types
public class GradientTrail(Color start, Color end) : ITrailColor
{
	private Color _startColour = start;
	private Color _endColour = end;
	private EaseFunction _easing = EaseFunction.Linear;

	public GradientTrail(Color start, Color end, EaseFunction easing) : this(start, end) => _easing = easing;

	public Color GetColourAt(float distanceFromStart, float trailLength, List<Vector2> points, Vector2 curPoint)
	{
		float progress = distanceFromStart / trailLength;
		return Color.Lerp(_startColour, _endColour, _easing.Ease(progress)) * (1f - progress);
	}
}

public class RainbowTrail(float animationSpeed = 5f, float distanceMultiplier = 0.01f, float saturation = 1f, float lightness = 0.5f) : ITrailColor
{
	private readonly float _saturation = saturation;
	private readonly float _lightness = lightness;
	private readonly float _speed = animationSpeed;
	private readonly float _distanceMultiplier = distanceMultiplier;

	public Color GetColourAt(float distanceFromStart, float trailLength, List<Vector2> points, Vector2 curPoint)
	{
		float progress = distanceFromStart / trailLength;
		float hue = (Main.GlobalTimeWrappedHourly * _speed + distanceFromStart * _distanceMultiplier) % MathHelper.TwoPi;
		return ColorFromHSL(hue, _saturation, _lightness) * (1f - progress);
	}

	//Borrowed methods for converting HSL to RGB
	private Color ColorFromHSL(float h, float s, float l)
	{
		h /= MathHelper.TwoPi;

		float r = 0, g = 0, b = 0;
		if (l != 0)
			if (s == 0)
				r = g = b = l;
			else
			{
				float temp2;
				if (l < 0.5f)
					temp2 = l * (1f + s);
				else
					temp2 = l + s - l * s;

				float temp1 = 2f * l - temp2;

				r = GetColorComponent(temp1, temp2, h + 0.33333333f);
				g = GetColorComponent(temp1, temp2, h);
				b = GetColorComponent(temp1, temp2, h - 0.33333333f);
			}

		return new Color(r, g, b);
	}
	private float GetColorComponent(float temp1, float temp2, float temp3)
	{
		if (temp3 < 0f)
			temp3 += 1f;
		else if (temp3 > 1f)
			temp3 -= 1f;

		if (temp3 < 0.166666667f)
			return temp1 + (temp2 - temp1) * 6f * temp3;
		else if (temp3 < 0.5f)
			return temp2;
		else if (temp3 < 0.66666666f)
			return temp1 + (temp2 - temp1) * (0.66666666f - temp3) * 6f;
		else
			return temp1;
	}
}

public class StandardColorTrail : ITrailColor
{
	private Color _colour;

	public StandardColorTrail(Color colour) => _colour = colour;

	public Color GetColourAt(float distanceFromStart, float trailLength, List<Vector2> points, Vector2 curPoint)
	{
		float progress = distanceFromStart / trailLength;
		return _colour * (1f - progress);
	}
}

public class OpacityUpdatingTrail : ITrailColor
{

	private Color _startcolor;
	private Color _endcolor;
	private Projectile _proj;
	private float _opacity = 1f;

	public OpacityUpdatingTrail(Projectile proj, Color color)
	{
		_startcolor = color;
		_endcolor = color;
		_proj = proj;
	}

	public OpacityUpdatingTrail(Projectile proj, Color startColor, Color endColor)
	{
		_startcolor = startColor;
		_endcolor = endColor;
		_proj = proj;
	}

	public Color GetColourAt(float distanceFromStart, float trailLength, List<Vector2> points, Vector2 curPoint)
	{
		float progress = distanceFromStart / trailLength;
		if (_proj.active && _proj != null)
			_opacity = _proj.Opacity;

		return Color.Lerp(_startcolor, _endcolor, progress) * (1f - progress) * _opacity;
	}
}

public class LightColorTrail : ITrailColor
{
	private Color _startColor;
	private Color _endColor;
	
	public LightColorTrail (Color startColor, Color endColor)
	{
		_startColor = startColor;
		_endColor = endColor;
	}
	public LightColorTrail(Color color)
	{
		_startColor = color;
		_endColor = color;
	}

	public Color GetColourAt(float distanceFromStart, float trailLength, List<Vector2> points, Vector2 curPoint)
	{
		Color baseColor = Color.Lerp(_startColor, _endColor, distanceFromStart / trailLength);
		Color lightColor = Lighting.GetColor(curPoint.ToTileCoordinates());

		return lightColor.MultiplyRGBA(baseColor);
	}
}
#endregion
