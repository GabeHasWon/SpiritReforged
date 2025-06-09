using SpiritReforged.Content.Savanna.Biome;

namespace SpiritReforged.Common.Visuals;

internal class SavannaMenuTheme : ModMenu
{
	public override ModSurfaceBackgroundStyle MenuBackgroundStyle => ModContent.GetInstance<SavannaBGStyle>();
	public override int Music => MusicLoader.GetMusicSlot(Mod, "Assets/Music/TitleTheme");

	public override void OnSelected() => SpiritLogo.Reset();

	public override void Update(bool isOnTitleScreen) => SpiritLogo.Update(1 / 60f);

	public static void SavannaOrangePalette(out Color outlineColor, out Color underlineColor, out Color shadowColor, //General colors
		out Color sFillColor, out Color sGradientTopColor, out Color sGradientBottomColor, out Color sOutlineGlowColor, //S colors
		out Color fillColorBase, out Color fillColorSecondary, out Color piritFillGlowColor, out Color piritOutlineBaseColor, out Color innerOutlineGlowColor, //Pirit colors
		out Color reforgedColorLeft, out Color reforgedColorRight) //Reforged colors
	{
		//Lots of colors, so much customizability... woah
		outlineColor = new Color(153, 46, 0);            //Orange letter outline (not so indigo now are we)
		underlineColor = Color.Black;                    //Black underline and shadows as usual
		shadowColor = new Color(0, 0, 0, 0.2f);

		sFillColor = new Color(255, 255, 255);            //S is still white inside
		sGradientTopColor = new Color(255, 98, 212);      //Sunset colored gradient in pink and yellow
		sGradientBottomColor = new Color(242, 255, 186);
		sOutlineGlowColor = new Color(215, 94, 0);        //Lighter orange outline

		fillColorBase = new Color(255, 173, 62);          // Summery orange for the left of the pirit
		fillColorSecondary = new Color(174, 255, 227);    // Cool pale blue for the right of the letters
		piritFillGlowColor = new Color(40, 51, 0);        // Dark green for the glow, but it layers well with the chosen colors
		piritOutlineBaseColor = new Color(255, 224, 159); // Bright orange-yellow letter inner outline
		innerOutlineGlowColor = new Color(98, 38, 0);     // No change from the usual

		reforgedColorLeft = new Color(193, 149, 0);       // Yellowish color on the left
		reforgedColorRight = new Color(255, 68, 68);      // Pinkish red on the right
	}

	public override bool PreDrawLogo(SpriteBatch spriteBatch, ref Vector2 logoDrawCenter, ref float logoRotation, ref float logoScale, ref Color drawColor)
	{
		logoDrawCenter.Y += 16f;

		//The title color alternates between white and a gray that's 1/3 brightness
		float logoLerper = Utils.GetLerpValue(0.6f, 0.4f, drawColor.R / 255f, true);
		SpiritLogo.Draw(spriteBatch, logoDrawCenter, logoScale, SavannaOrangePalette, logoLerper, SpiritLogo.PearlescentBluePalette);
		return false;
	}
}
