using System.Linq;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Discoveries;

internal class DiscoverySystem : ModSystem
{
	internal static readonly HashSet<Discovery> valid = [];

	public override void PreWorldGen()
	{
		valid.Clear();

		var allDiscoveries = ModContent.GetContent<Discovery>().ToArray();
		int numDiscoveries = allDiscoveries.Length; //Math.Min(Main.maxTilesX / (WorldGen.WorldSizeSmallX / 2), allDiscoveries.Length); //Per world

		for (int a = 0; a < numDiscoveries; a++)
			if (!valid.Add(allDiscoveries[WorldGen.genRand.Next(allDiscoveries.Length)]))
				a--;
	}
}
