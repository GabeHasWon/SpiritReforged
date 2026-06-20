namespace SpiritReforged.Content.Forest.Glyphs.Rot;

public class BlightSpread
{
	public const int MaxStacks = 10;
	public const int DecayTime = 60;

	public bool Active => stacks > 0;

	public int stacks;
	public int decayTime;
	public int decayIndicator;

	public void Decay()
	{
		if (--decayTime == 0)
		{
			if (stacks > 0)
				stacks--;

			decayTime = DecayTime;
			decayIndicator = 20;
		}

		if (decayIndicator > 0)
			decayIndicator--;
	}
}