using SpiritReforged.Common.Easing;

namespace SpiritReforged.Common.CombatTextCommon;
// TODO: make this more system more robust and work for more use cases
public class ColoredCombatText : ILoadable
{
	// the timeleft of each combat text
	internal static int[] maxTimeLefts = new int[Main.maxCombatText];
	// the color the combat text should start as
	internal static Color[] colors = new Color[Main.maxCombatText];
	// the crit color the combat text should start as
	internal static Color[] critColors = new Color[Main.maxCombatText];

	public void Load(Mod mod)
	{
		On_CombatText.UpdateCombatText += FadeDamageText;
	}

	public void Unload()
	{

	}

	private void FadeDamageText(On_CombatText.orig_UpdateCombatText orig)
	{
		orig();

		for (int i = 0; i < Main.maxCombatText; i++)
		{
			CombatText text = Main.combatText[i];
			if (maxTimeLefts[i] > 0)
			{
				if (text.active)
				{
					Color start = text.crit ? critColors[i] : colors[i];
					Color orange = text.crit ? CombatText.DamagedHostileCrit : CombatText.DamagedHostile;

					text.color = Color.Lerp(start, orange, EaseBuilder.EaseCircularInOut.Ease(1f - text.lifeTime / (float)maxTimeLefts[i]));
				}
				else
				{
					maxTimeLefts[i] = 0;
				}
			}
		}
	}

	/// <summary>
	/// Adds a combat text to be faded
	/// </summary>
	/// <param name="index">the index of the combat text</param>
	/// <param name="color">the color the combat text should start as</param>
	/// <param name="critColor">the color the combat text should start as if its a crit</param>
	public static void AddCombatText(int index, Color color, Color critColor)
	{
		maxTimeLefts[index] = Main.combatText[index]?.lifeTime ?? 10;

		colors[index] = color;
		critColors[index] = critColor;
	}
}
