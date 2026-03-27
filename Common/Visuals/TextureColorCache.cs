using Microsoft.CodeAnalysis;
using SpiritReforged.Common.Misc;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace SpiritReforged.Common.Visuals;

/// <summary> Caches basic color data of textures for efficiency. </summary>
[Autoload(Side = ModSide.Client)]
internal class TextureColorCache
{
	private static readonly Dictionary<Texture2D, Color[]> ColorCache = [];
	private static readonly Dictionary<Texture2D, Texture2D> SolidTextureCache = [];
	private static readonly Dictionary<Texture2D, Texture2D> TextureRampCache = [];

	public static Color[] GetColors(Texture2D texture)
	{
		if (ColorCache.TryGetValue(texture, out Color[] value))
			return value;

		var data = new Color[texture.Width * texture.Height];
		texture.GetData(data);

		//Orders colors from darkest to brightest, and excludes fully transparent pixels
		data = [.. data.OrderBy(x => x.ToVector3().Length()).Where(x => x != Color.Transparent)];

		if (data.Length != 0)
			ColorCache.Add(texture, data);
		return data;
	}

	public static Color GetBrightestColor(Texture2D texture) => GetColors(texture)[^1];
	public static Color GetDarkestColor(Texture2D texture) => GetColors(texture)[0];

	public static Texture2D ColorSolid(Texture2D texture, Color color)
	{
		if (SolidTextureCache.TryGetValue(texture, out var textureFromCache))
			return textureFromCache;

		var data = new Color[texture.Width * texture.Height];
		texture.GetData(data);

		for (int i = data.Length - 1; i >= 0; i--)
		{
			if (data[i] != Color.Transparent)
			{
				byte alpha = data[i].A;
				data[i] = color.Additive(alpha);
			}
		}

		var textureToCache = new Texture2D(Main.graphics.GraphicsDevice, texture.Width, texture.Height);
		textureToCache.SetData(data);

		SolidTextureCache.Add(texture, textureToCache);
		return textureToCache;
	}

	#region Mean shift clustering
	public static Texture2D GetDominantPaletteInTileTexture(Texture2D texture)
	{
		const bool debugMode = false;

		if (!debugMode && TextureRampCache.TryGetValue(texture, out Texture2D value))
			return value;

		var data = new Color[52 * 34];
		texture.GetData(0, new Rectangle(18, 0, 52, 34), data, 0, 52 * 34);

		Dictionary<Color, PaletteRampElement> paletteElements = new();

		//Grab the colors from the inner section of the tilesheet frames
		for (int i = 0; i < 52; i += 2)
		{
			for (int j = 0; j < 34; j += 2)
			{
				Color pixelColor = data[i + j * 52];
				//ignore transparent
				if (pixelColor == Color.Transparent)
					continue;

				//Add the pixel colors to our list of ramp elements, increasing their weight if we've seen them already
				if (!paletteElements.TryGetValue(pixelColor, out PaletteRampElement rampElement))
				{
					rampElement = new PaletteRampElement(new Vector3(pixelColor.R / 255f, pixelColor.G / 255f, pixelColor.B / 255f));
					paletteElements[pixelColor] = rampElement;
				}

				rampElement.weight++;
			}
		}

		PaletteRampElement[] rampElements = paletteElements.Values.ToArray();
		List<MeanShiftCluster> clusters = new List<MeanShiftCluster>();

		//Create a mean shift cluster per element
		for (int i = 0; i < rampElements.Length; i++)
			clusters.Add(new MeanShiftCluster(rampElements[i].color));

		//Loop over and over, repeatedly mean shifting our clusters until we settle
		IterativelyShiftMeanClusters(clusters, rampElements, bandWidth : 0.17f);

		//Find all orphaned colors that ended up outside of our mean shift radii and include them into the nearest cluster
		for (int i = 0; i < rampElements.Length; i++)
		{
			PaletteRampElement paletteElement = rampElements[i];

			//If our palette element is already in the cluster
			if (clusters.Any(c => c.clusterElements.Contains(paletteElement)))
				continue;

			//Add it to the nearest cluster
			MeanShiftCluster closestCluster = clusters.OrderBy(c => c.GetDistanceToHueSat(paletteElement)).First();
			closestCluster.clusterElements.Add(paletteElement);
			closestCluster.totalPixelWeight += paletteElement.weight;
		}

		if (debugMode)
		{
			int rampIndex = 0;
			foreach (MeanShiftCluster cluster in clusters.OrderByDescending(c => c.totalPixelWeight))
			{
				Texture2D rampTexture = GeneratePaletteRampFromMeanShiftCluster(cluster);
				string path = $"{Main.SavePath}/PaletteTest/";
				Stream saveStream = File.OpenWrite(path + texture.Name + "Ramp_" + rampIndex.ToString() + ".png");
				rampTexture.SaveAsPng(saveStream, 2, 128);
				saveStream.Dispose();
				rampIndex++;
			}
		}

		Texture2D textureToCache = GeneratePaletteRampFromMeanShiftCluster(clusters.OrderByDescending(c => c.totalPixelWeight).First());
		if (!TextureRampCache.ContainsKey(texture))
			TextureRampCache.Add(texture, textureToCache);
		return textureToCache;
	}

	private static void IterativelyShiftMeanClusters(List<MeanShiftCluster> clusters, PaletteRampElement[] rampElements, float clusterMergeDistance = 0.02f, float bandWidth = 0.15f, int iterations = 100)
	{
		clusterMergeDistance *= clusterMergeDistance;

		for (int i = 0; i < 100; i++)
		{
			float totalDistanceMoved = 0;

			for (int j = clusters.Count - 1; j >= 0; j--)
			{
				float clusterShift = clusters[j].UpdateNearestClusterElements(rampElements, bandWidth);

				//If clustershift is -1 it means the cluster was around zero ramp elements (???) so we remove it!
				if (clusterShift == -1)
					clusters.RemoveAt(j);
				else
					totalDistanceMoved += clusterShift;
			}

			//Merge clusters that are too close to one another
			for (int j = clusters.Count - 1; j >= 0; j--)
			{
				for (int k = j - 1; k >= 0; k--)
				{
					bool closeEnoughToMerge = (clusters[j].meanColor - clusters[k].meanColor).LengthSquared() < clusterMergeDistance;
					if (closeEnoughToMerge)
					{
						clusters.RemoveAt(j);
						break;
					}
				}
			}

			//If our clusters have settled enough, stop
			if (totalDistanceMoved < 0.02f)
				break;
		}
	}

	private static Texture2D GeneratePaletteRampFromMeanShiftCluster(MeanShiftCluster cluster)
	{
		Vector3 brightnessDot = new Vector3(0.299f, 0.587f, 0.114f);
		PaletteRampElement[] rampColors = cluster.clusterElements.OrderBy(p => Vector3.Dot(p.color, brightnessDot)).ToArray();
		Color[] data = new Color[128 * 2];

		//??? should never happen
		if (rampColors.Length == 0)
		{
			Color avgColor = new Color(cluster.meanColor.X, cluster.meanColor.Y, cluster.meanColor.Z);
			for (int i = 0; i < 128 * 2; i++)
				data[i] = avgColor;
		}
		else if (rampColors.Length == 1)
		{
			Color soleColor = new Color(rampColors[0].color.X, rampColors[0].color.Y, rampColors[0].color.Z);
			for (int i = 0; i < 128 * 2; i++)
				data[i] = soleColor;
		}
		else
		{
			int currentColorIndex = 0;
			Vector3 currentColor = rampColors[0].color;
			float colorBrightness = Vector3.Dot(currentColor, brightnessDot);

			Vector3 lastColor = currentColor;
			float lastColorBrightness = colorBrightness;

			for (int i = 0; i < 128; i++)
			{
				float brightness = i / 127f;
				while (currentColorIndex < rampColors.Length - 1 && brightness > colorBrightness)
				{
					currentColorIndex++;

					lastColor = currentColor;
					lastColorBrightness = colorBrightness;

					currentColor = rampColors[currentColorIndex].color;
					colorBrightness = Vector3.Dot(currentColor, brightnessDot);
				}

				float colorLerper = colorBrightness == lastColorBrightness ? 0 : Utils.GetLerpValue(lastColorBrightness, colorBrightness, brightness, true);
				Vector3 lerpedColor = Vector3.Lerp(lastColor, currentColor, colorLerper);
				data[i * 2] = new Color(lerpedColor.X, lerpedColor.Y, lerpedColor.Z);
				//Non-lerped colors
				data[i * 2 + 1] = new Color(currentColor.X, currentColor.Y, currentColor.Z);
			}
		}

		var outputTexture = new Texture2D(Main.graphics.GraphicsDevice, 2, 128);
		outputTexture.SetData(data);
		return outputTexture;
	}

	private class PaletteRampElement
	{
		public Vector3 color;
		public Vector2 colorHueSat;
		public float weight;

		public PaletteRampElement(Vector3 color)
		{
			this.color = color;
			colorHueSat = ColorToHueSat(color);
		}
	}

	private class MeanShiftCluster
	{
		public Vector3 meanColor;
		public Vector2 meanhueSat;

		public List<PaletteRampElement> clusterElements = new List<PaletteRampElement>();
		public float totalPixelWeight;

		public MeanShiftCluster(Vector3 color)
		{
			meanColor = color;
			meanhueSat = ColorToHueSat(color);
		}

		public float UpdateNearestClusterElements(PaletteRampElement[] allElements, float bandwidth = 0.15f)
		{
			float totalWeight = 0f;
			Vector3 totalColors = Vector3.Zero;
			clusterElements.Clear();

			foreach (PaletteRampElement rampElement in allElements)
			{
				float distToElement = GetDistanceToHueSat(rampElement);
				if (distToElement > bandwidth)
					continue;

				float gaussianWeight = GetGaussianWeight(distToElement, bandwidth);
				totalWeight += gaussianWeight * rampElement.weight;
				totalColors += rampElement.color * gaussianWeight * rampElement.weight;
				clusterElements.Add(rampElement);
				totalPixelWeight += rampElement.weight;
			}

			if (totalWeight == 0)
				return -1;

			Vector3 lastMeanColor = meanColor;
			meanColor = totalColors / totalWeight;
			meanhueSat = ColorToHueSat(meanColor);
			return (lastMeanColor - meanColor).Length();
		}

		private float GetDistanceTo(PaletteRampElement rampElement)
		{
			return (rampElement.color - meanColor).Length();
		}

		public float GetDistanceToHueSat(PaletteRampElement rampElement)
		{
			return (rampElement.colorHueSat - meanhueSat).Length();
		}

		private float GetGaussianWeight(float distance, float bandwidth)
		{
			return MathF.Exp(-0.5f * MathF.Pow(distance / bandwidth, 2)) / (bandwidth * MathF.Sqrt(2 * MathF.PI));
		}
	}

	private static Vector2 ColorToHueSat(Vector3 color)
	{
		float r = color.X;
		float g = color.Y;
		float b = color.Z;

		float maxValue = Math.Max(Math.Max(r, g), b);
		float minValue = Math.Min(Math.Min(r, g), b);

		float hue = 0;
		float sat = 0;
		float value = maxValue;

		float valueDiff = maxValue - minValue;

		if (maxValue != 0)
		{
			sat = valueDiff / maxValue;
		}

		//Entirely desaturated
		if (maxValue == minValue)
		{
			hue = -1;
		}

		else
		{
			if (maxValue == r)
			{
				hue = (g - b) / valueDiff + (g < b ? 6 : 0);
			}
			else if (maxValue == g)
			{
				hue = (b - r) / valueDiff + 2;
			}
			else if (maxValue == b)
			{
				hue = (r - g) / valueDiff + 4;
			}

			hue /= 6;
		}

		//Desaturate dark colors cuz they tend to be very saturated
		sat *= 0.3f + 0.7f * Utils.GetLerpValue(0f, 0.15f, value, true);

		return Vector2.UnitX.RotatedBy(hue * MathHelper.TwoPi) * sat;
	}
	#endregion
}