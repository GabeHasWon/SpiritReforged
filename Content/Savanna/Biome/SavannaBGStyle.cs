using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Content.Savanna.Biome;

public class SavannaBGStyle : ModSurfaceBackgroundStyle, IBGStyle
{
	public override int ChooseMiddleTexture() => BackgroundTextureLoader.GetBackgroundSlot(Mod, "Assets/Textures/Backgrounds/SavannaBackgroundMid");
	public override int ChooseFarTexture() => BackgroundTextureLoader.GetBackgroundSlot(Mod, "Assets/Textures/Backgrounds/SavannaBackgroundFar");

	public override int ChooseCloseTexture(ref float scale, ref double parallax, ref float a, ref float b)
	{
		b -= 400;
		return BackgroundTextureLoader.GetBackgroundSlot(Mod, "Assets/Textures/Backgrounds/SavannaBackgroundNear");
	}

	public override void ModifyFarFades(float[] fades, float transitionSpeed)
	{
		for (int i = 0; i < fades.Length; i++)
			if (i == Slot)
			{
				fades[i] += transitionSpeed;
				if (fades[i] > 1f)
					fades[i] = 1f;
			}
			else
			{
				fades[i] -= transitionSpeed;
				if (fades[i] < 0f)
					fades[i] = 0f;
			}
	}

	public void ForceBackgroundStyle(ref int style)
	{
		if (style == 2 && Main.LocalPlayer.InModBiome<SavannaBiome>()) //Forcefully override purity desert
			style = ModContent.GetInstance<SavannaBGStyle>().Slot;
	}
}