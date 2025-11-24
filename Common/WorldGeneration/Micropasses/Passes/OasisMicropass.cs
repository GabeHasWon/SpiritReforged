using SpiritReforged.Common.WorldGeneration.Microbiomes;
using SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes;
using System.Linq;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class OasisMicropass : Micropass
{
	public override string WorldGenName => "Underground Oasis";

	public override int GetWorldGenIndexInsert(List<GenPass> passes, ref bool afterIndex)
	{
		afterIndex = true;
		return passes.FindIndex(genpass => genpass.Name.Equals("Micro Biomes"));
	}

	public override void Run(GenerationProgress progress, Terraria.IO.GameConfiguration config)
	{
		const int maxAttempts = 200;
		const int area = 50;

		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.DesertExtras");

		int attempts = 0;
		int amount = 3 * (WorldGen.GetWorldSize() + 1);
		Rectangle region = new(GenVars.desertHiveLeft, (int)Main.worldSurface + 40, GenVars.desertHiveRight - GenVars.desertHiveLeft, GenVars.desertHiveLow - GenVars.desertHiveHigh);

		HashSet<Rectangle> biomesRectangles = [];

		for (int i = 0; i < amount; i++)
		{
			var pt = WorldGen.genRand.NextVector2FromRectangle(region).ToPoint();

			if (!GenVars.structures.CanPlace(new Rectangle(pt.X - area / 2, pt.Y - area / 2, area, area), 4) || biomesRectangles.Any(x => x.Contains(pt)))
			{
				if (++attempts < maxAttempts)
					i--;

				continue;
			}

			Dictionary<ushort, int> typeToCount = [];
			WorldUtils.Gen(pt - new Point(area / 2, area / 2), new Shapes.Rectangle(area, area), new Actions.TileScanner(TileID.Sand, TileID.Sandstone, TileID.HardenedSand).Output(typeToCount));

			if (typeToCount[TileID.Sand] + typeToCount[TileID.Sandstone] + typeToCount[TileID.HardenedSand] < area * area * 0.5f)
			{
				if (++attempts < maxAttempts)
					i--;

				continue;
			}

			var biome = Microbiome.Create<UndergroundOasisBiome>(pt);
			var rectangle = biome.Rectangle;
			rectangle.Inflate(100, 100);

			biomesRectangles.Add(rectangle);
		}
	}
}