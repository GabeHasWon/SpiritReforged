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
	public StripTaperType TaperingType { get; set; } 
	//Note: tapering through primitives doesn't really work well due to perspective warping- each triangle in a mesh doesn't know where the other triangles are, so the shader gets warped when the mesh has inconsistent sizes
	//Can be fixed but it seems rather complex to implement, probably best just to remove width changing features and handle it all through the shaders themselves

	public delegate float StripWidthDelagate(float progress);

	public StripWidthDelagate WidthDelegate { get; set; } = null;

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
		if(PositionArray.Length >= (TaperingType == StripTaperType.TaperBoth ? 3 : 2))
		{
			//Iterate through the given array of positions
			for (int i = 0; i < PositionArray.Length - 1; i++)
			{
				int start = 0;
				int end = PositionArray.Length - 2;

				float progress = (i + 1) / (float)PositionArray.Length;

				//Modify width of the triangles based on progress through iterating through the array
				float widthModifier = 1;
				switch (TaperingType)
				{
					case StripTaperType.TaperStart:
						widthModifier *= progress;
						break;
					case StripTaperType.TaperEnd:
						widthModifier *= (1 - progress);
						break;
					case StripTaperType.TaperBoth:
						progress = (i + 1) / (float)(PositionArray.Length - 1);
						widthModifier *= (float)Math.Pow(1 - Math.Abs(progress - 0.5f) * 2, 0.5f);
						break;
				}

				if (WidthDelegate != null)
					widthModifier *= WidthDelegate.Invoke(progress);

				//If on the first element of the array, add the vertices corresponding to the front of the trail
				if (i == start)
				{
					Vector2 currentPosition = PositionArray[i];
					if (TaperingType == StripTaperType.TaperStart || TaperingType == StripTaperType.TaperBoth) //Only add the center point if set to taper at the start
						AddVertexIndex(currentPosition, new Vector2(0.5f, 0));

					else
					{
						Vector2 currentWidthUnit = CurveNormalHelper.CurveNormal(PositionArray.ToList(), i);

						float startWidth = WidthDelegate == null ? 1 : WidthDelegate.Invoke(0);
						Vector2 currentLeft = currentPosition - (currentWidthUnit * Width * startWidth);
						Vector2 currentRight = currentPosition + (currentWidthUnit * Width * startWidth);

						AddVertexIndex(currentRight, new Vector2(1, 0));
						AddVertexIndex(currentLeft, new Vector2(0, 0));
					}
				}

				Vector2 nextPosition = PositionArray[i + 1];
				if (i == end && (TaperingType == StripTaperType.TaperEnd || TaperingType == StripTaperType.TaperBoth)) //Only add the center point if set to taper at the end
					AddVertexIndex(nextPosition, new Vector2(0.5f, 1));

				else //Add vertices based on the next position of the array
				{
					Vector2 nextWidthUnit = CurveNormalHelper.CurveNormal(PositionArray.ToList(), i + 1);

					Vector2 nextLeft = nextPosition - (nextWidthUnit * Width * widthModifier);
					Vector2 nextRight = nextPosition + (nextWidthUnit * Width * widthModifier);

					//Needs to be in opposite order with this tapering type to avoid backwards facing primitives 
					if(TaperingType == StripTaperType.TaperBoth)
					{
						AddVertexIndex(nextLeft, new Vector2(0, progress));
						AddVertexIndex(nextRight, new Vector2(1, progress));
					}

					else
					{
						AddVertexIndex(nextRight, new Vector2(1, progress));
						AddVertexIndex(nextLeft, new Vector2(0, progress));
					}
				}
			}
		}

		vertices = [.. vertexList];
		indeces = [.. indexList];
	}
}
