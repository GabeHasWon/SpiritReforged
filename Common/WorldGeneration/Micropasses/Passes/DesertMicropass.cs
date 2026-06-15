using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.WorldGeneration.GenConfiguration;
using SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes;
using SpiritReforged.Content.Desert.DragonFossil;
using SpiritReforged.Content.Desert.Tiles;
using Terraria.ModLoader.Config;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class DesertMicropass : Micropass, IGenerationPage
{
	private const int DefaultPatchScale = 10;

	[GenConfigurable(0, 60)]
	[Slider]
	private static int FossilCount = 13;

	[GenConfigurable(0f, 10f)]
	[Slider]
	private static float FossilMultiplier = 1;

	[GenConfigurable(1, 40)]
	private static int PatchScale = DefaultPatchScale;

	public override string WorldGenName => "Desert Extras";

	PageInfo IGenerationPage.Info => new()
	{
		CopiedPage = new UndergroundOasisBiome()
	};

	Mod IGenerationPage.Mod => SpiritReforgedMod.Instance;

	public override int GetWorldGenIndexInsert(List<GenPass> passes, ref bool afterIndex)
	{
		afterIndex = true;
		return passes.FindIndex(genpass => genpass.Name.Equals("Webs"));
	}

	public override void Run(GenerationProgress progress, Terraria.IO.GameConfiguration config)
	{
		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.DesertExtras");

		int generated = 0;
		int maxAmount = (int)(FossilCount * (WorldGen.GetWorldSize() + 1) * FossilMultiplier);

		int top = (int)(Main.worldSurface * 1.2f);
		Rectangle region = new(GenVars.desertHiveLeft, top, GenVars.desertHiveRight - GenVars.desertHiveLeft, GenVars.desertHiveLow - top);
		bool addedDragon = false;

		for (int a = 0; a < 200; a++)
		{
			var coords = WorldGen.genRand.NextVector2FromRectangle(region);
			int i = (int)coords.X;
			int j = (int)coords.Y;

			if (Main.tile[i, j].TileType == TileID.HardenedSand)
			{
				int scale = WorldGen.genRand.Next(PatchScale / 2, PatchScale * 2);

				WorldGen.OreRunner(i, j - 3, scale + 4, WorldGen.genRand.Next(1, 8), TileID.Sand);
				WorldGen.OreRunner(i, j, scale, WorldGen.genRand.Next(1, 8), (ushort)ModContent.TileType<PolishedAmber>());

				if (!addedDragon && AmberFossil.PlaceEntity(i, j) is FossilEntity e)
				{
					e.itemType = ModContent.ItemType<TinyDragon>();
					addedDragon = true;
				}

				AddFossils(i, j, scale / 2, scale);

				if (++generated >= maxAmount)
					break;
			}
		}
	}

	/// <summary> Randomly converts <see cref="PolishedAmber"/> into <see cref="AmberFossil"/>s around the provided coordinates, in an area based on <paramref name="scale"/>. </summary>
	private static void AddFossils(int i, int j, int count, int scale)
	{
		for (int c = 0; c < count; c++)
		{
			var coords = (new Vector2(i, j) + WorldGen.genRand.NextVector2Unit() * WorldGen.genRand.Next(scale)).ToPoint();
			
			if (Framing.GetTileSafely(coords).HasTileType(ModContent.TileType<PolishedAmber>()))
				Framing.GetTileSafely(coords).TileType = (ushort)ModContent.TileType<AmberFossil>();
		}
	}
}