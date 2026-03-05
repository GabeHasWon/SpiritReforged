using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Content.Savanna.Biome;

public class HallowSavannaBGStyle : ModSurfaceBackgroundStyle, IWaterStyle
{
	public override int ChooseMiddleTexture() => BackgroundTextureLoader.GetBackgroundSlot(Mod, "Assets/Textures/Backgrounds/HallowSavannaBackgroundMid");
	public override int ChooseFarTexture() => BackgroundTextureLoader.GetBackgroundSlot(Mod, "Assets/Textures/Backgrounds/HallowSavannaBackgroundFar");

	public override int ChooseCloseTexture(ref float scale, ref double parallax, ref float a, ref float b)
	{
		b -= 400;
		return BackgroundTextureLoader.GetBackgroundSlot(Mod, "Assets/Textures/Backgrounds/HallowSavannaBackgroundNear");
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

	public void ForceWaterStyle(ref int style)
	{
		if (style >= WaterStyleID.Count && Main.LocalPlayer.InModBiome<SavannaBiome>() && Main.LocalPlayer.ZoneHallow)
			style = WaterStyleID.Hallow;
	}
}
