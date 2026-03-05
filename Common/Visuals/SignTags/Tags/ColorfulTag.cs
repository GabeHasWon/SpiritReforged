using SpiritReforged.Common.Misc;

namespace SpiritReforged.Common.Visuals.CustomText;

internal class ColorfulTag : SignTag
{
	private Color _color;

	public override string Key => "colorful";

	protected override void Reset() => _color = default;

	protected override bool ParseParams(string[] parameters)
	{
		Color color = new();

		for (int i = 0; i < parameters.Length; i++)
		{
			if (!int.TryParse(parameters[i], out int type))
				return false;

			if (i == 0)
				color.R = (byte)type;
			else if (i == 1)
				color.G = (byte)type;
			else if (i == 2)
				color.B = (byte)type;
			else
				break; //Doesn't modify Color.A
		}

		_color = color;
		return true;
	}

	public override bool Draw(Rectangle panel, string[] text, int numLines, ref Color color)
	{
		color = ((_color == default) ? Main.DiscoColor : _color).Additive(Main.mouseTextColor);

		if (Main.SettingsEnabled_OpaqueBoxBehindTooltips)
			color = Color.Lerp(color, Color.White, .2f);

		return false;
	}
}
