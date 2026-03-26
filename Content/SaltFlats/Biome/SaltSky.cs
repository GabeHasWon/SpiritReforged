using SpiritReforged.Common.Visuals.Skies;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using SpiritReforged.Content.Savanna.Biome;
using System.Linq;
using Terraria.Graphics;

namespace SpiritReforged.Content.SaltFlats.Biome;

public class SaltSky : AutoloadedSky
{
	public override void DrawBelowSunMoon(SpriteBatch spriteBatch)
	{
		GraphicsDevice gd = Main.graphics.GraphicsDevice;

		// Cache old buffers
		VertexBufferBinding[] oldVertexBuffers = gd.GetVertexBuffers();
		IndexBuffer oldIndexBuffer = gd.Indices;

		Color[] gradientColor =
		[
			new Color(29, 63, 219) * FadeOpacity,
		Color.Lerp(Color.Pink, new Color(76, 108, 250), SavannaSky.TimeProgress()) * FadeOpacity,
		Color.Pink * FadeOpacity
		];

		if (Main.LocalPlayer.gravDir == -1)
			gradientColor = gradientColor.Reverse().ToArray();

		try
		{
			Main.tileBatch.Begin();
			//SaltBlockReflective.SaltGridOverlay.DrawSimpleGradient(gradientColor);
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

	public static Vector4 GetSkyColor(float gradientIndex)
	{
		Color[] gradientColor = [new Color(29, 63, 219), Color.Lerp(Color.Pink, new Color(76, 108, 250), SavannaSky.TimeProgress()), Color.Pink];
		return gradientColor[(int)gradientIndex].ToVector4() * Main.ColorOfTheSkies.ToVector4();
	}

	public override Color OnTileColor(Color inColor)
	{
		float progress = SavannaSky.TimeProgress();
		var outColor = Color.Lerp(Color.Pink * 0.5f, new(29, 63, 219), progress);

		return Color.Lerp(inColor, outColor, 0.2f * FadeOpacity);
	}

	internal override bool ActivationCondition(Player p) => !p.ZoneSkyHeight && p.InModBiome<SaltBiome>();
}