using MonoMod.RuntimeDetour;
using System.Runtime.CompilerServices;

namespace SpiritReforged.Content.SaltFlats.Biome;

public class SaltBGStyle : ModSurfaceBackgroundStyle
{
	private const string Path = "Assets/Textures/Backgrounds/";

	private static Hook DetourDrawFarBG = null;

	public override void Load()
	{
		var info = typeof(SurfaceBackgroundStylesLoader).GetMethod(nameof(SurfaceBackgroundStylesLoader.DrawFarTexture));
		DetourDrawFarBG = new(info, DetourDrawFar);
	}

	private void DetourDrawFar(Action<SurfaceBackgroundStylesLoader> orig, SurfaceBackgroundStylesLoader self)
	{
		ref int bgTopY = ref GetBGTopY(Main.instance);
		int old = bgTopY;

		float screenCenterY = Main.screenPosition.Y + Main.screenHeight / 2f;
		float dif = SaltFlatsSystem.SurfaceHeight * 16 - screenCenterY;
		bgTopY = (int)(dif - dif * 0.8f);

		orig(self);

		bgTopY = old;
	}

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "bgTopY")]
	private static extern ref int GetBGTopY(Main main);

	public override int ChooseMiddleTexture() => BackgroundTextureLoader.GetBackgroundSlot(Mod, Path + "SaltBackgroundMid");
	public override int ChooseFarTexture() => BackgroundTextureLoader.GetBackgroundSlot(Mod, Path + "SaltBackgroundFar");
	public override int ChooseCloseTexture(ref float scale, ref double p, ref float a, ref float b) => BackgroundTextureLoader.GetBackgroundSlot(Mod, Path + "SaltBackgroundNear");

	public override void ModifyFarFades(float[] fades, float transitionSpeed)
	{
		for (int i = 0; i < fades.Length; i++)
		{
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
}