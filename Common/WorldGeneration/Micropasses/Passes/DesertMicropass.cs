using SpiritReforged.Common.WorldGeneration.Noise;
using SpiritReforged.Content.Desert.Tiles;
using SpiritReforged.Content.Desert.Tiles.Amber;
using SpiritReforged.Content.Desert.Walls;
using Terraria.WorldBuilding;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class DesertMicropass : Micropass
{
	private const int DefaultPatchScale = 10;

	public override string WorldGenName => "Desert Extras";

	public override int GetWorldGenIndexInsert(List<GenPass> passes, ref bool afterIndex)
	{
		afterIndex = true;
		return passes.FindIndex(genpass => genpass.Name.Equals("Webs"));
	}

	public override void Run(GenerationProgress progress, Terraria.IO.GameConfiguration config)
	{
		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.DesertExtras");

		int generated = 0;
		int maxAmount = 10 * (WorldGen.GetWorldSize() + 1);

		int top = (int)(Main.worldSurface * 1.2f);
		Rectangle region = new(GenVars.desertHiveLeft, top, GenVars.desertHiveRight - GenVars.desertHiveLeft, GenVars.desertHiveLow - top);
		
		for (int a = 0; a < 200; a++)
		{
			var coords = WorldGen.genRand.NextVector2FromRectangle(region);
			int i = (int)coords.X;
			int j = (int)coords.Y;

			if (Main.tile[i, j].TileType == TileID.Sand)
			{
				int scale = WorldGen.genRand.Next(DefaultPatchScale / 2, DefaultPatchScale * 2);

				WorldGen.OreRunner(i, j - 3, scale + 4, WorldGen.genRand.Next(1, 8), TileID.Sand);
				WorldGen.OreRunner(i, j, scale, WorldGen.genRand.Next(1, 8), (ushort)ModContent.TileType<PolishedAmber>());
				AddFossils(i, j, scale / 2, scale);

				if (++generated >= maxAmount)
					break;
			}
		}

		generated = 0;

		for (int a = 0; a < 50; a++)
		{
			var coords = WorldGen.genRand.NextVector2FromRectangle(region);
			int i = (int)coords.X;
			int j = (int)coords.Y;

			if (Main.tile[i, j].TileType == TileID.Sand)
			{
				FastNoiseLite noise = new(WorldGen.genRand.Next());
				noise.SetFrequency(0.1f);

				CreateScarabNest(i, j, WorldGen.genRand.Next(30, 80), WorldGen.genRand.Next(30, 80), noise, 120, -0.25f);

				if (++generated >= maxAmount)
					break;
			}
		}
	}

	private static void CreateScarabNest(int i, int j, int width, int height, FastNoiseLite noise, float outerThickness, float thickness = 0)
	{
		int halfWidth = width / 2;
		int halfHeight = height / 2;
		Rectangle area = new(i - halfWidth, j - halfHeight, width, height);

		for (int x = area.Left; x < area.Right; x++)
		{
			for (int y = j - area.Top; y < area.Bottom; y++)
			{
				float noiseValue = noise.GetNoise(x, y);
				float distance = Vector2.DistanceSquared(new Vector2(x, y), new Vector2(i, j));
				float distanceLimit = width * height * (0.1f + noiseValue * 0.05f);

				if (distance > distanceLimit)
					continue;

				Tile tile = Main.tile[x, y];
				tile.ClearTile();
				tile.WallType = (ushort)ModContent.WallType<SilkWall>();

				if (noiseValue < thickness || distance > distanceLimit - outerThickness)
					tile.ResetToType((ushort)ModContent.TileType<PaleHive>());
			}
		}

		int blobCount = WorldGen.genRand.Next(1, 8);
		for (int x = 0; x < blobCount; x++)
		{
			ShapeData data = new();
			var pt = WorldGen.genRand.NextVector2FromRectangle(area).ToPoint();
			int size = WorldGen.genRand.Next(5, 12);

			WorldUtils.Gen(pt, new Shapes.Slime(size, 1.0, 1.0), Actions.Chain(
				new Modifiers.Blotches(),
				new Actions.Clear(),
				new Modifiers.RadialDither(size, 3),
				new Actions.PlaceWall((ushort)ModContent.WallType<SilkWall>())
			).Output(data));

			WorldUtils.Gen(pt, new ModShapes.OuterOutline(data), Actions.Chain(
				new Modifiers.Blotches(), 
				new Modifiers.IsSolid(),
				new Actions.SetTileKeepWall((ushort)ModContent.TileType<PaleHive>())
			));
		}
	}

	/// <summary> Randomly converts <see cref="PolishedAmber"/> into <see cref="AmberFossil"/>s around the provided coordinates, in an area based on <paramref name="scale"/>. </summary>
	private static void AddFossils(int i, int j, int count, int scale)
	{
		for (int c = 0; c < count; c++)
		{
			var coords = (new Vector2(i, j) + WorldGen.genRand.NextVector2Unit() * WorldGen.genRand.Next(scale)).ToPoint();
			
			if (Framing.GetTileSafely(coords).TileType == ModContent.TileType<PolishedAmber>())
				Framing.GetTileSafely(coords).TileType = (ushort)ModContent.TileType<AmberFossil>();
		}
	}
}