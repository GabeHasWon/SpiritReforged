using Terraria.DataStructures;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.CaveEntrances;

internal class KarstEntrance : CaveEntrance
{
	public override CaveEntranceType Type => CaveEntranceType.Karst;

	public override void Generate(int x, int y)
	{
		y += 16;
		ShapeData data = new();

		WorldUtils.Gen(new Point(x, y), new Shapes.Circle(20, 8),
			Actions.Chain(new Actions.ClearWall().Output(data), new Modifiers.Blotches(3, 2, 0.6f),
			new Actions.ClearTile().Output(data)));

		int count = WorldGen.genRand.Next(1, 4);

		for (int i = 1; i < count + 1; ++i)
		{
			var origin = new Point(x + WorldGen.genRand.Next(-5 * i, 5 * i + 1), y + WorldGen.genRand.Next(2, 5 * i));
			bool circle = WorldGen.genRand.NextBool();
			GenShape shape = circle ? new Shapes.Circle(12 + 3 * i, 3 + i) : new Shapes.Rectangle(12 + 3 * i, 4 + i);
			var blotches = new Modifiers.Blotches(circle ? 3 : 7, circle ? 2 : 4, circle ? 0.9f : 0.6f);
			WorldUtils.Gen(origin, shape, Actions.Chain(blotches, new Actions.ClearTile().Output(data)));
		}

		WorldUtils.Gen(new Point(x, y - 26), new Shapes.Rectangle(8, 25),
			Actions.Chain(new Modifiers.Blotches(4, 7, 0.2f), new Actions.ClearWall(), new Modifiers.Blotches(5, 3, 0.6f), new Actions.ClearTile().Output(data)));

		foreach (var pos in data.GetData())
		{
			if (pos.Y > 0)
			{
				Tile tile = Main.tile[new Point16(pos.X + x, pos.Y + y)];
				tile.LiquidAmount = 255;
				tile.LiquidType = LiquidID.Water;
			}

			for (int i = pos.X - 4 + x; i < pos.X + 4 + x; i++)
				for (int j = pos.Y - 4 + y; j < pos.Y + 4 + y; ++j)
				{
					if (data.Contains(i, j))
						continue;

					Tile tile = Main.tile[i, j];

					if (tile.WallType == WallID.DirtUnsafe)
						tile.WallType = WallID.GrassUnsafe;
					else if (tile.WallType == WallID.MudUnsafe)
						tile.WallType = WallID.JungleUnsafe;
				}
		}
	}

	public override bool ModifyOpening(ref int x, ref int y, bool isCavinator)
	{
		if (isCavinator)
			x += WorldGen.genRand.NextBool() ? WorldGen.genRand.Next(-20, -16) : WorldGen.genRand.Next(15, 21);

		for (int i = x - 40; i < x + 40; ++i)
			for (int j = y - 40; j < y + 40; ++j)
			{
				Tile tile = Main.tile[i, j];
				Tile up = Main.tile[i, j - 1];
				Tile down = Main.tile[i, j + 1];

				if (up.WallType == WallID.None && down.WallType == WallID.None)
					tile.WallType = WallID.None;
				else if (up.WallType != WallID.None && down.WallType != WallID.None)
					tile.WallType = WorldGen.genRand.NextBool() ? down.WallType : up.WallType;
			}

		return true;
	}
}
