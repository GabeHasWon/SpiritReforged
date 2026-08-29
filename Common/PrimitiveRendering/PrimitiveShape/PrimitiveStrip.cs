using System.Linq;
using SpiritReforged.Common.MathHelpers;

namespace SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;

public enum StripTaperType
{
	TaperEnd,
	TaperStart,
	TaperBoth,
	None
}

/// <summary>
/// Draws a strip of rectangles through a given array of positions, tapering and ending with a triagle towards the start or end of the array, or not at all
/// </summary>
public class PrimitiveStrip : IPrimitiveShape
{
	public PrimitiveType GetPrimitiveType => PrimitiveType.TriangleStrip;
	public Vector2[] PositionArray { get; set; }
	public float Width { get; set; }
	public Color Color { get; set; }

	public void PrimitiveStructure(out VertexPositionColorTexture[] vertices, out short[] indeces)
	{
		var vertexList = new List<VertexPositionColorTexture>();
		var indexList = new List<short>();

		//Cut down a bit on boilerplate by adding a method
		void AddVertexIndex(Vector2 position, Vector2 TextureCoords)
		{
			indexList.Add((short)vertexList.Count);
			vertexList.Add(new VertexPositionColorTexture(new Vector3(position - Main.screenPosition, 0), Color, TextureCoords));
		}

		//Check if the array is not too small first
		if(PositionArray.Length >= 2)
		{
			//Iterate through the given array of positions
			for (int i = 0; i < PositionArray.Length - 1; i++)
			{
				int start = 0;

				float progress = (i + 1) / (float)PositionArray.Length;

				//If on the first element of the array, add the vertices corresponding to the front of the trail
				if (i == start)
				{
					Vector2 currentPosition = PositionArray[i];

					Vector2 currentWidthUnit = CurveNormalHelper.CurveNormal([.. PositionArray], i);

					float startWidth = 1;
					Vector2 currentLeft = currentPosition - currentWidthUnit * Width * startWidth;
					Vector2 currentRight = currentPosition + currentWidthUnit * Width * startWidth;

					AddVertexIndex(currentRight, new Vector2(1, 0));
					AddVertexIndex(currentLeft, new Vector2(0, 0));
				}

				Vector2 nextPosition = PositionArray[i + 1];

				Vector2 nextWidthUnit = CurveNormalHelper.CurveNormal(PositionArray.ToList(), i + 1);

				Vector2 nextLeft = nextPosition - nextWidthUnit * Width;
				Vector2 nextRight = nextPosition + nextWidthUnit * Width;

				AddVertexIndex(nextRight, new Vector2(1, progress));
				AddVertexIndex(nextLeft, new Vector2(0, progress));
			}
		}

		vertices = [.. vertexList];
		indeces = [.. indexList];
	}
}
