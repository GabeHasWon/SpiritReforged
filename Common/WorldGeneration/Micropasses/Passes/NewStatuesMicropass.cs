using SpiritReforged.Common.TileCommon;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class NewStatuesMicropass : Micropass
{
	/// <summary> The list of statues to generate in this micropass. </summary>
	public static readonly List<int> Statues = [];

	public override string WorldGenName => "New Statues";

	public override int GetWorldGenIndexInsert(List<GenPass> passes, ref bool afterIndex)
	{
		afterIndex = true;
		return passes.FindIndex(genpass => genpass.Name.Equals("Statues"));
	}

	public override void Run(GenerationProgress progress, Terraria.IO.GameConfiguration config)
	{
		const int maxTries = 1000; //Failsafe
		const int numPerType = 4;

		progress.Message = Lang.gen[29].Value; //Localization for `Statues`

		int maxStatues = Main.maxTilesX / WorldGen.WorldSizeSmallX * numPerType * Statues.Count;
		int statues = 0;

		for (int t = 0; t < maxTries; t++)
		{
			int x = WorldGen.genRand.Next(20, Main.maxTilesX - 20);
			int y = WorldGen.genRand.Next((int)Main.worldSurface, Main.UnderworldLayer - 20);

			WorldMethods.FindGround(x, ref y);
			Tile aboveTile = Framing.GetTileSafely(x, y - 1);

			if (y > Main.UnderworldLayer || y < (int)Main.worldSurface || WorldGen.oceanDepths(x, y) || aboveTile.LiquidAmount > 150)
				continue;

			int type = Statues[Math.Clamp(statues / numPerType, 0, Statues.Count - 1)];
			if (Placer.PlaceTile(x, y - 1, type).success && ++statues >= maxStatues)
				break;
		}
	}
}