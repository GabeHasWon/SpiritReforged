using SpiritReforged.Common.Visuals.Skies;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using SpiritReforged.Content.Savanna.Biome;

namespace SpiritReforged.Content.SaltFlats.Biome;

public class SaltSky : AutoloadedSky
{
	public override void DrawBelowSunMoon(SpriteBatch spriteBatch)
	{
		Main.tileBatch.Begin();
		SaltBlockReflective.SaltGridOverlay.DrawSimpleGradient(new Color(29, 63, 219) * FadeOpacity, Color.Lerp(Color.Pink, new Color(76, 108, 250), SavannaSky.TimeProgress()) * FadeOpacity, Color.Pink * FadeOpacity);
		Main.tileBatch.End();
	}

	public override Color OnTileColor(Color inColor)
	{
		float progress = SavannaSky.TimeProgress();
		var outColor = Color.Lerp(Color.Pink * 0.5f, new(29, 63, 219), progress);

		return Color.Lerp(inColor, outColor, 0.2f * FadeOpacity);
	}

	internal override bool ActivationCondition(Player p) => !p.ZoneSkyHeight && p.InModBiome<SaltBiome>();
}