using SpiritReforged.Common.Easing;
using SpiritReforged.Common.WorldGeneration.Microbiomes;
using SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes.Ziggurat;
using SpiritReforged.Common.WorldGeneration.Noise;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class ZigguratMicropass : Micropass
{
	public override string WorldGenName => "Ziggurat";

	public override int GetWorldGenIndexInsert(List<GenPass> tasks, ref bool afterIndex) => tasks.FindIndex(x => x.Name == "Pyramids");
	public override void Run(GenerationProgress progress, GameConfiguration config)
	{
		const int scanRadius = 50;
		const int range = ZigguratBiome.Width / 2;

		Rectangle loc = GenVars.UndergroundDesertLocation;
		for (int a = 0; a < 300; a++)
		{
			int rangeLeft = WorldGen.genRand.Next(loc.Left, Math.Max((int)(loc.Center().X - range), loc.Left + 20));
			int rangeRight = WorldGen.genRand.Next(Math.Min((int)(loc.Center().X + range), loc.Right - 20), loc.Right);

			int x = WorldGen.genRand.Next([rangeLeft, rangeRight]);
			int y = loc.Y - 40;

			if (!WorldUtils.Find(new(x, y), new Searches.Down(1500).Conditions(new Conditions.IsSolid()), out Point foundPos))
				return; // ?? big hole where the desert is?

			Point zigguratPos = new(foundPos.X, foundPos.Y + (int)(ZigguratBiome.Height * 0.3f));

			Dictionary<ushort, int> typeToCount = [];
			WorldUtils.Gen(zigguratPos, new Shapes.Rectangle(new Rectangle(-(ZigguratBiome.Width / 2), -(ZigguratBiome.Height / 2), ZigguratBiome.Width, ZigguratBiome.Height)), new Actions.TileScanner(TileID.Sand, TileID.SandstoneBrick).Output(typeToCount));

			if (typeToCount[TileID.Sand] < scanRadius * scanRadius * 0.5f || typeToCount[TileID.SandstoneBrick] > 10)
				continue;

			CreateDunes(foundPos.X - 80, foundPos.X + 80, foundPos.Y, 10);
			Microbiome.Create<ZigguratBiome>(zigguratPos);

			break;
		}

		//WorldMethods.Generate(GenerateRuins, 3, out _, new(loc.X, loc.Y - 40, loc.Width, 40), 100);
	}

	/*private static bool GenerateRuins(int x, int y)
	{
		if (WorldGen.SolidOrSlopedTile(x, y) || !WorldUtils.Find(new(x, y), new Searches.Down(30).Conditions(new Conditions.IsSolid()), out Point foundPos))
			return false;

		Rectangle scanRectangle = new(-10, -10, 20, 20);
		Dictionary<ushort, int> typeToCount = [];
		WorldUtils.Gen(foundPos, new Shapes.Rectangle(scanRectangle), new Actions.TileScanner(TileID.Sand, TileID.SandstoneBrick).Output(typeToCount));

		if (typeToCount[TileID.Sand] < scanRectangle.Width * scanRectangle.Height * 0.5f || typeToCount[TileID.SandstoneBrick] > 5)
			return false;

		CreateRuin(foundPos.X, foundPos.Y - 20, WorldGen.genRand.Next(2, 5));
		return true;
	}*/

	public static void CreateDunes(int left, int right, int startY, int duneHeight)
	{
		FastNoiseLite noise = new(WorldGen.genRand.Next());
		noise.SetFrequency(0.03f);

		int y = WorldMethods.FindGround(left, startY);

		for (int x = left; x < right; x++)
		{
			int groundY = WorldMethods.FindGround(x, y);

			if (CanCreateColumn(x, groundY))
			{
				float targetY = groundY - (duneHeight + noise.GetNoise(x, 100) * duneHeight) * EaseFunction.EaseSine.Ease((float)(x - left) / (right - left));
				y = (int)MathHelper.Lerp(y, targetY, 0.3f);

				FillColumn(x, y, TileID.Sand);
			}
			else
			{
				left = x;
			}
		}

		void FillColumn(int x, int top, ushort tileType)
		{
			int y = top;
			int digMax = (int)Math.Max(4f + noise.GetNoise(x, 100) * 8, 1);
			int dig = 0;

			while (WorldGen.InWorld(x, y, 20) && dig < digMax)
			{
				if (WorldGen.SolidTile3(x, y))
					dig++;

				var tile = Main.tile[x, y++];

				if (TileID.Sets.GeneralPlacementTiles[tile.TileType])
					tile.ResetToType(tileType);
			}
		}

		static bool CanCreateColumn(int x, int y) => Main.tile[x, y].TileType != ModContent.TileType<SaltBlockReflective>() && Main.tile[x, y].TileType != ModContent.TileType<SaltBlockDull>();
	}

	/*public static void CreateRuin(int x, int y, int segments)
	{
		List<Rectangle> areas = [];
		Point direction = new();

		for (int c = 0; c < segments; c++)
		{
			Point rectSize = new(8, 8);

			Rectangle source = new(x - rectSize.X / 2, y - rectSize.Y / 2, rectSize.X, rectSize.Y);
			source.X += rectSize.X * direction.X;
			source.Y += rectSize.Y * direction.Y;

			if (!areas.Contains(source))
				areas.Add(source); //Avoid duplicates if direction reiterates

			int unit = WorldGen.genRand.NextFromList(-1, 1);
			direction += WorldGen.genRand.NextBool() ? new(unit, 0) : new(0, unit);
		}

		segments = areas.Count; //Reassign segments to be consistent with our number of predetermined areas
		var shapeData = Enumerable.Repeat(new ShapeData(), segments).ToArray();

		for (int c = 0; c < segments; c++)
		{
			Rectangle a = areas[c];
			WorldUtils.Gen(a.Center, new Shapes.Rectangle(a.Width, a.Height), Actions.Chain(
				new Actions.PlaceWall((ushort)RedSandstoneBrickCrackedWall.UnsafeType),
				new Modifiers.RectangleMask(2, a.Width - 3, 0, a.Height),
				new Actions.PlaceWall(WallID.Sandstone)
			).Output(shapeData[c]));
		} //Generate all segment walls first and collect ShapeData

		for (int c = 0; c < segments; c++)
		{
			Rectangle a = areas[c];
			WorldUtils.Gen(a.Center, new ModShapes.OuterOutline(shapeData[c]), Actions.Chain(
				new Modifiers.SkipWalls(WallID.Sandstone, (ushort)RedSandstoneBrickCrackedWall.UnsafeType),
				new Actions.PlaceTile((ushort)ModContent.TileType<RedSandstoneBrick>())
			));
		}
	}*/
}