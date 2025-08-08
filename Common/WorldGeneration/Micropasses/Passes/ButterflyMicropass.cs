using SpiritReforged.Content.Forest.ButterflyStaff;
using System.Linq;
using Terraria.DataStructures;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class ButterflyMicropass : Micropass
{
	public override string WorldGenName => "Butterfly Shrines";

	// Remnants will take care of our butterfly shrines on their end at some point, change in the future
	public override int GetWorldGenIndexInsert(List<GenPass> passes, ref bool afterIndex) => passes.FindIndex(genpass => genpass.Name.Equals("Sunflowers"));

	public override void Run(GenerationProgress progress, Terraria.IO.GameConfiguration config)
	{
		const int maxTries = 2000;

		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.Butterfly");
		int repeats = Main.maxTilesX / WorldGen.WorldSizeSmallX; // 1 shrine in small and medium worlds, 2 in large

		for (int i = 0; i < repeats; i++)
		{
			Point16 size = ButterflyShrineBiome.Size; 
			Point16 position = Point16.Zero;

			int tries = 0;
			while (tries < maxTries)
			{
				int third = Main.maxTilesX / 3;
				int x = WorldGen.genRand.NextBool() ? WorldGen.genRand.Next(GenVars.leftBeachEnd, third) : WorldGen.genRand.Next(Main.maxTilesX - third, GenVars.rightBeachStart);
				int y = (int)GenVars.worldSurface + WorldGen.genRand.Next(50, 100);

				position = new Point16(x, y);

				if (SolidPerimeter(new Rectangle(position.X, position.Y, size.X, size.Y)))
					break;

				tries++;
			}

			if (tries == maxTries)
			{
				SpiritReforgedMod.Instance.Logger.Info("Generator exceeded maximum tries for structure: Butterfly Shrine");
				return; //Failed
			}

			if (GenVars.structures.CanPlace(new Rectangle(position.X, position.Y, size.X, size.Y), 4))
			{
				var blacklist = new QuickConversion.BiomeType[] { QuickConversion.BiomeType.Jungle, QuickConversion.BiomeType.Mushroom, QuickConversion.BiomeType.Desert, QuickConversion.BiomeType.Ice };
				var biome = QuickConversion.FindConversionBiome(position, size);
	
				if (blacklist.Contains(biome))
				{
					i--;
					continue;
				}

				Microbiome.Create<ButterflyShrineBiome>(new(position.X + size.X / 2, position.Y));

				var origin = new Point(position.X + size.X / 2, position.Y + 8); //Centered position
				bool foundClearing = WorldUtils.Find(origin, Searches.Chain(new Searches.Up(1000), new Conditions.IsSolid().AreaOr(1, 50).Not()), out var top);
				top.Y += 50;

				if (foundClearing) //Generate a shaft like sword shrines do
				{
					var data = new ShapeData();
					ushort[] ignore = [TileID.LivingWood, TileID.LeafBlock, TileID.BlueDungeonBrick, TileID.GreenDungeonBrick, TileID.PinkDungeonBrick];

					//Fill sand-type walls
					WorldUtils.Gen(new Point(origin.X - 1, top.Y + 10), new Shapes.Rectangle(3, origin.Y - top.Y - 9), Actions.Chain(new Modifiers.Blotches(2, 0.2), new Modifiers.OnlyTiles(TileID.Sand, TileID.HardenedSand, TileID.Sandstone), new Actions.PlaceWall(WallID.HardenedSand)));

					WorldUtils.Gen(new Point(origin.X, top.Y + 10), new Shapes.Rectangle(1, origin.Y - top.Y - 9), Actions.Chain(new Modifiers.Blotches(2, 0.2), new Modifiers.SkipTiles(ignore), new Actions.ClearTile().Output(data), new Modifiers.Expand(1), new Modifiers.OnlyTiles(53), new Actions.SetTile(397).Output(data)));
					WorldUtils.Gen(new Point(origin.X, top.Y + 10), new ModShapes.All(data), new Actions.SetFrames(frameNeighbors: true));
				}
			}
			else
			{
				i--;
			}
		}
	}

	static bool SolidPerimeter(Rectangle area) //Scan the perimeter of 'area' for solid and dirt tiles
	{
		const float solidMargin = .9f, dirtMargin = .5f; //at least 90% solid and 50% dirt
		int solidCount = 0, dirtCount = 0;

		for (int a = 0; a < 2; a++)
		{
			for (int x = area.X; x < area.X + area.Width; x++)
			{
				int y = area.Y + area.Height * a;
				if (WorldGen.SolidTile(x, y))
				{
					solidCount++;

					if (Main.tile[x, y].TileType == TileID.Dirt)
						dirtCount++;
				}
			}
		}

		for (int a = 0; a < 2; a++)
		{
			for (int y = area.Y; y < area.Y + area.Height; y++)
			{
				int x = area.X + area.Width * a;
				if (WorldGen.SolidTile(x, y))
				{
					solidCount++;

					if (Main.tile[x, y].TileType == TileID.Dirt)
						dirtCount++;
				}
			}
		}

		int totalCount = area.Width * 2 + area.Height * 2;
		return solidCount / (float)totalCount >= solidMargin && dirtCount / (float)totalCount >= dirtMargin;
	}
}