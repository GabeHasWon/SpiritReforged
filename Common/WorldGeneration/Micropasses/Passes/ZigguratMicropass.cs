using SpiritReforged.Common.WorldGeneration.Microbiomes;
using SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes.Ziggurat;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class ZigguratMicropass : Micropass
{
	public override string WorldGenName => "Ziggurat";

	public override int GetWorldGenIndexInsert(List<GenPass> tasks, ref bool afterIndex) => tasks.FindIndex(x => x.Name == "Full Desert"); //"Water Chests" "Full Desert"
	public override void Run(GenerationProgress progress, GameConfiguration config)
	{
		int x = WorldGen.genRand.Next(GenVars.UndergroundDesertLocation.Left, GenVars.UndergroundDesertLocation.Right);
		int y = GenVars.UndergroundDesertLocation.Y - 40;

		if (!WorldUtils.Find(new Point(x, y), new Searches.Down(1500).Conditions(new Conditions.IsSolid()), out Point foundPos))
			return; // ?? big hole where the desert is?

		(x, y) = (foundPos.X, foundPos.Y);
		Microbiome.Create<ZigguratBiome>(new(x, y + ZigguratBiome.Height / 4));
	}
}