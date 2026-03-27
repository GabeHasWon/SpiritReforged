using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.Skies;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using SpiritReforged.Content.Savanna.Biome;
using System.Linq;
using Terraria.Graphics;

namespace SpiritReforged.Content.SaltFlats.Biome;

public class SaltSky : AutoloadedSky
{
	public static Color[] GetSkyGradient(float opacity = 1f)
	{
		Color[] gradientColors =
		[
			new Color(29, 63, 219),
			Color.Lerp(Color.Pink, new Color(76, 108, 250), SavannaSky.TimeProgress()),
			Color.Pink
		];

		SaltFlatsSystem.ModifySkyGradientColors(ref gradientColors[0], ref gradientColors[1], ref gradientColors[2]);

		for (int i = 0; i < 3; i++)
			gradientColors[i] *= opacity;

		return gradientColors;
	}

	public override void DrawBelowSunMoon(SpriteBatch spriteBatch)
	{
		GraphicsDevice gd = Main.graphics.GraphicsDevice;

		// Cache old buffers
		VertexBufferBinding[] oldVertexBuffers = gd.GetVertexBuffers();
		IndexBuffer oldIndexBuffer = gd.Indices;

		float opacity = FadeOpacity * (SaltBGStyle.DrawingSkyObjectReflection ? 0.5f : 1f);

		Color[] gradientColor = GetSkyGradient(opacity);

		if (Main.LocalPlayer.gravDir == -1)
			gradientColor = gradientColor.Reverse().ToArray();

		try
		{
			Main.tileBatch.Begin();
			SaltBlockReflective.SaltGridOverlay.DrawSimpleGradient(gradientColor);
			Main.tileBatch.End();
		}
		finally
		{
			// Re-prime the transform expected by the background.
			Main.tileBatch.Begin(transformation: Main.BackgroundViewMatrix.TransformationMatrix);
			Main.tileBatch.End();

			// Re apply
			if (oldVertexBuffers != null && oldVertexBuffers.Length > 0)
				gd.SetVertexBuffers(oldVertexBuffers);
			else
				gd.SetVertexBuffer(null);

			gd.Indices = oldIndexBuffer;
		}
	}

	public override Color OnTileColor(Color inColor)
	{
		float progress = SavannaSky.TimeProgress();
		var outColor = Color.Lerp(Color.Pink * 0.5f, inColor, Math.Min(1, progress * 2f));

		return Color.Lerp(inColor, outColor, 0.2f * FadeOpacity);
	}

	internal override bool ActivationCondition(Player p) => !p.ZoneSkyHeight && p.InModBiome<SaltBiome>();
}