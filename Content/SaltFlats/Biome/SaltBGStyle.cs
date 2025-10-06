namespace SpiritReforged.Content.SaltFlats.Biome;

public class SaltBGStyle : ModSurfaceBackgroundStyle
{
	//public override int ChooseMiddleTexture() => BackgroundTextureLoader.GetBackgroundSlot(Mod, "Assets/Textures/Backgrounds/SaltBackgroundMid");
	//public override int ChooseFarTexture() => BackgroundTextureLoader.GetBackgroundSlot(Mod, "Assets/Textures/Backgrounds/SaltBackgroundFar");

	public override int ChooseCloseTexture(ref float scale, ref double parallax, ref float a, ref float b)
	{
		b -= 350;
		return BackgroundTextureLoader.GetBackgroundSlot(Mod, "Assets/Textures/Backgrounds/SaltBackgroundNear");
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
}