using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.WorldGeneration.SecretSeeds;
using SpiritReforged.Common.WorldGeneration.SecretSeeds.Seeds;
using SpiritReforged.Content.Underground.Moss.Oganesson;
using SpiritReforged.Content.Underground.Moss.Radon;
using System.Reflection;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class NewNeonsMicropass : Micropass
{
	public override string WorldGenName => "New Neon Mosses";

	/// <summary> An array of natural glowing moss types from Spirit and vanilla. </summary>
	public static int[] NeonMossTypes { get; private set; }
	private static FieldInfo NeonMossInfo;

	public override void Load(Mod mod)
	{
		NeonMossInfo = typeof(WorldGen).GetField("neonMossType", BindingFlags.Static | BindingFlags.NonPublic);
		On_WorldGen.randMoss += ForceNewMoss;
	}

	private static void ForceNewMoss(On_WorldGen.orig_randMoss orig, bool justNeon)
	{
		orig(justNeon);

		NeonMossTypes ??= [TileID.KryptonMoss, TileID.XenonMoss, TileID.ArgonMoss, TileID.VioletMoss, ModContent.TileType<RadonMoss>(), ModContent.TileType<OganessonMoss>()];

		if (WorldGen.genRand.NextBool(6))
			NeonMossInfo.SetValue(null, (ushort)WorldGen.genRand.Next([ModContent.TileType<RadonMoss>(), ModContent.TileType<OganessonMoss>()]));
	}

	public override int GetWorldGenIndexInsert(List<GenPass> passes, ref bool afterIndex)
	{
		afterIndex = false;
		return passes.FindIndex(genpass => genpass.Name.Equals("Moss Grass"));
	}

	public override void Run(GenerationProgress progress, Terraria.IO.GameConfiguration config)
	{
		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.NeonMosses");

		if (SecretSeedSystem.WorldSecretSeed == SecretSeedSystem.GetSeed<NeonSeed>())
			GenerateAllMosses();

		for (int i = 40; i < Main.maxTilesX - 40; i++) //Grow long moss plants for our modded types
		{
			for (int j = (int)Main.worldSurface; j < Main.maxTilesY - 200; ++j)
				GrowPlants(i, j);
		}

		static void GrowPlants(int i, int j)
		{
			var tile = Main.tile[i, j];
			if (tile.HasTile && tile.TileType == ModContent.TileType<RadonMoss>() || tile.TileType == ModContent.TileType<OganessonMoss>())
			{
				for (int x = 0; x < 4; x++)
					(TileLoader.GetTile(tile.TileType) as GrassTile).GrowPlants(i, j);
			}
		}
	}

	/// <summary> Generate all neon mosses, per <see cref="NeonSeed"/>. </summary>
	private static void GenerateAllMosses()
	{
		float numGenerated = 0;
		float rate = 2100f / Main.maxTilesX;

		ushort gennedType = (ushort)NeonMossInfo.GetValue(null);
		int[] allTypes = [.. NeonMossTypes, TileID.RainbowMoss];

		int top = (int)Main.worldSurface;
		int left = 20;
		Rectangle area = new(left, top, Main.maxTilesX - left - 20, Main.maxTilesY - top - 20);

		WorldMethods.Generate(GeneratePatch, 1, out _, maxTries: 100);

		bool GeneratePatch(int i, int j)
		{
			if (Main.tile[i, j].HasTile && Main.tile[i, j].TileType == TileID.Stone)
			{
				ushort typeToGen = (ushort)allTypes[(int)numGenerated];
				if (typeToGen != gennedType) //Avoid generating the already selected moss type
				{
					NeonMossInfo.SetValue(null, typeToGen);
					WorldGen.neonMossBiome(i, j);
				}

				numGenerated += rate;
			}

			return numGenerated >= allTypes.Length;
		}
	}
}