namespace SpiritReforged.Common.Visuals.CustomText;

internal class WavyTag : SignTag
{
	private int _strength, _length;

	public override string Key => "wavy";

	protected override void Reset()
	{
		_strength = 12;
		_length = 200;
	}

	protected override bool ParseParams(string[] parameters)
	{
		int strength = 0, length = 0;

		for (int i = 0; i < parameters.Length; i++)
		{
			if (!int.TryParse(parameters[i], out int type))
				return false;

			if (i == 0)
				strength = type;
			else if (i == 1)
				length = type;
			else
				break;
		}

		_strength = strength;
		_length = length;

		return true;
	}

	public override bool Draw(Rectangle panel, string[] text, int numLines, ref Color color)
	{
		var effect = AssetLoader.LoadedShaders["Ripple"].Value;
		effect.Parameters["progress"].SetValue((float)(Main.timeForVisualEffects / 10f % MathHelper.TwoPi));
		effect.Parameters["strength"].SetValue(.001f * _strength);
		effect.Parameters["length"].SetValue(.001f * _length);

		effect.CurrentTechnique.Passes[0].Apply(); //Restarting the spritebatch is unecessary here

		return false;
	}
}
