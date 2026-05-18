using SpiritReforged.Common.WorldGeneration.Ecotones;
using System.Linq;

namespace SpiritReforged.Common.ModCompat.EcotoneMapper;

internal class EcotoneMapperDisplay : ModSystem
{
	internal static void DrawSelectionAreas()
	{
		if (!EcotoneMapperHooks.ActuallyManuallyMapping)
		{
			return;
		}

		float offscreenXMin = 10f;
		float offscreenYMin = 10f;

		float num20 = Main.mapFullscreenPos.X;// does it zoom into cursor or center.
		float num21 = Main.mapFullscreenPos.Y;
		num20 *= Main.mapFullscreenScale;
		num21 *= Main.mapFullscreenScale;
		float panX = -num20 + Main.screenWidth / 2f;
		float num2 = -num21 + Main.screenHeight / 2f;
		panX += offscreenXMin * Main.mapFullscreenScale;
		num2 += offscreenYMin * Main.mapFullscreenScale;

		foreach (var entry in EcotoneSurfaceMapping.Entries)
		{
			Point minimumPoint = entry.SurfacePoints.MinBy(x => x.X);
			int maxY = entry.SurfacePoints.MaxBy(x => x.Y).Y;

			int x = (int)((minimumPoint.X - offscreenXMin) * Main.mapFullscreenScale + panX);
			int y = (int)((minimumPoint.Y - offscreenYMin) * Main.mapFullscreenScale + num2);
			int width = (int)(entry.Width * Main.mapFullscreenScale);
			int height = (int)((maxY - minimumPoint.Y) * Main.mapFullscreenScale);

			Rectangle drawRectangle = new Rectangle(x, y, width, height);
			Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, drawRectangle, Color.Red * 0.6f);
		}
	}
}
