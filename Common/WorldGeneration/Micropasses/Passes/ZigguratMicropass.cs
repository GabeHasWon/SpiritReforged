using SpiritReforged.Common.WorldGeneration.Microbiomes;
using SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes.Ziggurat;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class ZigguratMicropass : Micropass
{
	public override string WorldGenName => "Ziggurat";

	public override int GetWorldGenIndexInsert(List<GenPass> tasks, ref bool afterIndex) => tasks.FindIndex(x => x.Name == "Pyramids") - 1;
	public override void Run(GenerationProgress progress, GameConfiguration config)
	{
		const int scanRadius = 10;
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

			Dictionary<ushort, int> typeToCount = [];
			WorldUtils.Gen(foundPos, new Shapes.Circle(scanRadius), new Actions.TileScanner(TileID.Sand, TileID.SandstoneBrick).Output(typeToCount));

			if (typeToCount[TileID.Sand] < scanRadius * scanRadius * 0.4f || typeToCount[TileID.SandstoneBrick] > 10)
				continue;

			(x, y) = (foundPos.X, foundPos.Y);
			Microbiome.Create<ZigguratBiome>(new(x, y + (int)(ZigguratBiome.Height * 0.3f)));
			break;
		}
	}
}