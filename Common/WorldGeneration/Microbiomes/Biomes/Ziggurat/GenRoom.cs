namespace SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes.Ziggurat;

public abstract class GenRoom
{
	public static readonly Point Left = new(-1, 0);
	public static readonly Point Right = new(1, 0);
	public static readonly Point Top = new(0, -1);
	public static readonly Point Bottom = new(0, 1);

	public sealed class Link(Point location, Point direction)
	{
		public readonly Point Location = location;
		public readonly Point Direction = direction;

		public bool consumed;
	}

	/// <summary> The boundaries of this room. </summary>
	public Rectangle Bounds => new(Origin.X - Size.X / 2, Origin.Y - Size.Y / 2, Size.X, Size.Y);

	public Point Origin { get; private set; }
	/// <summary> Stores all links associated with this room. </summary>
	public readonly HashSet<Link> Links;
	private readonly Point Size;

	public GenRoom(Point origin = default)
	{
		Links = [];
		
		if (origin != default)
			SetOrigin(origin);

		Initialize(out Size);
	}

	/// <summary> </summary>
	/// <param name="size"> The approximate dimensions of this room. It's important that all tasks in <see cref="Create"/> respect this range. </param>
	protected abstract void Initialize(out Point size);
	/// <summary> Runs the worldgen tasks associated with this room. </summary>
	public virtual void Create() { }
	public virtual void AddLinks() { }

	/// <summary> Sets the origin of this room. Useful before calling <see cref="Create"/> if the value relies on data set in <see cref="Initialize"/>. <br/>
	/// Otherwise, the origin can simply be set using the default constructor. </summary>
	public GenRoom SetOrigin(Point value)
	{
		Origin = value;
		Links.Clear();
		AddLinks();

		return this;
	}

	public bool Intersects(Point point, int padding = 0)
	{
		Rectangle bounds = Bounds;
		bounds.Inflate(padding, padding);

		return bounds.Contains(point);
	}

	public bool Intersects(Rectangle area, int padding = 0)
	{
		Rectangle bounds = Bounds;
		bounds.Inflate(padding, padding);

		return bounds.Intersects(area);
	}
}