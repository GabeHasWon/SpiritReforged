/*namespace SpiritReforged.Content.Ocean.Biome;

internal class DeepOceanBackgroundStyle : ModSurfaceBackgroundStyle
{
	/// <summary> Used with <see cref="Common.Visuals.ForceWaterStyle"/> to enforce the correct water style in various cases. </summary>
	/// <returns> The water style to use. </returns>
	public static int ChooseWaterStyle()
	{
		if (Main.LocalPlayer.ZoneCorrupt)
			return WaterStyleID.Corrupt;
		else if (Main.LocalPlayer.ZoneCrimson)
			return WaterStyleID.Crimson;
		else if (Main.LocalPlayer.ZoneHallow)
			return WaterStyleID.Hallow;
		else
			return 0;
	}

	public override int ChooseMiddleTexture() => BackgroundTextureLoader.GetBackgroundSlot(Mod, "Assets/Textures/Backgrounds/OceanUnderwaterBG2");
	public override int ChooseFarTexture() => BackgroundTextureLoader.GetBackgroundSlot(Mod, "Assets/Textures/Backgrounds/OceanUnderwaterBG3");

	public override int ChooseCloseTexture(ref float scale, ref double parallax, ref float a, ref float b)
	{
		scale *= .86f;
		b -= 300;
		return BackgroundTextureLoader.GetBackgroundSlot(Mod, "Assets/Textures/Backgrounds/OceanUnderwaterBG2");
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
}*/
