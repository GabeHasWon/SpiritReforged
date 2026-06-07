using SpiritReforged.Common.WorldGeneration.GenConfiguration;
using SpiritReforged.Common.WorldGeneration.Micropasses.CaveEntrances;
using SpiritReforged.Content.Forest.Stargrass.Items;
using System.Runtime.CompilerServices;
using Terraria.ModLoader.Config;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class StargrassMicropass : Micropass, IGenerationPage
{
	public override string WorldGenName => "Stargrass Patch";

	[GenConfigurable(0, 500, 10)]
	[Slider]
	private static int MaxStargrassCount = 0;

	PageInfo IGenerationPage.Info => new()
	{
		CopiedPage = new CanyonEntrance(),
	};

	Mod IGenerationPage.Mod => SpiritReforgedMod.Instance;

	public override int GetWorldGenIndexInsert(List<GenPass> passes, ref bool afterIndex) => passes.FindIndex(genpass => genpass.Name.Equals("Sunflowers"));

	public override void Run(GenerationProgress progress, Terraria.IO.GameConfiguration config)
	{
		const int attempts = 300;
		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.Stargrass");
		
		float worldSize = Main.maxTilesX / 4200f;
		int count = 0;
		MaxStargrassCount = this.GetPage().ValueOrDefault(nameof(MaxStargrassCount), (int)(4 * worldSize));

		for (int a = 0; a < attempts; a++)
		{
			bool failed = false;
			int x = WorldGen.genRand.Next(100, Main.maxTilesX - 100);
			int y = WorldGen.remixWorldGen ? WorldGen.genRand.Next(Main.maxTilesY / 2, Main.maxTilesY - 200) : (int)(Main.worldSurface * 0.35f);

			while (!Main.tile[x, y].HasTile || Main.tile[x, y].TileType != TileID.Grass)
			{
				if (++y > Main.worldSurface)
				{
					failed = true;
					break;
				}
			}

			if (failed)
				continue;

			int size = WorldGen.genRand.Next(30, 61);
			WorldGen.Convert(x, y, StarConversion.ConversionType, size);

			if (++count > MaxStargrassCount)
				break;
		}
	}
}
