using SpiritReforged.Content.SaltFlats.Biome;
using SpiritReforged.Content.Savanna.Biome;

namespace SpiritReforged.Common.Visuals;

internal class SaltMenuTheme : ModMenu
{
	public override ModSurfaceBackgroundStyle MenuBackgroundStyle => ModContent.GetInstance<SaltBGStyle>();
	public override int Music => MusicLoader.GetMusicSlot(Mod, "Assets/Music/TitleTheme");

	public override void OnSelected() => SpiritLogo.Reset();

	public override void Update(bool isOnTitleScreen) => SpiritLogo.Update(1 / 60f);

	public static void SaltFlatsDayPalette(out Color outlineColor, out Color underlineColor, out Color shadowColor, //General colors
		out Color sFillColor, out Color sGradientTopColor, out Color sGradientBottomColor, out Color sOutlineGlowColor, //S colors
		out Color fillColorBase, out Color fillColorSecondary, out Color piritFillGlowColor, out Color piritOutlineBaseColor, out Color innerOutlineGlowColor, //Pirit colors
		out Color reforgedColorLeft, out Color reforgedColorRight) //Reforged colors
	{
		//Lots of colors, so much customizability... woah
		outlineColor = new Color(255, 181, 132);            //Orangey outlines
		underlineColor = new Color(172, 27, 20);            //Dark reddish underline
		shadowColor = new Color(40, 204, 255, 51);          //Blueish shadows

		sFillColor = new Color(255, 255, 255);              //S is still white inside
		sGradientTopColor = new Color(255, 221, 213);       //Very bright pink at the top of the gradient, and light blue at the bottom
		sGradientBottomColor = new Color(187, 255, 244);
		sOutlineGlowColor = new Color(255, 139, 132);       //Salmon outline

		fillColorBase = new Color(255, 208, 226);          //Light pastel pink
		fillColorSecondary = new Color(204, 233, 249);     //Light pastel blu
		piritFillGlowColor = new Color(51, 81, 94);        //Gray-blueish glow which makes it appear reflective and cristalline
		piritOutlineBaseColor = Color.White;               //White inner outline
		innerOutlineGlowColor = Color.White;               //Wont glow cuz its white lol

		reforgedColorLeft = Color.Black ;      
		reforgedColorRight = new Color(255, 85, 85);    //Deep salmon gradient
	}

	public static void SaltFlatsNightPalette(out Color outlineColor, out Color underlineColor, out Color shadowColor, //General colors
		out Color sFillColor, out Color sGradientTopColor, out Color sGradientBottomColor, out Color sOutlineGlowColor, //S colors
		out Color fillColorBase, out Color fillColorSecondary, out Color piritFillGlowColor, out Color piritOutlineBaseColor, out Color innerOutlineGlowColor, //Pirit colors
		out Color reforgedColorLeft, out Color reforgedColorRight) //Reforged colors
	{
		//Lots of colors, so much customizability... woah
		outlineColor = Color.White;                       //Glowing white outline
		underlineColor = new Color(30, 0, 240);           //Dark red underline
		shadowColor = new Color(255, 255, 152, 25);

		sFillColor = new Color(255, 255, 255);            //S is still white inside
		sGradientTopColor = new Color(57, 186, 255, 92); //Gradient between faint blue at the top and white at the top
		sGradientBottomColor = new Color(200, 245, 255);
		sOutlineGlowColor = new Color(255, 255, 255);     //Glowing white outline

		fillColorBase = new Color(40, 190, 255);          // Deep azure fill
		fillColorSecondary = new Color(181, 255, 255);    // Paler azure blue
		piritFillGlowColor = new Color(0, 47, 244);       // Deep indigo glow
		piritOutlineBaseColor = new Color(128, 228, 255); // Bright blue inner outline
		innerOutlineGlowColor = new Color(255, 255, 255);  

		reforgedColorLeft = new Color(193, 149, 0);       // Yellowish color on the left
		reforgedColorRight = new Color(255, 68, 68);      // Pinkish red on the right
	}

	public override bool PreDrawLogo(SpriteBatch spriteBatch, ref Vector2 logoDrawCenter, ref float logoRotation, ref float logoScale, ref Color drawColor)
	{
		logoDrawCenter.Y += 16f;

		//The title color alternates between white and a gray that's 1/3 brightness
		float logoLerper = Utils.GetLerpValue(0.6f, 0.4f, drawColor.R / 255f, true);
		SpiritLogo.Draw(spriteBatch, logoDrawCenter, logoScale, SaltFlatsDayPalette, logoLerper, SaltFlatsNightPalette);
		return false;
	}
}
