﻿using System.Reflection;

namespace SpiritReforged.Common.Visuals.Glowmasks;

/// <summary>
/// Autoloads the glowmask of the according Entity (currently, NPCs and Tiles).<br/>
/// <paramref name="stringData"/> is one of two things. First, the Color of the glowmask, if constant, in one of these formats:<br/>
/// <c>R,G,B,A</c><br/><c>R,G,B</c><br/>All numbers are bytes.<br/><br/>
/// If you want a dynamic color, you can create a static method that takes an <c>object</c> and returns <c>Color</c>.<br/>
/// You'll need a fully qualified class name and the name of the method, prepended by "Method:". For example,
/// <code>[AutoloadGlowmask("Method:Content.Savanna.NPCs.Gar.Gar Glow")]</code><br/>
/// This would call <c>Gar.Glow</c>. 
/// </summary>
/// <param name="stringData"></param>
[AttributeUsage(AttributeTargets.Class)]
internal class AutoloadGlowmaskAttribute(string stringData, bool drawAutomatically = true) : Attribute
{
	public const BindingFlags SearchFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

	public string StringData = stringData;
	public bool DrawAutomatically = drawAutomatically;

	public static Color GetColorFromString(string colorString)
	{
		string[] rgba = colorString.Split(',');

		if (rgba.Length <= 2 || rgba.Length >= 5)
			throw new InvalidCastException("GlowmaskAttribute GlowColorString should be R,G,B or R,G,B,A!");

		byte r = byte.Parse(rgba[0]);
		byte g = byte.Parse(rgba[1]);
		byte b = byte.Parse(rgba[2]);

		if (rgba.Length == 3)
			return new Color(r, g, b);

		return new Color(r, g, b, byte.Parse(rgba[3]));
	}

	public static Func<object, Color> GetAttributeInfo(Mod mod, Type type, out bool autoDraw)
	{
		var attribute = GetCustomAttribute(type, typeof(AutoloadGlowmaskAttribute)) as AutoloadGlowmaskAttribute;
		string colString = attribute.StringData;
		Func<object, Color> color;

		if (!colString.StartsWith("Method:"))
			color = (_) => GetColorFromString(colString);
		else
		{
			string[] split = colString.Split(' ');
			var colorMethod = mod.Code.GetType("SpiritReforged." + split[0]["Method:".Length..]).GetMethod(split[1], SearchFlags);
			color = Delegate.CreateDelegate(typeof(Func<object, Color>), null, colorMethod) as Func<object, Color>;
		}

		autoDraw = attribute.DrawAutomatically;
		return color;
	}
}
