using SpiritReforged.Common.WorldGeneration.Microbiomes;
using Terraria.DataStructures;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.ModCompat.Spooky.Generation;

internal class RottenGourdMicrobiome : Microbiome
{
	public class CheckWall(int wall) : GenCondition
	{
		private readonly int Wall = wall;

		protected override bool CheckValidity(int x, int y)
		{
			if (!WorldGen.InWorld(x, y, 10))
				return false;

			if (_tiles[x, y].WallType == Wall)
				return true;

			return false;
		}
	}

	protected override void OnPlace(Point16 point)
	{
		if (!CrossMod.Spooky.Enabled)
			return;

		Mod spooky = CrossMod.Spooky.Instance;

		int baseWidth = WorldGen.genRand.Next(8, 16);
		int baseHeight = baseWidth + WorldGen.genRand.Next(-3, 4);
		int id = WorldGen.genRand.Next(8);

		int tileType = spooky.Find<ModTile>(id switch
		{
			0 => "GourdBlockGreen",
			1 => "GourdBlockLime",
			2 => "GourdBlockWhite",
			3 => "GourdBlockLimeOrange",
			4 => "GourdBlockOrange",
			5 => "GourdBlockRed",
			6 => "GourdBlockYellow",
			_ => "GourdBlockYellowGreen",
		}).Type;

		int wallType = spooky.Find<ModWall>(id switch
		{
			0 => "GourdBlockGreenWall",
			1 => "GourdBlockLimeWall",
			2 => "GourdBlockWhiteWall",
			3 => "GourdBlockLimeOrangeWall",
			4 => "GourdBlockOrangeWall",
			5 => "GourdBlockRedWall",
			6 => "GourdBlockYellowWall",
			_ => "GourdBlockYellowGreenWall",
		}).Type;

		ShapeData shapes = new();

		Point gourdPos = point.ToPoint();
		int reps = Math.Min(WorldGen.genRand.Next(1, 5), WorldGen.genRand.Next(1, 5));
		WorldUtils.Gen(gourdPos, new Shapes.Circle(baseWidth, baseHeight), Actions.Chain(new Modifiers.Blotches(2, 0.4f), new Actions.ClearTile(), 
			new Actions.PlaceWall((ushort)wallType)).Output(shapes));
		Point off = new Point(0, 0);

		for (int i = 0; i < reps; ++i)
		{
			off.X += WorldGen.genRand.Next(-2, 3);
			off.Y -= (int)WorldGen.genRand.NextFloat(baseHeight * 0.8f, baseHeight * 1.2f);
			baseWidth = (int)(baseWidth * WorldGen.genRand.NextFloat(0.55f, 0.8f));
			baseHeight = (int)(baseHeight * WorldGen.genRand.NextFloat(0.55f, 0.8f));

			WorldUtils.Gen(gourdPos, new Shapes.Circle(baseWidth, baseHeight), Actions.Chain(new Modifiers.Offset(off.X, off.Y), new Modifiers.Blotches(2, 0.4f), new Actions.ClearTile(),
				new Actions.PlaceWall((ushort)wallType)).Output(shapes));
		}

		int wallReps = WorldGen.genRand.Next(3, 6);
		for (int i = 0; i < wallReps; ++i)
		{
			ShapeData tileData = new();
			GenAction chain = i == 0
				? Actions.Chain(new Modifiers.Dither(0.4f), new Actions.PlaceTile((ushort)tileType)).Output(tileData)
				: Actions.Chain(new Modifiers.Dither(0.6f), new Modifiers.Conditions(new CheckWall(wallType)), new Actions.ClearTile(), 
					new Actions.PlaceTile((ushort)tileType)).Output(tileData);
			WorldUtils.Gen(point.ToPoint(), new ModShapes.OuterOutline(shapes, true), chain);
			shapes = tileData;
		}

		WorldUtils.Gen(gourdPos, new Shapes.Circle((int)(baseWidth * WorldGen.genRand.NextFloat(0.4f, 0.55f)), (int)(baseHeight * WorldGen.genRand.NextFloat(0.4f, 0.55f)) + 1), 
			Actions.Chain(new Modifiers.Offset(off.X, off.Y - baseHeight - 3), new Actions.ClearTile(), new Modifiers.Blotches(2, 0.4f), new Actions.PlaceTile(TileID.LivingWood)));
	}
}
 