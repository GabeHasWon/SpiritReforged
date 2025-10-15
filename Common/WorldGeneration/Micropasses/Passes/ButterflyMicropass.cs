using SpiritReforged.Common.WorldGeneration.Microbiomes;
using SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes;
using System.Linq;
using Terraria.DataStructures;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class ButterflyMicropass : Micropass
{
	public override string WorldGenName => "Butterfly Shrines";
	private static readonly ushort[] Ignore = [TileID.LivingWood, TileID.LeafBlock, TileID.BlueDungeonBrick, TileID.GreenDungeonBrick, TileID.PinkDungeonBrick];

	// Remnants will take care of our butterfly shrines on their end at some point, change in the future
	public override int GetWorldGenIndexInsert(List<GenPass> passes, ref bool afterIndex) => passes.FindIndex(genpass => genpass.Name.Equals("Sunflowers"));

	public override void Run(GenerationProgress progress, Terraria.IO.GameConfiguration config)
	{
		const int maxAttempts = 2000;

		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.Butterfly");
		int count = 0;
		int maxCount = Main.maxTilesX / WorldGen.WorldSizeSmallX; // 1 shrine in small and medium worlds, 2 in large

		Point16 size = ButterflyShrineBiome.Size;
		int third = Main.maxTilesX / 3;

		for (int a = 0; a < maxAttempts; a++)
		{
			Point16 pt = new(
				WorldGen.genRand.NextBool() ? WorldGen.genRand.Next(GenVars.leftBeachEnd, third) : WorldGen.genRand.Next(Main.maxTilesX - third, GenVars.rightBeachStart),
				(int)GenVars.worldSurface + WorldGen.genRand.Next(50, 100));

			Dictionary<ushort, int> typeToCount = [];
			WorldUtils.Gen(new Point(pt.X, pt.Y) - new Point(size.X / 2, size.Y / 2), new Shapes.Rectangle(size.X, size.Y), new Actions.TileScanner(TileID.Dirt).Output(typeToCount));

			if (typeToCount[TileID.Dirt] > size.X * size.Y * 0.5f && GenVars.structures.CanPlace(new Rectangle(pt.X, pt.Y, size.X, size.Y), 4))
			{
				var blacklist = new QuickConversion.BiomeType[] { QuickConversion.BiomeType.Jungle, QuickConversion.BiomeType.Mushroom, QuickConversion.BiomeType.Desert, QuickConversion.BiomeType.Ice };
				var biome = QuickConversion.FindConversionBiome(pt, size);

				if (blacklist.Contains(biome))
					continue;

				Microbiome.Create<ButterflyShrineBiome>(new Point(pt.X + size.X / 2, pt.Y));

				var origin = new Point(pt.X + size.X / 2, pt.Y + 8); //Centered position
				bool foundClearing = WorldUtils.Find(origin, Searches.Chain(new Searches.Up(1000), new Conditions.IsSolid().AreaOr(1, 50).Not()), out var top);
				top.Y += 50;

				if (foundClearing) //Generate a shaft like sword shrines do
				{
					ShapeData data = new();
					Point shaftOrigin = new(origin.X, top.Y + 10);
					int shaftHeight = origin.Y - top.Y - 9;

					//Sand wall fill
					WorldUtils.Gen(new(shaftOrigin.X - 1, shaftOrigin.Y - 1), new Shapes.Rectangle(3, shaftHeight + 2), Actions.Chain(
						new Modifiers.Blotches(2, 0.2),
						new Modifiers.OnlyTiles(TileID.Sand, TileID.HardenedSand, TileID.Sandstone),
						new Actions.PlaceWall(WallID.HardenedSand)
					));

					WorldUtils.Gen(shaftOrigin, new Shapes.Rectangle(1, shaftHeight), Actions.Chain(
						new Modifiers.Blotches(2, 0.2),
						new Modifiers.SkipTiles(Ignore),
						new Actions.ClearTile().Output(data),
						new Modifiers.Expand(1),
						new Modifiers.OnlyTiles(TileID.Sand),
						new Actions.SetTileKeepWall(TileID.HardenedSand).Output(data)
					));

					WorldUtils.Gen(new Point(origin.X, top.Y + 10), new ModShapes.All(data), new Actions.SetFrames(frameNeighbors: true));
				}

				if (++count >= maxCount)
					return;
			}
		}

		SpiritReforgedMod.Instance.Logger.Info("Generator exceeded maximum tries for structure: Butterfly Shrine");
	}
}