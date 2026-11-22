using System.Linq;

namespace SpiritReforged.Common.VerletChains;

public struct ChainPhysics(float drag = 0.9f, float groundBounce = 0.5f, float gravity = 0.2f)
{
	public float drag = drag;
	public float groundBounce = groundBounce;
	public float gravity = gravity;
}

public class Chain
{
	private readonly int Stiffness = 6;
	public List<ChainSegment> Segments { get; set; }
	public List<ChainVertex> Vertices { get; set; }

	public ChainVertex FirstVertex { get; set; }
	public ChainVertex LastVertex { get; set; }

	public ChainPhysics Coefficients { get; set; }

	public Chain(float segmentLength, int segmentCount, Vector2 startPosition, ChainPhysics? physicsCoefficients = null, bool staticFirst = true, bool staticLast = true, int stiffness = 6)
	{
		Stiffness = stiffness;
		Coefficients = physicsCoefficients ?? new ChainPhysics();
		PopulateLists(segmentLength, segmentCount, startPosition, staticFirst, staticLast);
	}

	private void PopulateLists(float segmentLength, int segmentCount, Vector2 startPosition, bool staticFirst = true, bool staticLast = true)
	{
		Segments = new List<ChainSegment>();
		Vertices = new List<ChainVertex>();

		for (int i = 0; i < segmentCount; i++)
			Vertices.Add(new ChainVertex(startPosition, 1f, Coefficients.drag, Coefficients.groundBounce, Coefficients.gravity));

		for (int i = 0; i < segmentCount - 1; i++)
			Segments.Add(new ChainSegment(Vertices[i], Vertices[i + 1], segmentLength));

		FirstVertex = Vertices.First();
		FirstVertex.Static = staticFirst;

		LastVertex = Vertices.Last();
		LastVertex.Static = staticLast;
	}

	public void Update(Vector2 startPosition, Vector2 endPosition)
	{
		FirstVertex.StaticPos = startPosition;
		LastVertex.StaticPos = endPosition;

		foreach (var vertex in Vertices)
		{
			vertex.Update();
			vertex.StandardConstrain();
			vertex.SetStatic();
		}

		for (int i = 0; i < Stiffness; i++)
			foreach (var segment in Segments)
				segment.ConstrainLine();
	}

	public float StartRotation => Segments[0].Rotation();
	public float EndRotation => Segments[Segments.Count - 1].Rotation();
	public Vector2 StartPosition => Segments[0].Vertex1.Position;
	public Vector2 EndPosition => Segments[Segments.Count - 1].Vertex2.Position;

	public Vector2[] VerticesArray()
	{
		var verticeslist = new List<Vector2>();
		foreach (ChainVertex vertex in Vertices)
			verticeslist.Add(vertex.Position);

		return verticeslist.ToArray();
	}

	public void Draw(SpriteBatch sB, Texture2D texture, Texture2D headTexture = null, Texture2D tailTexture = null, float scale = 1)
	{
		foreach (var segment in Segments)
		{
			if (segment == Segments.Last() && headTexture != null)
				segment.Draw(sB, headTexture, headTexture.Frame(), scale);
			else if (segment == Segments.First() && tailTexture != null)
				segment.Draw(sB, tailTexture, tailTexture.Frame(), scale);
			else
				segment.Draw(sB, texture, texture.Frame(), scale);
		}
	}
}