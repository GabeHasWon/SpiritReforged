using Humanizer.Localisation.DateToOrdinalWords;
using MonoMod.RuntimeDetour;
using SpiritReforged.Content.Underground.WayfarerSet;
using System.Runtime.CompilerServices;

namespace SpiritReforged.Content.SaltFlats.Biome;

public class SaltBGStyle : ModSurfaceBackgroundStyle
{
	private static Hook DetourDrawFarBG = null;

	private static int pushBGTopHack = 0;

	public override void Load()
	{
		var info = typeof(SurfaceBackgroundStylesLoader).GetMethod(nameof(SurfaceBackgroundStylesLoader.DrawFarTexture));
		DetourDrawFarBG = new(info, DetourDrawFar);

		On_Main.DrawSurfaceBG_BackMountainsStep1 += DrawBGStep1;
	}

	private void DrawBGStep1(On_Main.orig_DrawSurfaceBG_BackMountainsStep1 orig, Main self, double backgroundTopMagicNumber, float bgGlobalScaleMultiplier, int pushBGTopHack)
	{
		SaltBGStyle.pushBGTopHack = pushBGTopHack;
		orig(self, backgroundTopMagicNumber, bgGlobalScaleMultiplier, pushBGTopHack);
	}

	private void DetourDrawFar(Action<SurfaceBackgroundStylesLoader> orig, SurfaceBackgroundStylesLoader self)
	{
		ref int bgTopY = ref GetBGTopY(Main.instance);
		int old = bgTopY;

		int expected = GetExpectedBGHeight(Main.LocalPlayer.Center.Y - Main.screenHeight / 2f);

		Main.NewText("Expected: " + expected);
		Main.NewText("Before: " + bgTopY);

		int surface = (int)(Main.LocalPlayer.Center.Y / 16f) - 40;
		surface += (int)Main.worldSurface - surface;
		bgTopY = GetExpectedBGHeight(surface * 16 - Main.screenHeight / 2f);

		orig(self);

		bgTopY = old;
	}

	private static int GetExpectedBGHeight(float verticalPosition)
	{
		float adjSurface = (float)Main.worldSurface;
		float num17 = verticalPosition + Main.screenHeight / 2 - 600f;

		if (adjSurface == 0f)
			adjSurface = 1f;

		float num18 = (0f - num17 + GetScreenOff(Main.instance) / 2f) / (adjSurface * 16f);
		return (int)(num18 * 1300.0 + 1090.0) + (int)GetScAdj(Main.instance) + pushBGTopHack;
	}
	private static int AdjustBackgroundToFitFlatArea(int bgTopY)
	{
		int y = (int)(Main.LocalPlayer.Center.Y / 16f);
		return (int)(bgTopY - Main.MouseScreen.Y);
	}

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "bgTopY")]
	private static extern ref int GetBGTopY(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "screenOff")]
	private static extern ref float GetScreenOff(Main main);

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "scAdj")]
	private static extern ref float GetScAdj(Main main);

	public override int ChooseMiddleTexture() => BackgroundTextureLoader.GetBackgroundSlot(Mod, "Assets/Textures/Backgrounds/SaltBackgroundMid");
	public override int ChooseFarTexture() => BackgroundTextureLoader.GetBackgroundSlot(Mod, "Assets/Textures/Backgrounds/SaltBackgroundFar");
	public override int ChooseCloseTexture(ref float scale, ref double parallax, ref float a, ref float b) 
		=> BackgroundTextureLoader.GetBackgroundSlot(Mod, "Assets/Textures/Backgrounds/SaltBackgroundNear");

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