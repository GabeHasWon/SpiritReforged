using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals.Skies;

namespace SpiritReforged.Content.Savanna.Biome;

public class SavannaSky : AutoloadedSky
{
	public static float TimeProgress()
	{
		if (Main.dayTime)
			return (float)Math.Sin(Math.PI * Main.time / Main.dayLength);
		else
			return (float)Math.Sin(Math.PI * Main.time / Main.nightLength);
	}

	private static Color SavannaColor()
	{
		float sunRiseSetFactor = 1 - TimeProgress();
		float midDayFactor = Main.dayTime ? TimeProgress() : 0;

		sunRiseSetFactor = Main.dayTime ? 
			EaseFunction.EaseQuadOut.Ease(sunRiseSetFactor) 
			: EaseFunction.EaseCircularIn.Ease(sunRiseSetFactor);

		var sunRiseSetColor = new Color(242, 89, 58);
		var midDayColor = new Color(184, 204, 217, 200);

		var finalColor = Color.Lerp(sunRiseSetColor, midDayColor, EaseFunction.EaseQuadOut.Ease(midDayFactor));

		//Make it slightly dimmer during the sunrise
		float sunRiseFactor = EaseFunction.EaseCircularOut.Ease((float)(Main.time / Main.dayLength));
		if (!Main.dayTime)
			sunRiseFactor = 1 - EaseFunction.EaseCircularOut.Ease((float)(Main.time / Main.nightLength));

		sunRiseFactor = MathHelper.Lerp(sunRiseFactor, 1, 0.7f);

		return finalColor * Math.Min(midDayFactor + sunRiseSetFactor, 1) * sunRiseFactor;
	}

	public override void DrawBelowSunMoon(SpriteBatch spriteBatch)
	{
		float dayProgress = Main.dayTime ? TimeProgress() : 0;
		dayProgress = EaseFunction.EaseQuadOut.Ease(dayProgress);
		Color skyColor = SavannaColor() * FadeOpacity;
		spriteBatch.Draw(TextureAssets.MagicPixel.Value,
			new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
			null,
			skyColor * dayProgress * FadeOpacity * 0.5f);

		Texture2D gradientTex = AssetLoader.LoadedTextures["SkyGradient"].Value;
		float invertedDayProgress(float minValue) => Math.Max((1 - dayProgress), minValue);
		Color gradientColor = Color.Lerp(skyColor, Color.White * FadeOpacity * dayProgress * invertedDayProgress(0.25f), 0.1f).Additive(170);
		int verticalOffset = (int)MathHelper.Lerp(0, -100, dayProgress);

		spriteBatch.Draw(gradientTex,
			new Rectangle(0, verticalOffset, Main.screenWidth, Main.screenHeight),
			null,
			Color.Lerp(skyColor, Color.White * FadeOpacity * dayProgress * invertedDayProgress(0.25f), 0.1f).Additive(170));

		spriteBatch.Draw(gradientTex,
			new Rectangle(0, Main.screenHeight + verticalOffset, Main.screenWidth, -verticalOffset),
			new Rectangle(0, gradientTex.Height - 1, gradientTex.Width, 1),
			gradientColor);
	}

	public override Color OnTileColor(Color inColor) => Color.Lerp(inColor, SavannaColor(), 0.2f * FadeOpacity);
	internal override bool ActivationCondition(Player p) => !p.ZoneSkyHeight && p.InModBiome<SavannaBiome>();
}