using System.Linq;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes.Ziggurat;

public class BasicRoom(Rectangle bounds, Point origin = default) : GenRoom(origin)
{
	protected Rectangle _bounds = bounds;

	protected override void Initialize(out Point size) => size = new(ZigguratBiome.Width / WorldGen.genRand.Next([6, 8]), 14);

	public override void Create()
	{
		const int curveHeight = 4;
		Rectangle rectangleBounds = new(Bounds.Left, Bounds.Top + curveHeight, Bounds.Width, Bounds.Height - curveHeight);

		WorldUtils.Gen(new(rectangleBounds.Left, rectangleBounds.Top), new Shapes.Rectangle(rectangleBounds.Width, rectangleBounds.Height), new Actions.ClearTile());
		WorldUtils.Gen(new(Origin.X, rectangleBounds.Top - 1), new Shapes.Mound((int)(rectangleBounds.Width * 0.75f), curveHeight), Actions.Chain(
			new Modifiers.RectangleMask(-rectangleBounds.Width / 2, rectangleBounds.Width / 2, -curveHeight, curveHeight),
			new Actions.ClearTile()
		));

		AddLinks();
	}

	/// <summary> Called after all possible hallways linking ziggurat rooms are placed.<para/>
	/// Should be used to safely place furniture and check for consumed links in <see cref="GenRoom.Links"/>. </summary>
	public virtual void PostPlaceHallways() { }

	protected virtual void AddLinks()
	{
		Links.Add(new(new(Bounds.Left, Bounds.Bottom - 2), Left));
		Links.Add(new(new(Bounds.Right, Bounds.Bottom - 2), Right));

		if (WorldGen.genRand.NextBool(4))
			Links.Add(new(new(Bounds.Center.X, Bounds.Bottom), Bottom));
	}
}

public class EntranceRoom(Rectangle bounds, Point origin = default) : BasicRoom(bounds, origin)
{
	private Link _firstLink;

	protected override void Initialize(out Point size) => size = new(_bounds.Width / 2 + 1, _bounds.Height - 4);

	public override void Create()
	{
		const int scanDistance = 10;

		base.Create();
		int bottom = Bounds.Bottom - 2;

		if (WorldGen.genRand.NextBool() && Clear(new(_bounds.Left, bottom), new Searches.Left(scanDistance)))
			ZigguratBiome.BlockOut(new(Bounds.Left, bottom), new(_bounds.Left, bottom), 3);
		else if (Clear(new(_bounds.Right, bottom), new Searches.Right(scanDistance)))
			ZigguratBiome.BlockOut(new(Bounds.Right, bottom), new(_bounds.Right, bottom), 3);

		//Center indent
		int tailSquared = Bounds.Width / 6;
		WorldUtils.Gen(new(Bounds.Center.X - tailSquared, Bounds.Bottom), new Shapes.Tail(tailSquared, new(0, tailSquared / 2 + 1)), new Actions.ClearTile());
		WorldUtils.Gen(new(Bounds.Center.X + tailSquared, Bounds.Bottom), new Shapes.Tail(tailSquared, new(0, tailSquared / 2 + 1)), new Actions.ClearTile());
		WorldUtils.Gen(new(Bounds.Center.X, Bounds.Bottom), new Shapes.Rectangle(new(-tailSquared, 0, tailSquared * 2, tailSquared / 2 + 1)), new Actions.ClearTile());

		static bool Clear(Point origin, GenSearch search) => WorldUtils.Find(origin, Searches.Chain(search, new Conditions.IsSolid().AreaOr(3, 3).Not()), out _);
	}

	public override void PostPlaceHallways()
	{
		if (Links.Count == 1) //If the only link wasn't consumed
		{
			var origin = _firstLink.Location;
			WorldUtils.Gen(origin, new Shapes.Tail(8, new(10, 15)), new Actions.ClearTile());
		}
	}

	protected override void AddLinks() => Links.Add(_firstLink = new(new(Bounds.Center.X, Bounds.Bottom), Bottom));
}

/*public class Connector(Rectangle bounds, Point origin = default) : BasicRoom(bounds, origin)
{
	protected override void Initialize(out Point size) => size = new(ZigguratBiome.HallwayWidth + 2, ZigguratBiome.HallwayWidth + 2);

	public override void Create()
	{
		Rectangle rectangleBounds = new(Bounds.Left, Bounds.Top, Bounds.Width, Bounds.Height);
		WorldUtils.Gen(new(rectangleBounds.Left, rectangleBounds.Top), new Shapes.Rectangle(rectangleBounds.Width, rectangleBounds.Height), new Actions.ClearTile());

		AddLinks();
	}

	protected override void AddLinks()
	{
		Links.Add(new(new(Bounds.Left, Bounds.Bottom - 2), Left));
		Links.Add(new(new(Bounds.Right, Bounds.Bottom - 2), Right));
		Links.Add(new(new(Bounds.Center.X, Bounds.Top), Top));
	}
}*/