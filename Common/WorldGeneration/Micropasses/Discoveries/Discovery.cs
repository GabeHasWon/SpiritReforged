using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Discoveries;

internal abstract class Discovery : Micropass
{
	public override abstract string WorldGenName { get; }

	public sealed override int GetWorldGenIndexInsert(List<GenPass> tasks, ref bool afterIndex)
	{
		var valid = DiscoverySystem.valid;
		if (!valid.Contains(this))
			return -1;

		return GetWorldGenIndexInsert(tasks, [.. valid], ref afterIndex);
	}

	public abstract int GetWorldGenIndexInsert(List<GenPass> passes, List<Discovery> discoveries, ref bool afterIndex);
	public override abstract void Run(GenerationProgress progress, GameConfiguration config);
}
